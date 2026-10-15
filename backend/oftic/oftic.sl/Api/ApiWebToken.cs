using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;  
using System;
using System.Linq;
using System.Net.Http;             
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos;
using Comun.Dtos.Administracion;
using Servicios.ApiInterfaz;

namespace Servicios.Api
{
    public class ApiWebToken : IApiWebToken
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ApiWebToken> _logger;

        public ApiWebToken(
            IHttpClientFactory factory,
            IConfiguration cfg,
            ILogger<ApiWebToken> logger)
        {
            _factory = factory;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<DtoRespuesta<string>> ObtenerTokenPipAsync(CancellationToken ct)
        {
            var client = _factory.CreateClient("AuthClient");

            var url = _cfg["ApiSettings:OudLogin"]?.Trim();
            var usuario = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
            var clave = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

            var payload = new DtoUsuarioPip
            {
                usuario = usuario,
                clave = clave
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content, ct);

            var responseJson = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OudLogin no autorizado o fallido. Status={StatusCode}. Body={Body}",
                    (int)resp.StatusCode,
                    responseJson);

                return new DtoRespuesta<string>
                {
                    Estado = false,
                    Codigo = (int)resp.StatusCode,
                    Mensaje = "OudLogin falló",
                    Respuesta = string.Empty
                };
            }

            return JsonSerializer.Deserialize<DtoRespuesta<string>>(responseJson,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new DtoRespuesta<string>
                   {
                       Estado = false,
                       Codigo = 500,
                       Mensaje = "Respuesta inválida de OudLogin",
                       Respuesta = string.Empty
                   };
        }

