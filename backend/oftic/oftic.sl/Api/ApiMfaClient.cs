using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Servicios.ApiInterfaz;

namespace Servicios.Api
{
    /// <summary>
    /// Cliente HTTP del servicio centralizado de doble autenticación (Api2FA).
    /// Configuración (appsettings):
    ///   "Api2FA": { "BaseUrl": "...", "Sigla": "SI2FA", "Secret": "&lt;secreto HMAC&gt;" }
    /// </summary>
    public class ApiMfaClient : IApiMfaClient
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;
        private readonly ILogger<ApiMfaClient> _logger;
        private readonly string _baseUrl;
        private readonly string _sigla;
        private readonly string _secret;

        public ApiMfaClient(HttpClient http, IConfiguration cfg, ILogger<ApiMfaClient> logger)
        {
            _http = http;
            _logger = logger;
            _baseUrl = (cfg["Api2FA:BaseUrl"] ?? string.Empty).TrimEnd('/');
            _sigla = string.IsNullOrWhiteSpace(cfg["Api2FA:Sigla"]) ? "SI2FA" : cfg["Api2FA:Sigla"]!.Trim();
            _secret = cfg["Api2FA:Secret"] ?? string.Empty;
        }

        public async Task<string?> ObtenerTokenAsync(string usuario, long identificacion, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_secret))
            {
                _logger.LogWarning("Api2FA no está configurado (BaseUrl/Secret). No se puede obtener token de servicio.");
                return null;
            }
            try
            {
                var fecha = DateTimeOffset.UtcNow;
                var nonce = Guid.NewGuid().ToString("N");
                var canonical = BuildCanonical(usuario, identificacion, _sigla, fecha, nonce);
                var signature = HmacSha256Hex(_secret, canonical);

                var body = new
                {
                    Usuario = (usuario ?? string.Empty).Trim(),
                    Identificacion = identificacion,
                    Sistema = _sigla,
                    SistemaOrigen = _sigla,
                    FechaUtc = fecha,
                    Nonce = nonce,
                    Signature = signature
                };

                using var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/Token/TokenSistema", body, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                var env = JsonSerializer.Deserialize<Envelope<string>>(json, JsonOpts);
                if (env != null && env.CodigoExito == 1 && !string.IsNullOrWhiteSpace(env.Data))
                    return env.Data;

                _logger.LogWarning("Api2FA TokenSistema no exitoso: {Msg}", env?.Mensaje);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo token de servicio de Api2FA");
                return null;
            }
        }

        public async Task<DtoMfaEstadoCliente> GetStateAsync(string bearer, long identificacion, string usuario, CancellationToken ct)
        {
            var r = new DtoMfaEstadoCliente();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"{_baseUrl}/api/ApiMfa/State?identificacion={identificacion}&usuario={Uri.EscapeDataString(usuario ?? string.Empty)}");
                req.Headers.Add("Authorization", "Bearer " + bearer);
                using var resp = await _http.SendAsync(req, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                var env = JsonSerializer.Deserialize<Envelope<StateData>>(json, JsonOpts);
                if (env != null && string.Equals(env.IsError, "false", StringComparison.OrdinalIgnoreCase) && env.Data != null)
                {
                    r.Exito = true;
                    r.MfaHabilitado = env.Data.MfaHabilitado;
                    r.RequireReenroll = env.Data.RequireReenroll;
                    r.BloqueoHasta = env.Data.BloqueoHasta;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando State en Api2FA");
            }
            return r;
        }

        public async Task<DtoMfaEnrolCliente> EnrollStartAsync(string bearer, long identificacion, string usuario, CancellationToken ct)
        {
            var r = new DtoMfaEnrolCliente();
            try
            {
                var body = new { Identificacion = identificacion, Usuario = usuario, SystemId = _sigla };
                var env = await PostAsync<EnrollData>(bearer, "/api/ApiMfa/EnrollStart", body, ct);
                if (env != null && env.CodigoExito == 1 && env.Data != null)
                {
                    r.Exito = true;
                    r.QrBase64 = env.Data.QrBase64 ?? string.Empty;
                    r.ManualKey = env.Data.ManualKey ?? string.Empty;
                    r.EnrollToken = env.Data.EnrollToken ?? string.Empty;
                }
                else r.Mensaje = env?.Mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EnrollStart de Api2FA");
            }
            return r;
        }

        public async Task<DtoMfaVerifyCliente> EnrollConfirmAsync(string bearer, long identificacion, string usuario, string enrollToken, string code, string ip, CancellationToken ct)
        {
            var body = new { Identificacion = identificacion, Usuario = usuario, EnrollToken = enrollToken, Code = code, IpMaquina = ip ?? string.Empty, UserAudit = identificacion };
            return await VerifyLikeAsync(bearer, "/api/ApiMfa/EnrollConfirm", body, ct);
        }

        public async Task<DtoMfaVerifyCliente> VerifyAsync(string bearer, long identificacion, string usuario, string code, string ip, bool rememberDevice, string? deviceId, CancellationToken ct)
        {
            var body = new
            {
                Identificacion = identificacion,
                Usuario = usuario,
                Code = code,
                RememberDevice = rememberDevice,
                DeviceId = deviceId ?? string.Empty,
                IpMaquina = ip ?? string.Empty,
                UserAudit = identificacion
            };
            return await VerifyLikeAsync(bearer, "/api/ApiMfa/Verify", body, ct);
        }

        public async Task<bool> IsTrustedAsync(string bearer, long identificacion, string usuario, string deviceId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return false;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"{_baseUrl}/api/ApiMfa/IsTrusted?identificacion={identificacion}&usuario={Uri.EscapeDataString(usuario ?? string.Empty)}&deviceId={Uri.EscapeDataString(deviceId)}");
                req.Headers.Add("Authorization", "Bearer " + bearer);
                using var resp = await _http.SendAsync(req, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                var env = JsonSerializer.Deserialize<Envelope<int>>(json, JsonOpts);
                return env != null && env.Data == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando IsTrusted en Api2FA");
                return false;
            }
        }

        public async Task<DtoMfaVerifyCliente> ResetRequestAsync(string bearer, long identificacion, string usuario, string ip, CancellationToken ct)
        {
            var body = new { Identificacion = identificacion, Usuario = usuario, IpMaquina = ip ?? string.Empty, UserAudit = identificacion, Sistema = _sigla };
            return await ResetLikeAsync(bearer, "/api/ApiMfa/ResetRequest", body, ct);
        }

        public async Task<DtoMfaVerifyCliente> ResetConfirmAsync(string bearer, long identificacion, string usuario, string code, string ip, CancellationToken ct)
        {
            var body = new { Identificacion = identificacion, Usuario = usuario, Code = code, IpMaquina = ip ?? string.Empty, UserAudit = identificacion, Sistema = _sigla };
            return await ResetLikeAsync(bearer, "/api/ApiMfa/ResetConfirm", body, ct);
        }

        public async Task<DtoMfaVerifyCliente> ResetExecuteAsync(string bearer, long identificacion, string usuario, string ip, CancellationToken ct)
        {
            var body = new { Identificacion = identificacion, Usuario = usuario, IpMaquina = ip ?? string.Empty, UserAudit = identificacion, Sistema = _sigla };
            return await ResetLikeAsync(bearer, "/api/ApiMfa/ResetExecute", body, ct);
        }

        private async Task<DtoMfaVerifyCliente> ResetLikeAsync(string bearer, string ruta, object body, CancellationToken ct)
        {
            var r = new DtoMfaVerifyCliente();
            try
            {
                var env = await PostAsync<object>(bearer, ruta, body, ct);
                r.Ok = env != null && (env.CodigoExito == 1 || string.Equals(env.IsError, "false", StringComparison.OrdinalIgnoreCase));
                r.Mensaje = env?.Mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en {Ruta} de Api2FA", ruta);
            }
            return r;
        }

        private async Task<DtoMfaVerifyCliente> VerifyLikeAsync(string bearer, string ruta, object body, CancellationToken ct)
        {
            var r = new DtoMfaVerifyCliente();
            try
            {
                var env = await PostAsync<VerifyData>(bearer, ruta, body, ct);
                if (env?.Data != null) { r.Ok = env.Data.Ok; r.BloqueoHasta = env.Data.BloqueoHasta; }
                r.Mensaje = env?.Mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en {Ruta} de Api2FA", ruta);
            }
            return r;
        }

        private async Task<Envelope<T>?> PostAsync<T>(string bearer, string ruta, object body, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{ruta}")
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add("Authorization", "Bearer " + bearer);
            using var resp = await _http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<Envelope<T>>(json, JsonOpts);
        }

        // ---- firma HMAC (debe coincidir con TokenController.BuildCanonical del Api2FA) ----
        private static string BuildCanonical(string usuario, long identificacion, string sistema, DateTimeOffset fechaUtc, string nonce)
        {
            usuario = (usuario ?? string.Empty).Trim();
            sistema = (sistema ?? string.Empty).Trim();
            nonce = (nonce ?? string.Empty).Trim();
            var fecha = fechaUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            return $"{usuario}|{identificacion}|{sistema}|{fecha}|{nonce}";
        }

        private static string HmacSha256Hex(string secret, string message)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash);
        }

        // ---- envoltorios de respuesta de Api2FA ----
        private class Envelope<T>
        {
            public string? IsError { get; set; }
            public long CodigoExito { get; set; }
            public int CodigoError { get; set; }
            public string? Mensaje { get; set; }
            public string? Operacion { get; set; }
            public T? Data { get; set; }
        }
        private class StateData
        {
            public int MfaHabilitado { get; set; }
            public int RequireReenroll { get; set; }
            public DateTime? BloqueoHasta { get; set; }
            public int IntentosFallidos { get; set; }
            public string? UsuarioInstitucional { get; set; }
        }
        private class EnrollData
        {
            public string? QrBase64 { get; set; }
            public string? ManualKey { get; set; }
            public string? EnrollToken { get; set; }
        }
        private class VerifyData
        {
            public bool Ok { get; set; }
            public DateTime? BloqueoHasta { get; set; }
        }
    }
}
