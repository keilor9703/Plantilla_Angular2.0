using Comun.Dtos;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Servicios.ApiInterfaz;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Servicios.Api
{
    public class ApiWebOud : IApiWebOud
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ApiWebOud> _logger;

       public ApiWebOud(HttpClient http, IConfiguration cfg, ILogger<ApiWebOud> logger)
        {
            _http = http;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<string> ConsultarFuncionarioAsync(string identificacion, CancellationToken ct)
        {
            var baseUrl = _cfg["ApiSettings:ConsultaFuncionarios"]!;
            var url = $"{baseUrl}{Uri.EscapeDataString(identificacion)}";

            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadAsStringAsync(ct);
        }

        public async Task<bool> ValidarCredencialesAsync(string usuario, string contrasena, CancellationToken ct)
        {
            var url = _cfg["ApiSettings:OudLogin"]?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("ApiSettings:OudLogin no configurado.");
                return false;
            }

            var normalizedUser = usuario?.Trim() ?? string.Empty;
            var normalizedPassword = contrasena ?? string.Empty;

            var payloadCandidates = new object[]
            {
                new { usuario = normalizedUser, clave = normalizedPassword },
                new { Usuario = normalizedUser, Clave = normalizedPassword },
                new { usuario = normalizedUser, contrasena = normalizedPassword }
            };

            foreach (var payloadObj in payloadCandidates)
            {
                var json = JsonSerializer.Serialize(payloadObj);
                try
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var resp = await _http.PostAsync(url, content, ct);
                    var body = await resp.Content.ReadAsStringAsync(ct);

                    if (resp.IsSuccessStatusCode && IsSuccessfulAuthResponse(body))
                    {
                        _logger.LogInformation("Autenticación exitosa en OUD para usuario: {Usuario}", normalizedUser);
                        return true;
                    }

                    _logger.LogWarning("Respuesta de OUD no satisfactoria para usuario {Usuario}. Status={StatusCode}, Body={Body}", normalizedUser, resp.StatusCode, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error conectando con servicio OUD ({Url}) para usuario {Usuario}", url, normalizedUser);
                }
            }

            return false;
        }

        private static bool IsSuccessfulAuthResponse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            var plain = body.Trim();
            if (bool.TryParse(plain, out var asBool))
            {
                return asBool;
            }

            if (plain.Equals("1") || plain.Equals("\"1\"")) return true;
            if (plain.Equals("0") || plain.Equals("\"0\"")) return false;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                return IsSuccessfulAuthResponse(root);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSuccessfulAuthResponse(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.True) return true;
            if (root.ValueKind == JsonValueKind.False) return false;

            if (root.ValueKind != JsonValueKind.Object) return false;

            // 1. Si contiene propiedad 'estado', verificar su valor explícito
            if (TryGetPropertyIgnoreCase(root, "estado", out var estadoElement))
            {
                bool isEstadoTrue = estadoElement.ValueKind == JsonValueKind.True ||
                                    estadoElement.ToString() == "1" ||
                                    estadoElement.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

                if (!isEstadoTrue)
                {
                    return false; // Si estado es false, la autenticación FALLÓ.
                }

                if (TryGetPropertyIgnoreCase(root, "respuesta", out var respElem))
                {
                    if (respElem.ValueKind == JsonValueKind.False || respElem.ToString() == "0" || respElem.ToString().Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }

            // 2. Si contiene propiedad 'success', verificar explícitamente
            if (TryGetPropertyIgnoreCase(root, "success", out var successElement))
            {
                bool isSuccessTrue = successElement.ValueKind == JsonValueKind.True ||
                                     successElement.ToString() == "1" ||
                                     successElement.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

                if (!isSuccessTrue)
                {
                    return false; // Si success es false, la autenticación FALLÓ.
                }
                return true;
            }

            // 3. Si contiene propiedad 'codigo', verificar que sea 1 o 200 (0 o -1 son FALLOS en PIP)
            if (TryGetPropertyIgnoreCase(root, "codigo", out var codigoElement) && codigoElement.ValueKind == JsonValueKind.Number)
            {
                int code = codigoElement.GetInt32();
                if (code != 1 && code != 200)
                {
                    return false; // En PIP/OUD, código 0 o != 1/200 es error/fallo.
                }

                if (TryGetPropertyIgnoreCase(root, "mensaje", out var msgElem))
                {
                    var msg = msgElem.GetString() ?? "";
                    if (msg.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("incorrect", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("fall", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("denegad", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("errad", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            // 4. Si contiene token / access_token no vacío
            if (TryGetPropertyIgnoreCase(root, "token", out var tokenElement) && HasMeaningfulValue(tokenElement))
            {
                return true;
            }

            if (TryGetPropertyIgnoreCase(root, "access_token", out tokenElement) && HasMeaningfulValue(tokenElement))
            {
                return true;
            }

            return false;
        }

        private static bool HasMeaningfulValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                return !string.IsNullOrWhiteSpace(value.GetString());
            }

            return value.ValueKind != JsonValueKind.Null &&
                   value.ValueKind != JsonValueKind.Undefined &&
                   value.ValueKind != JsonValueKind.False;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            if (element.TryGetProperty(name, out value))
            {
                return true;
            }

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