        public async Task<DtoRespuesta<string>> ObtenerTokenSeviciosAsync(DtoUsuarioPip usuario)
        {
            var client = _factory.CreateClient("AuthClient");  
            var url = _cfg["ApiSettings:PostPipToken"]?.Trim() ?? "https://internalpip.policia.gov.co:8080/api/Cuenta/Token";

            var content = new StringContent(
                JsonSerializer.Serialize(usuario),
                Encoding.UTF8,
                "application/json");

            using var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("PIP Token request failed: {StatusCode}", resp.StatusCode);
                return new DtoRespuesta<string>
                {
                    Estado = false,
                    Codigo = (int)resp.StatusCode,
                    Mensaje = "Credenciales o conexión inválida en PIP Token",
                    Respuesta = string.Empty
                };
            }

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DtoRespuesta<string>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new DtoRespuesta<string>
                {
                    Estado = false,
                    Codigo = 500,
                    Mensaje = "Respuesta inválida de OUD",
                    Respuesta = string.Empty
                };
        }

        public async Task<string> GetTokenAsync(string usuario, string contrasena)
        {
            try
            {
                var client = _factory.CreateClient("AuthClient");
                var url = _cfg["ApiSettings:PostPipToken"] ?? "https://internalpip.policia.gov.co:8080/api/Cuenta/Token";
                var payloads = new object[]
                {
                    new { Usuario = usuario, Clave = contrasena },
                    new { usuario, clave = contrasena },
                    new { usuario, contrasena },
                    new { Usuario = usuario, Contrasena = contrasena }
                };

                HttpResponseMessage? lastResponse = null;
                string lastPayload = string.Empty;
                string lastBody = string.Empty;

                foreach (var payload in payloads)
                {
                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var resp = await client.PostAsync(url, content);
                    var responseJson = await resp.Content.ReadAsStringAsync();
                    lastResponse = resp;
                    lastPayload = json;
                    lastBody = responseJson;

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Token attempt failed: {StatusCode}. Payload: {Payload}",
                            resp.StatusCode, json, responseJson);
                        continue;
                    }

                    var token = TryExtractToken(responseJson);
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        return token;
                    }

                    _logger.LogInformation("Token attempt without token in response. Payload: {Payload}", json);
                }

                if (lastResponse is not null)
                {
                    _logger.LogWarning(
                        "Token request failed after all attempts. LastStatus: {StatusCode}. LastPayload: {Payload}. LastBody: {Body}",
                        lastResponse.StatusCode,
                        lastPayload,
                        lastBody);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token");
                return string.Empty;
            }
        }

        private static string TryExtractToken(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            // Intento 1: formato conocido { token: "..."}
            var dto = JsonSerializer.Deserialize<DtoTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(dto?.token))
            {
                return dto.token!;
            }

            // Intento 2: formatos alternos { Token }, { respuesta }, { Respuesta }, { data: { token } }
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (TryGetString(root, "token", out var token)) return token;
            if (TryGetString(root, "Token", out token)) return token;
            if (TryGetString(root, "respuesta", out token)) return token;
            if (TryGetString(root, "Respuesta", out token)) return token;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(data, "token", out token)) return token;
                if (TryGetString(data, "Token", out token)) return token;
            }

            return string.Empty;
        }

        private static bool TryGetString(JsonElement element, string propName, out string value)
        {
            value = string.Empty;
            if (!element.TryGetProperty(propName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                value = prop.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        public async Task<DtoFuncionario> GetFuncionarioAsync(string token, string identificacion)
        {
            try
            {
                var client = _factory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var url = _cfg["ApiSettings:ConsultaFuncionarios"] ?? "https://internalpip.policia.gov.co:8080/api/Icahu/FuncionarioPorIdentificacion";
                url = $"{url}{identificacion}";

                var resp = await client.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetFuncionario request failed: {StatusCode}", resp.StatusCode);
                    return new DtoFuncionario();
                }

                var json = await resp.Content.ReadAsStringAsync();
                var funcionario = TryExtractFuncionario(json);
                if (funcionario is null)
                {
                    _logger.LogWarning("GetFuncionario returned 200 but could not parse payload. Body: {Body}", json);
                    return new DtoFuncionario();
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee");
                return new DtoFuncionario();
            }
        }
        public async Task<DtoFuncionario> GetFuncionarioPersonajeMesAsync(string token, string identificacion)
        {
            try
            {
                var client = _factory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var url = _cfg["ApiSettings:ConsultaFuncionarios"] ?? "https://internalpip.policia.gov.co:8080/api/Icahu/FuncionarioPorIdentificacion";
                url = $"{url}{identificacion}";

                var resp = await client.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetFuncionarioPersonajeMes request failed: {StatusCode}", resp.StatusCode);
                    return new DtoFuncionario();
                }

                var json = await resp.Content.ReadAsStringAsync();
                var funcionario = TryExtractFuncionario(json);
                if (funcionario is null)
                {
                    _logger.LogWarning("GetFuncionarioPersonajeMes returned 200 but could not parse payload. Body: {Body}", json);
                    return new DtoFuncionario();
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee for PersonajeMes");
                return new DtoFuncionario();
            }
        }

        public async Task<string> GetFotoFuncionarioAsync(string token, string identificacion)
        {
            try
            {
                var client = _factory.CreateClient("AuthClient");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var baseUrl = "https://internalpip.policia.gov.co:8080/api/Icahu/ImagenFuncionarioB64";
                var url = $"{baseUrl}?_Identificacion={identificacion}";

                var resp = await client.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetFoto request failed: {StatusCode}", resp.StatusCode);
                    return string.Empty;
                }

                var raw = await resp.Content.ReadAsStringAsync();
                var foto = TryExtractFotoBase64(raw);
                if (string.IsNullOrWhiteSpace(foto))
                {
                    _logger.LogWarning("GetFoto returned 200 but no base64 could be extracted. Body: {Body}", raw);
                }
                return foto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photo");
                return string.Empty;
            }
        }

        private static DtoFuncionario? TryExtractFuncionario(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1) Formato directo
            var direct = SafeDeserialize<DtoFuncionario>(json, opts);
            if (HasFuncionarioData(direct))
            {
                return direct;
            }

            // 2) Formato envuelto: { estado, respuesta: { ... } }
            var wrapped = SafeDeserialize<DtoRespuesta<DtoFuncionario>>(json, opts);
            if (wrapped?.Respuesta is not null && HasFuncionarioData(wrapped.Respuesta))
            {
                return wrapped.Respuesta;
            }

            // 3) Formato envuelto con lista: { respuesta: [ { ... } ] }
            var wrappedList = SafeDeserialize<DtoRespuesta<List<DtoFuncionario>>>(json, opts);
            var first = wrappedList?.Respuesta?.FirstOrDefault();
            if (HasFuncionarioData(first))
            {
                return first;
            }

            // 4) Parse flexible con data/respuesta/Resultado/etc.
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (TryGetFuncionarioFromProperty(root, "respuesta", opts, out var fromRespuesta)) return fromRespuesta;
            if (TryGetFuncionarioFromProperty(root, "Respuesta", opts, out fromRespuesta)) return fromRespuesta;
            if (TryGetFuncionarioFromProperty(root, "data", opts, out fromRespuesta)) return fromRespuesta;
            if (TryGetFuncionarioFromProperty(root, "Data", opts, out fromRespuesta)) return fromRespuesta;
            if (TryGetFuncionarioFromProperty(root, "resultado", opts, out fromRespuesta)) return fromRespuesta;
            if (TryGetFuncionarioFromProperty(root, "Resultado", opts, out fromRespuesta)) return fromRespuesta;

            // 5) Búsqueda recursiva del objeto candidato en cualquier nivel.
            if (TryFindFuncionarioCandidate(root, 0, out var candidate))
            {
                var mapped = MapFlexibleFuncionario(candidate);
                if (HasFuncionarioData(mapped))
                {
                    return mapped;
                }
            }

            return null;
        }

        private static bool TryGetFuncionarioFromProperty(
            JsonElement root,
            string propertyName,
            JsonSerializerOptions opts,
            out DtoFuncionario? funcionario)
        {
            funcionario = null;
            if (!root.TryGetProperty(propertyName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.Object)
            {
                funcionario = SafeDeserialize<DtoFuncionario>(prop.GetRawText(), opts);
                var mapped = MapFlexibleFuncionario(prop);
                funcionario = MergeFuncionario(funcionario, mapped);
                return HasFuncionarioData(funcionario);
            }

            if (prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
            {
                var first = prop[0];
                if (first.ValueKind == JsonValueKind.Object)
                {
                    funcionario = SafeDeserialize<DtoFuncionario>(first.GetRawText(), opts);
                    var mapped = MapFlexibleFuncionario(first);
                    funcionario = MergeFuncionario(funcionario, mapped);
                    return HasFuncionarioData(funcionario);
                }
            }

            return false;
        }

        private static bool HasFuncionarioData(DtoFuncionario? f)
        {
            if (f is null) return false;
            return
                !string.IsNullOrWhiteSpace(f.identificacion) ||
                !string.IsNullOrWhiteSpace(f.nombres) ||
                !string.IsNullOrWhiteSpace(f.apellidos) ||
                !string.IsNullOrWhiteSpace(f.usuario) ||
                !string.IsNullOrWhiteSpace(f.correo);
        }

        private static DtoFuncionario MapFlexibleFuncionario(JsonElement obj)
        {
            var correoStr = Pick(obj, "correo", "Correo", "email", "Email", "correoElectronico", "CorreoElectronico", "mail", "Mail");
            var userStr = Pick(obj, "usuario", "Usuario", "username", "Username", "usuarioEmpresarial", "UsuarioEmpresarial");
            if (string.IsNullOrWhiteSpace(userStr) && !string.IsNullOrWhiteSpace(correoStr) && correoStr.Contains('@'))
            {
                userStr = correoStr.Split('@')[0].Trim();
            }

            return new DtoFuncionario
            {
                identificacion = Pick(obj, "identificacion", "Identificacion", "numeroDocumento", "NumeroDocumento", "documento", "Documento", "identidad", "IdFuncionario"),
                nombres = Pick(obj, "nombres", "Nombres", "nombre", "Nombre", "primerNombre", "PrimerNombre", "nombresFuncionario"),
                apellidos = Pick(obj, "apellidos", "Apellidos", "apellido", "Apellido", "primerApellido", "PrimerApellido", "apellidosFuncionario"),
                situacionLaboral = Pick(obj, "situacionLaboral", "SituacionLaboral", "situacion_laboral", "Situacion_Laboral", "situacion", "Situacion", "estadoLaboral", "EstadoLaboral"),
                correo = correoStr,
                usuario = userStr,
                celular = Pick(obj, "celular", "Celular", "numeroCelular", "NumeroCelular", "telefono", "Telefono", "telefonoMovil", "TelefonoMovil", "movil", "Movil"),
                dependencia = Pick(obj, "descripcionDependencia", "DescripcionDependencia"),
                siglaFisica = Pick(obj, "siglaFisica", "SiglaFisica", "siglaLaborando", "SiglaLaborando"),
                siglaLaborando = Pick(obj, "siglaLaborando", "SiglaLaborando"),
                siglaDependencia = Pick(obj, "siglaDependencia", "SiglaDependencia", "siglaDependenciaLaborando"),
                nombreGrado = Pick(obj, "nombreGrado", "NombreGrado", "grado", "Grado"),
                gradAlfabetico = Pick(obj, "gradAlfabetico", "GradAlfabetico", "gradoAlfabetico", "GradoAlfabetico", "siglaGrado", "SiglaGrado"),
                tiempoServicio = Pick(obj, "tiempoServicio", "TiempoServicio", "tiempo_servicio", "Tiempo_Servicio"),
                funcionario = Pick(obj, "funcionario", "Funcionario"),
                // En PIP "funcionario" llega como texto; para código usamos consecutivo/numerico.
                funcionarioCodigo = Pick(obj, "consecutivo", "Consecutivo", "numerico", "Numerico", "identificacion", "Identificacion"),
                // Código de unidad donde labora.
                undeLaborandoCodigo = Pick(obj, "undeConsecutivoLaborando", "UndeConsecutivoLaborando", "undeConsecutivo", "UndeConsecutivo", "tiunCodigo", "TiunCodigo"),
                // No viene "codigoCargo" explícito; usamos ordenamiento como fallback numérico.
                codigoCargo = Pick(obj, "codigoCargo", "CodigoCargo", "codigocargo", "idCargo", "IdCargo", "cargoCodigo", "CargoCodigo", "ordenamiento", "Ordenamiento"),
                cargo = Pick(obj, "cargo", "Cargo", "rolCargo", "RolCargo"),
                idCategoria = PickLong(obj, "idCategoria", "IdCategoria", "categoriaId", "CategoriaId", "idcategoria", "Idcategoria"),
                categoriaDescripcion = Pick(obj, "categoriaDescripcion", "CategoriaDescripcion", "descripcionCategoria", "DescripcionCategoria", "categoria", "Categoria"),
                activo = PickBool(obj, "activo", "Activo", "vigente", "Vigente")
            };
        }

        private static DtoFuncionario MergeFuncionario(DtoFuncionario? primary, DtoFuncionario fallback)
        {
            primary ??= new DtoFuncionario();

            primary.identificacion ??= fallback.identificacion;
            primary.nombres ??= fallback.nombres;
            primary.apellidos ??= fallback.apellidos;
            primary.situacionLaboral ??= fallback.situacionLaboral;
            primary.correo ??= fallback.correo;
            primary.usuario ??= fallback.usuario;
            primary.celular ??= fallback.celular;
            primary.dependencia ??= fallback.dependencia;
            primary.siglaFisica ??= fallback.siglaFisica;
            primary.siglaLaborando ??= fallback.siglaLaborando;
            primary.siglaDependencia ??= fallback.siglaDependencia;
            primary.funcionario ??= fallback.funcionario;
            primary.siglaLaborando ??= fallback.siglaLaborando;
            primary.nombreGrado ??= fallback.nombreGrado;
            primary.cargo ??= fallback.cargo;
            primary.gradAlfabetico ??= fallback.gradAlfabetico;
            primary.tiempoServicio ??= fallback.tiempoServicio;
            primary.funcionarioCodigo ??= fallback.funcionarioCodigo;
            primary.undeLaborandoCodigo ??= fallback.undeLaborandoCodigo;
            primary.codigoCargo ??= fallback.codigoCargo;
            primary.idCategoria ??= fallback.idCategoria;
            primary.categoriaDescripcion ??= fallback.categoriaDescripcion;

            return primary;
        }

        private static string? Pick(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryExtractStringProp(obj, n, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
            return null;
        }
        private static long? PickLong(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetLongProp(obj, n, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        private static bool PickBool(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
            {
                if (TryGetBoolProp(obj, n, out var value))
                {
                    return value;
                }
            }
            return true;
        }

        private static string TryExtractFotoBase64(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var trimmed = raw.Trim();

            // 1) String plano (puede venir como "....")
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                try
                {
                    trimmed = JsonSerializer.Deserialize<string>(trimmed) ?? string.Empty;
                }
                catch
                {
                    // dejar trimmed original
                }
            }

            if (LooksLikeBase64(trimmed))
            {
                return trimmed;
            }

            // 2) JSON envuelto: { respuesta: "..." } o { data: { foto: "..." } }
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (TryExtractStringProp(root, "respuesta", out var s) && LooksLikeBase64(s)) return s;
                if (TryExtractStringProp(root, "Respuesta", out s) && LooksLikeBase64(s)) return s;
                if (TryExtractStringProp(root, "foto", out s) && LooksLikeBase64(s)) return s;
                if (TryExtractStringProp(root, "Foto", out s) && LooksLikeBase64(s)) return s;

                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (TryExtractStringProp(data, "foto", out s) && LooksLikeBase64(s)) return s;
                    if (TryExtractStringProp(data, "Foto", out s) && LooksLikeBase64(s)) return s;
                    if (TryExtractStringProp(data, "respuesta", out s) && LooksLikeBase64(s)) return s;
                }
            }
            catch
            {
                // No JSON parseable
            }

            return string.Empty;
        }

        private static bool TryExtractStringProp(JsonElement root, string propName, out string value)
        {
            value = string.Empty;
            if (!TryGetPropertyIgnoreCase(root, propName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                value = prop.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }

            if (prop.ValueKind == JsonValueKind.Number)
            {
                value = prop.GetRawText();
                return !string.IsNullOrWhiteSpace(value);
            }

            if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
            {
                value = prop.GetBoolean() ? "true" : "false";
                return true;
            }

            return false;
        }

        private static bool TryGetBoolProp(JsonElement root, string propName, out bool value)
        {
            value = false;
            if (!TryGetPropertyIgnoreCase(root, propName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
            {
                value = prop.GetBoolean();
                return true;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var number))
            {
                value = number != 0;
                return true;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var raw = prop.GetString() ?? string.Empty;
                if (bool.TryParse(raw, out var parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                if (int.TryParse(raw, out var parsedInt))
                {
                    value = parsedInt != 0;
                    return true;
                }
            }

            return false;
        }
        private static bool TryGetLongProp(JsonElement root, string propName, out long value)
        {
            value = 0;
            if (!TryGetPropertyIgnoreCase(root, propName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var number))
            {
                value = number;
                return true;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var raw = (prop.GetString() ?? string.Empty).Trim();
                if (long.TryParse(raw, out var parsedLong))
                {
                    value = parsedLong;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement root, string propName, out JsonElement value)
        {
            foreach (var p in root.EnumerateObject())
            {
                if (string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private static bool LooksLikeBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 40)
            {
                return false;
            }

            // Acepta Base64 puro o data URL.
            if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return value.All(c =>
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '+' || c == '/' || c == '=' ||
                c == '-' || c == '_');
        }

        private static T? SafeDeserialize<T>(string json, JsonSerializerOptions opts)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, opts);
            }
            catch
            {
                return default;
            }
        }

        private static bool TryFindFuncionarioCandidate(JsonElement element, int depth, out JsonElement candidate)
        {
            candidate = default;
            if (depth > 8)
            {
                return false;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                if (LooksLikeFuncionarioObject(element))
                {
                    candidate = element;
                    return true;
                }

                foreach (var prop in element.EnumerateObject())
                {
                    if (TryFindFuncionarioCandidate(prop.Value, depth + 1, out candidate))
                    {
                        return true;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryFindFuncionarioCandidate(item, depth + 1, out candidate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool LooksLikeFuncionarioObject(JsonElement obj)
        {
            var keys = obj.EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();

            var score = 0;
            if (keys.Any(k => k.Contains("ident"))) score++;
            if (keys.Any(k => k.Contains("nombre"))) score++;
            if (keys.Any(k => k.Contains("apellido"))) score++;
            if (keys.Any(k => k.Contains("correo") || k.Contains("email") || k.Contains("mail"))) score++;
            if (keys.Any(k => k.Contains("usuario"))) score++;
            if (keys.Any(k => k.Contains("celular") || k.Contains("telefono") || k.Contains("movil"))) score++;
            if (keys.Any(k => k.Contains("dependencia") || k.Contains("unidad"))) score++;
            if (keys.Any(k => k.Contains("cargo"))) score++;

            return score >= 2;
        }

        public async Task<List<Comun.Dtos.Administracion.DtoUnidadOrg>> GetUnidadesPipAsync(CancellationToken ct = default)
        {
            var result = new List<Comun.Dtos.Administracion.DtoUnidadOrg>();
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:ConsultaUnidadesPip"] ?? "https://internalpip.policia.gov.co:8080/api/Icahu/Unidades?_vigente=SI";
                var resp = await client.GetAsync(url, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ConsultaUnidadesPip request failed with StatusCode={StatusCode}", resp.StatusCode);
                    return result;
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement arrayElem = default;
                if (root.TryGetProperty("respuesta", out var rElem) && rElem.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = rElem;
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = root;
                }

                if (arrayElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        var consecutivo = elem.TryGetProperty("consecutivo", out var cProp) && cProp.ValueKind == JsonValueKind.Number ? cProp.GetInt32() : 0;
                        var nombre = elem.TryGetProperty("descripcionDependencia", out var dProp) ? dProp.GetString() ?? string.Empty : string.Empty;
                        var sigla = elem.TryGetProperty("siglaFisica", out var sProp) ? sProp.GetString() ?? string.Empty :
                                    (elem.TryGetProperty("siglaDepende", out var sdProp) ? sdProp.GetString() ?? string.Empty : string.Empty);
                        var siglaPapa = elem.TryGetProperty("siglaPapa", out var spProp) ? spProp.GetString() : null;
                        var vigente = elem.TryGetProperty("vigente", out var vProp) ? vProp.GetString() ?? "SI" : "SI";
                        var direccion = elem.TryGetProperty("direccion", out var dirProp) ? dirProp.GetString() : null;

                        if (consecutivo > 0 && !string.IsNullOrWhiteSpace(nombre))
                        {
                            result.Add(new Comun.Dtos.Administracion.DtoUnidadOrg
                            {
                                Consecutivo = consecutivo,
                                Nombre = nombre,
                                Sigla = string.IsNullOrWhiteSpace(sigla) ? nombre : sigla,
                                SiglaPapa = siglaPapa,
                                Vigente = vigente,
                                Direccion = direccion
                            });
                        }
                    }
                }

                _logger.LogInformation("Se obtuvieron {Count} unidades institucionales desde PIP (Icahu/Unidades)", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar unidades en PIP API (Icahu/Unidades)");
            }

            return result;
        }

        public async Task<List<DtoSliderPublico>> GetSlidersPublicosAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default)
        {
            var result = new List<DtoSliderPublico>();
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:ConsultaSlidersPublicos"] ?? "https://internalpip.policia.gov.co:8080/api/Coest/Consulta_Sliders_Publicos";
                var imageBaseUrl = _cfg["ApiSettings:SliderImageBaseUrl"] ?? "http://srvsicoeapp1.policia.gov.co/api/Slider/Image/";

                var jsonPayload = JsonSerializer.Serialize(request);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Consultando sliders públicos en PIP ({Url}) con payload: {Payload}", url, jsonPayload);
                var resp = await client.PostAsync(url, content, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Consulta_Sliders_Publicos request failed. StatusCode={StatusCode}", resp.StatusCode);
                    return result;
                }

                var responseJson = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Respuesta recibida de Consulta_Sliders_Publicos: {Body}", responseJson);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                JsonElement arrayElem = default;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = root;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("respuesta", out var rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Respuesta", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("sliders", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Sliders", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                }

                if (arrayElem.ValueKind == JsonValueKind.Array)
                {
                    int index = 1;
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        var vigenteVal = PickLong(elem, "vigente", "Vigente", "activo", "Activo", "estado", "Estado");
                        if (vigenteVal.HasValue && vigenteVal.Value == 0)
                        {
                            continue;
                        }

                        var rawImg = Pick(elem, "urlImagen", "UrlImagen", "url_imagen", "Url_Imagen", "imagenUrl", "ImagenUrl", "imagen", "Imagen", "rutaImagen", "RutaImagen", "foto", "Foto", "path", "Path", "url", "Url", "archivo", "Archivo");
                        if (string.IsNullOrWhiteSpace(rawImg))
                        {
                            _logger.LogWarning("Elemento de slider omitido por no tener propiedad de imagen válida. Elemento JSON: {Elem}", elem.GetRawText());
                            continue;
                        }

                        string fullImgUrl;
                        if (rawImg.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                        {
                            fullImgUrl = rawImg;
                        }
                        else if (rawImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                 rawImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            if (rawImg.Contains("srvdockergusof.policia.gov.co", StringComparison.OrdinalIgnoreCase))
                            {
                                var fileName = Path.GetFileName(new Uri(rawImg).AbsolutePath);
                                var baseClean = imageBaseUrl.TrimEnd('/');
                                fullImgUrl = $"{baseClean}/{fileName}";
                            }
                            else
                            {
                                fullImgUrl = rawImg;
                            }
                        }
                        else
                        {
                            var baseClean = imageBaseUrl.TrimEnd('/');
                            var pathClean = rawImg.TrimStart('/');

                            if (baseClean.EndsWith("/api/Slider/Image", StringComparison.OrdinalIgnoreCase) &&
                                pathClean.StartsWith("api/Slider/Image/", StringComparison.OrdinalIgnoreCase))
                            {
                                pathClean = pathClean.Substring("api/Slider/Image/".Length);
                            }
                            else if (!baseClean.Contains("/api/Slider/Image", StringComparison.OrdinalIgnoreCase) &&
                                     !pathClean.StartsWith("api/Slider/Image/", StringComparison.OrdinalIgnoreCase))
                            {
                                baseClean = $"{baseClean}/api/Slider/Image";
                            }

                            fullImgUrl = $"{baseClean}/{pathClean}";
                        }

                        var sliderId = PickLong(elem, "idSlider", "IdSlider", "id_slider", "id", "Id") ?? index;
                        var titulo = Pick(elem, "titulo", "Titulo", "nombre", "Nombre", "descripcion", "Descripcion", "title", "Title");
                        var desc = Pick(elem, "subtitulo", "Subtitulo", "descripcion", "Descripcion", "detalle", "Detalle", "lead", "Lead");
                        var redirect = Pick(elem, "urlDestino", "UrlDestino", "url_destino", "Url_Destino", "redireccionUrl", "RedireccionUrl", "link", "Link", "url", "Url", "hipervinculo", "Hipervinculo", "enlace", "Enlace");
                        var orden = PickLong(elem, "orden", "Orden", "posicion", "Posicion", "secuencia", "Secuencia") ?? index;

                        result.Add(new DtoSliderPublico
                        {
                            Id = sliderId,
                            Titulo = titulo,
                            Descripcion = desc,
                            ImagenUrl = fullImgUrl,
                            RedireccionUrl = redirect,
                            Orden = (int)orden,
                            Activo = true
                        });
                        index++;
                    }
                }

                _logger.LogInformation("Se obtuvieron {Count} sliders públicos desde PIP", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando sliders públicos en PIP");
            }

            return result;
        }

        public async Task<List<DtoNoticia>> GetNoticiasInternasAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default)
        {
            var result = new List<DtoNoticia>();
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:ConsultaNoticiasInternas"] ?? "https://internalpip.policia.gov.co:8080/api/Coest/Consulta_Noticia_Interna";
                var imageBaseUrl = _cfg["ApiSettings:SliderImageBaseUrl"] ?? "http://srvdockergusof.policia.gov.co/sicoe/";

                var jsonPayload = JsonSerializer.Serialize(request);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Consultando noticias internas en PIP ({Url}) con payload: {Payload}", url, jsonPayload);
                var resp = await client.PostAsync(url, content, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Consulta_Noticia_Interna request failed. StatusCode={StatusCode}", resp.StatusCode);
                    return result;
                }

                var responseJson = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Respuesta recibida de Consulta_Noticia_Interna: {Body}", responseJson);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                JsonElement arrayElem = default;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = root;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("respuesta", out var rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Respuesta", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("noticias", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Noticias", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                }

                if (arrayElem.ValueKind == JsonValueKind.Array)
                {
                    int index = 1;
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        var vigenteVal = PickLong(elem, "vigente", "Vigente", "activo", "Activo", "estado", "Estado");
                        if (vigenteVal.HasValue && vigenteVal.Value == 0)
                        {
                            continue;
                        }

                        var idNoticia = PickLong(elem, "idNoticia", "IdNoticia", "id_noticia", "id", "Id") ?? index;
                        var unidad = Pick(elem, "unidad", "Unidad");
                        var titulo = Pick(elem, "titulo", "Titulo");
                        var seccion = Pick(elem, "seccion", "Seccion");
                        var ciudad = Pick(elem, "ciudad", "Ciudad");
                        var subtitulo = Pick(elem, "subtitulo", "Subtitulo");
                        var contenido = Pick(elem, "contenido", "Contenido");
                        var fechaCreacion = Pick(elem, "fechaCreacion", "FechaCreacion", "fecha_creacion");
                        var usuarioCreacion = Pick(elem, "usuarioCreacion", "UsuarioCreacion");
                        var maquinaCreacion = Pick(elem, "maquinaCreacion", "MaquinaCreacion");
                        var fechaModifica = Pick(elem, "fechaModifica", "FechaModifica");
                        var usuarioModifica = Pick(elem, "usuarioModifica", "UsuarioModifica");
                        var maquinaModifica = Pick(elem, "maquinaModifica", "MaquinaModifica");
                        var megusta = PickLong(elem, "megusta", "Megusta", "meGusta", "MeGusta") ?? 0;

                        var rawImg = Pick(elem, "imagenNoticia", "ImagenNoticia", "imagen_noticia", "imagen", "Imagen", "rutaImagen", "RutaImagen");
                        string? formattedImages = null;
                        if (!string.IsNullOrWhiteSpace(rawImg))
                        {
                            var parts = rawImg.Split('|').Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                            var fullParts = new List<string>();
                            foreach (var p in parts)
                            {
                                if (p.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                    p.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                    p.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                                {
                                    fullParts.Add(p);
                                }
                                else
                                {
                                    var baseClean = imageBaseUrl.TrimEnd('/');
                                    var pathClean = p.TrimStart('/');
                                    if (pathClean.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                                    {
                                        fullParts.Add($"{baseClean}/{pathClean}");
                                    }
                                    else
                                    {
                                        fullParts.Add($"{baseClean}/api/NoticiaUpload/Imagen/{pathClean}");
                                    }
                                }
                            }
                            formattedImages = string.Join("|", fullParts);
                        }

                        result.Add(new DtoNoticia
                        {
                            IdNoticia = idNoticia,
                            Unidad = unidad ?? string.Empty,
                            Titulo = titulo ?? string.Empty,
                            Seccion = seccion ?? "General",
                            Ciudad = ciudad ?? string.Empty,
                            ImagenNoticia = formattedImages,
                            Subtitulo = subtitulo,
                            Contenido = contenido,
                            Vigente = (int)(vigenteVal ?? 1),
                            FechaCreacion = fechaCreacion ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            UsuarioCreacion = usuarioCreacion ?? string.Empty,
                            MaquinaCreacion = maquinaCreacion,
                            FechaModifica = fechaModifica,
                            UsuarioModifica = usuarioModifica,
                            MaquinaModifica = maquinaModifica,
                            Megusta = (int)megusta
                        });
                        index++;
                    }
                }

                _logger.LogInformation("Se obtuvieron {Count} noticias internas desde PIP", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando noticias internas en PIP");
            }

            return result;
        }

        public async Task<bool> RegistrarLikeNoticiaAsync(DtoLikeNoticiaRequest request, CancellationToken ct = default)
        {
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:LikeNoticiaUrl"] ?? "https://internalpip.policia.gov.co:8080/api/Coest/Like_Noticia";
                var jsonPayload = JsonSerializer.Serialize(request);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando Like_Noticia a PIP ({Url}) con payload: {Payload}", url, jsonPayload);
                var resp = await client.PostAsync(url, content, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Like_Noticia request failed. StatusCode={StatusCode}", resp.StatusCode);
                    return false;
                }

                var responseJson = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Respuesta recibida de Like_Noticia: {Body}", responseJson);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar Like_Noticia a PIP");
                return false;
            }
        }

        public async Task<List<DtoModalVigente>> GetModalesVigentesAsync(DtoConsultaSliderPublicoRequest request, CancellationToken ct = default)
        {
            var result = new List<DtoModalVigente>();
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:ConsultaModalVigente"] ?? "https://internalpip.policia.gov.co:8080/api/Coest/Consulta_Modal_Vigente";
                var imageBaseUrl = _cfg["ApiSettings:SliderImageBaseUrl"] ?? "http://srvdockergusof.policia.gov.co/sicoe/";

                var jsonPayload = JsonSerializer.Serialize(request);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Consultando modales vigentes en PIP ({Url}) con payload: {Payload}", url, jsonPayload);
                var resp = await client.PostAsync(url, content, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Consulta_Modal_Vigente request failed. StatusCode={StatusCode}", resp.StatusCode);
                    return result;
                }

                var responseJson = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Respuesta recibida de Consulta_Modal_Vigente: {Body}", responseJson);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                JsonElement arrayElem = default;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = root;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("respuesta", out var rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Respuesta", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("modales", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Modales", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                }

                if (arrayElem.ValueKind == JsonValueKind.Array)
                {
                    int index = 1;
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        var vigenteVal = PickLong(elem, "vigente", "Vigente", "activo", "Activo", "estado", "Estado");
                        if (vigenteVal.HasValue && vigenteVal.Value == 0)
                        {
                            continue;
                        }

                        var idModal = PickLong(elem, "idModal", "IdModal", "id_modal", "id", "Id") ?? index;
                        var titulo = Pick(elem, "titulo", "Titulo", "nombre", "Nombre");
                        var subtitulo = Pick(elem, "subtitulo", "Subtitulo", "descripcion", "Descripcion");
                        var redirect = Pick(elem, "urlDestino", "UrlDestino", "url_destino", "redireccionUrl", "RedireccionUrl", "link", "Link", "url", "Url");
                        var orden = PickLong(elem, "orden", "Orden", "posicion", "Posicion") ?? index;

                        var rawImg = Pick(elem, "urlImagen", "UrlImagen", "url_imagen", "imagenUrl", "ImagenUrl", "imagen", "Imagen", "rutaImagen", "RutaImagen");
                        string? fullImgUrl = null;
                        if (!string.IsNullOrWhiteSpace(rawImg))
                        {
                            if (rawImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                rawImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                rawImg.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                            {
                                fullImgUrl = rawImg;
                            }
                            else
                            {
                                var baseClean = imageBaseUrl.TrimEnd('/');
                                var pathClean = rawImg.TrimStart('/');
                                fullImgUrl = $"{baseClean}/{pathClean}";
                            }
                        }

                        result.Add(new DtoModalVigente
                        {
                            IdModal = idModal,
                            Titulo = titulo,
                            Subtitulo = subtitulo,
                            ImagenUrl = fullImgUrl,
                            RedireccionUrl = redirect,
                            Orden = (int)orden,
                            Vigente = (int)(vigenteVal ?? 1)
                        });
                        index++;
                    }
                }

                _logger.LogInformation("Se obtuvieron {Count} modales vigentes desde PIP", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando modales vigentes en PIP");
            }

            return result;
        }

        public async Task<List<DtoRadioEmisora>> GetEmisorasAsync(DtoConsultaSliderPublicoRequest? request = null, CancellationToken ct = default)
        {
            var result = new List<DtoRadioEmisora>();
            try
            {
                var usuarioPip = Environment.GetEnvironmentVariable("ApiSettings__UsuarioPip") ?? _cfg["ApiSettings:UsuarioPip"];
                var clavePip = Environment.GetEnvironmentVariable("ApiSettings__ClavePip") ?? _cfg["ApiSettings:ClavePip"];

                if (string.IsNullOrWhiteSpace(usuarioPip) || string.IsNullOrWhiteSpace(clavePip))
                {
                    usuarioPip = "USRSW.NEWUPD";
                    clavePip = "HWV9Dq8N!*_X%zU%juz*";
                }

                var tokenResp = await ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                {
                    usuario = usuarioPip.Trim(),
                    clave = clavePip.Trim()
                });
                var token = tokenResp?.Respuesta ?? string.Empty;

                var client = _factory.CreateClient("AuthClient");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                var url = _cfg["ApiSettings:ConsultaEmisorasAll"] ?? "https://internalpip.policia.gov.co:8080/api/Coest/Consulta_Emisoras_All";
                var imageBaseUrl = _cfg["ApiSettings:SliderImageBaseUrl"] ?? "http://srvdockergusof.policia.gov.co/sicoe/";
                var siglaSistema = _cfg["ApiSettings:SiglaSistema"] ?? "GUSOF";
                var nombreSistema = _cfg["ApiSettings:NombreSistema"] ?? "OFTIC";

                var auditPayload = new
                {
                    FUENTE_TRANSACCION = string.IsNullOrWhiteSpace(request?.FUENTE_TRANSACCION) ? siglaSistema : request.FUENTE_TRANSACCION,
                    SISTEMA_INFORMACION = string.IsNullOrWhiteSpace(request?.SISTEMA_INFORMACION) ? nombreSistema : request.SISTEMA_INFORMACION,
                    IP_MAQUINA = string.IsNullOrWhiteSpace(request?.IP_MAQUINA) ? "127.0.0.1" : request.IP_MAQUINA,
                    USUARIO = string.IsNullOrWhiteSpace(request?.USUARIO) ? "INVITADO" : request.USUARIO
                };
                var jsonAudit = JsonSerializer.Serialize(auditPayload);

                _logger.LogInformation("Consultando emisoras de radio en PIP ({Url}) con payload dinámico: {Payload}", url, jsonAudit);
                
                using var postContent = new StringContent(jsonAudit, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(url, postContent, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("POST Consulta_Emisoras_All falló con status {StatusCode}. Intentando GET...", resp.StatusCode);
                    resp = await client.GetAsync(url, ct);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Consulta_Emisoras_All request failed. StatusCode={StatusCode}", resp.StatusCode);
                    return result;
                }

                var responseJson = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("Respuesta recibida de Consulta_Emisoras_All: {Body}", responseJson);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                JsonElement arrayElem = default;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    arrayElem = root;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("respuesta", out var rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Respuesta", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Data", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Resultado", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("emisoras", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                    else if (root.TryGetProperty("Emisoras", out rElem) && rElem.ValueKind == JsonValueKind.Array) arrayElem = rElem;
                }

                if (arrayElem.ValueKind == JsonValueKind.Array)
                {
                    int index = 1;
                    foreach (var elem in arrayElem.EnumerateArray())
                    {
                        var activoVal = PickLong(elem, "activo", "Activo", "vigente", "Vigente", "estado", "Estado");
                        if (activoVal.HasValue && activoVal.Value == 0)
                        {
                            continue;
                        }

                        var idEmisora = PickLong(elem, "idEmisora", "IdEmisora", "id_emisora", "id", "Id") ?? index;
                        var nombre = Pick(elem, "nombre", "Nombre", "titulo", "Titulo");
                        var streamUrl = Pick(elem, "streamUrl", "StreamUrl", "stream_url", "url", "Url");
                        var orden = PickLong(elem, "orden", "Orden", "posicion", "Posicion") ?? index;

                        var rawLogo = Pick(elem, "logoUrl", "LogoUrl", "logo_url", "logo", "Logo", "urlLogo", "UrlLogo");
                        string? fullLogoUrl = null;
                        if (!string.IsNullOrWhiteSpace(rawLogo))
                        {
                            if (rawLogo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                rawLogo.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                rawLogo.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                            {
                                fullLogoUrl = rawLogo;
                            }
                            else
                            {
                                var baseClean = imageBaseUrl.TrimEnd('/');
                                var pathClean = rawLogo.TrimStart('/');
                                fullLogoUrl = $"{baseClean}/{pathClean}";
                            }
                        }

                        result.Add(new DtoRadioEmisora
                        {
                            IdEmisora = idEmisora,
                            Nombre = nombre ?? $"Emisora {index}",
                            StreamUrl = streamUrl,
                            LogoUrl = fullLogoUrl,
                            Orden = (int)orden,
                            Activo = (int)(activoVal ?? 1)
                        });
                        index++;
                    }
                }

                _logger.LogInformation("Se obtuvieron {Count} emisoras de radio desde PIP", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando emisoras de radio en PIP");
            }

            return result;
        }
    }
}
