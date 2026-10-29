using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Comun.Dtos;
using Negocio.Gestion;
using Negocio.Interfaz;
using Datos.Interfaz;
using Servicios.ApiInterfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Api.Services;

namespace ofic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentaController : ControllerBase
    {
        private readonly IApiWebOud _apiWebOud;
        private readonly IApiWebToken _apiWebToken;
        private readonly IJwtService _jwtService;
        private readonly IDbAuthRepository _dbAuthRepository;
        private readonly IDbUsuarioRepository _dbUsuarioRepository;
        private readonly ILogger<CuentaController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IMfaCentralService _mfa;
        private readonly MfaSessionTokenService _mfaSession;
        private readonly HashSet<long> _impersonationAllowedUserIds;

        public CuentaController(
            IApiWebOud apiWebOud,
            IApiWebToken apiWebToken,
            IJwtService jwtService,
            IDbAuthRepository dbAuthRepository,
            IDbUsuarioRepository dbUsuarioRepository,
            ILogger<CuentaController> logger,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IMfaCentralService mfa,
            MfaSessionTokenService mfaSession)
        {
            _apiWebOud = apiWebOud;
            _apiWebToken = apiWebToken;
            _jwtService = jwtService;
            _dbAuthRepository = dbAuthRepository;
            _dbUsuarioRepository = dbUsuarioRepository;
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _mfa = mfa;
            _mfaSession = mfaSession;
            _impersonationAllowedUserIds = ResolveImpersonationAllowedUserIds(configuration);
        }

        [HttpPost("Token")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> GetToken([FromBody] DtoTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Usuario}", request.Usuario);

                if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Contrasena))
                {
                    return BadRequest(new DtoTokenResponse 
                    { 
                        success = false, 
                        message = "Usuario y contraseña requeridos" 
                    });
                }

                var bypassValidationInDev = _env.IsDevelopment() && _configuration.GetValue<bool>("Auth:BypassOudPasswordValidation");

                if (bypassValidationInDev)
                {
                    _logger.LogWarning("¡ADVERTENCIA DE SEGURIDAD! La validación de contraseña OUD está desactivada explícitamente en desarrollo (Auth:BypassOudPasswordValidation=true) para usuario: {Usuario}", request.Usuario);
                }
                else
                {
                    // 1. Validación estricta de credenciales en OUD / PIP
                    var externalOk = await _apiWebOud.ValidarCredencialesAsync(
                        request.Usuario,
                        request.Contrasena,
                        CancellationToken.None);

                    if (!externalOk)
                    {
                        _logger.LogWarning("Validación OUD fallida para usuario: {Usuario}", request.Usuario);
                        return Unauthorized(new DtoTokenResponse
                        {
                            success = false,
                            message = "Usuario o contraseña incorrectos"
                        });
                    }
                }

                // 2. Obtener idUsuario y roles desde la BD local
                var (idUsuario, roles) = await _dbAuthRepository.GetUsuarioYRolesAsync(request.Usuario, CancellationToken.None);

                _logger.LogInformation("User {Usuario} - idUsuario: {id}, roles: {roles}", request.Usuario, idUsuario, roles?.Count);

                if (idUsuario is null)
                {
                    // Auto-aprovisionar usuario autenticado consultando sus datos en PIP
                    DtoFuncionario? pipFunc = null;
                    try
                    {
                        var tokenResp = await _apiWebToken.ObtenerTokenPipAsync(CancellationToken.None);
                        var tokenPip = tokenResp?.Respuesta ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(tokenPip))
                        {
                            pipFunc = await _apiWebToken.GetFuncionarioAsync(tokenPip, request.Usuario.Trim());
                        }
                    }
                    catch (Exception exPip)
                    {
                        _logger.LogWarning(exPip, "No se pudo consultar PIP para auto-aprovisionar a {Usuario}", request.Usuario);
                    }

                    if (pipFunc != null && (!string.IsNullOrWhiteSpace(pipFunc.identificacion) || !string.IsNullOrWhiteSpace(pipFunc.nombres)))
                    {
                        if (string.IsNullOrWhiteSpace(pipFunc.usuario))
                        {
                            pipFunc.usuario = request.Usuario.Trim();
                        }
                        idUsuario = await _dbUsuarioRepository.EnsureUsuarioExistsAsync(pipFunc, CancellationToken.None);
                    }
                    else
                    {
                        idUsuario = await _dbUsuarioRepository.EnsureUsuarioExistsAsync(request.Usuario.Trim(), request.Usuario.Trim(), CancellationToken.None);
                    }
                    roles = new List<long> { 1 };
                }

                if (idUsuario is null || idUsuario <= 0)
                {
                    _logger.LogWarning("No fue posible resolver ni crear el idUsuario local para {Usuario}", request.Usuario);
                    return StatusCode(500, new DtoTokenResponse
                    {
                        success = false,
                        message = "No fue posible resolver la cuenta de usuario en la base de datos."
                    });
                }

                var rolesFinal = roles ?? new List<long> { 1 };

                // 3. Gate de segundo factor (estándar institucional @policia/mfa). Si está
                //    habilitado y el equipo no es confiable, no se emite el JWT aún: se devuelve
                //    el reto MFA (enroll/verify/blocked/svcdown) para que el frontend muestre la modal.
                if (MfaEnabled())
                {
                    var ident = await ResolverIdentificacionAsync(request.Usuario!, idUsuario.Value, CancellationToken.None);

                    // Dispositivo confiable → se omite el segundo factor.
                    if (!string.IsNullOrWhiteSpace(request.DeviceId) &&
                        await _mfa.IsTrustedAsync(ident, request.Usuario!, request.DeviceId!, CancellationToken.None))
                    {
                        var jwtTrusted = _jwtService.CreateToken(idUsuario.Value, request.Usuario!, rolesFinal);
                        return Ok(new DtoTokenResponse { token = jwtTrusted, usuario = request.Usuario, success = true, message = "Login exitoso" });
                    }

                    var estado = await _mfa.GetStateAsync(ident, request.Usuario!, CancellationToken.None);

                    // Servicio 2FA caído → svcdown (el segundo factor es obligatorio, no se omite).
                    if (estado.ServiceDown)
                        return Ok(BuildMfaChallenge(idUsuario.Value, request.Usuario!, ident, rolesFinal, ClientIp(), "svcdown"));

                    if (estado.BloqueoHasta.HasValue && estado.BloqueoHasta.Value > DateTime.Now)
                        return Ok(BuildMfaChallenge(idUsuario.Value, request.Usuario!, ident, rolesFinal, ClientIp(), "blocked",
                            bloqueoHasta: estado.BloqueoHasta.Value.ToString("yyyy-MM-dd HH:mm:ss")));

                    var enrolado = estado.MfaHabilitado == 1 && estado.RequireReenroll == 0;
                    if (enrolado)
                        return Ok(BuildMfaChallenge(idUsuario.Value, request.Usuario!, ident, rolesFinal, ClientIp(), "verify"));

                    // No enrolado → iniciar enrolamiento y devolver el QR.
                    var enroll = await _mfa.EnrollStartAsync(ident, request.Usuario!, CancellationToken.None);
                    if (!enroll.Ok)
                        return Ok(BuildMfaChallenge(idUsuario.Value, request.Usuario!, ident, rolesFinal, ClientIp(), "svcdown"));

                    return Ok(BuildMfaChallenge(idUsuario.Value, request.Usuario!, ident, rolesFinal, ClientIp(), "enroll",
                        qrBase64: enroll.QrBase64, manualKey: enroll.ManualKey, enrollToken: enroll.EnrollToken));
                }

                // 3-bis. Sin segundo factor: se emite el JWT directamente.
                var jwtToken = _jwtService.CreateToken(idUsuario.Value, request.Usuario, rolesFinal);

                return Ok(new DtoTokenResponse
                {
                    token = jwtToken,
                    usuario = request.Usuario,
                    success = true,
                    message = "Login exitoso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para usuario {Usuario}: {Message}", request?.Usuario, ex.Message);
                return StatusCode(500, new DtoTokenResponse 
                { 
                    success = false, 
                    message = $"Error al iniciar sesión: {ex.Message}" 
                });
            }
        }
        [HttpPost("Impersonate")]
        [Authorize]
        public async Task<IActionResult> Impersonate([FromBody] DtoImpersonateRequest request)
        {
            try
            {
                var targetUser = (request?.Usuario ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(targetUser))
                {
                    return BadRequest(new DtoTokenResponse
                    {
                        success = false,
                        message = "Usuario objetivo requerido."
                    });
                }

                var actorRawId = User.FindFirstValue("id_usuario")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("nameid");

                if (!long.TryParse(actorRawId, out var actorId) || actorId <= 0)
                {
                    return Unauthorized(new DtoTokenResponse
                    {
                        success = false,
                        message = "No se pudo resolver el usuario autenticado."
                    });
                }

                if (!CanImpersonate(actorId, User))
                {
                    return StatusCode(403, new DtoTokenResponse
                    {
                        success = false,
                        message = "No tiene permisos para impersonar usuarios."
                    });
                }

                var actorUser = User.Identity?.Name?.Trim()
                    ?? User.FindFirstValue(ClaimTypes.Name)?.Trim()
                    ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(actorUser) &&
                    string.Equals(actorUser, targetUser, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new DtoTokenResponse
                    {
                        success = false,
                        message = "No puede impersonar su propio usuario."
                    });
                }

                var (targetId, targetRoles) = await _dbAuthRepository.GetUsuarioYRolesAsync(targetUser, HttpContext.RequestAborted);
                if (targetId is null || targetId <= 0)
                {
                    return NotFound(new DtoTokenResponse
                    {
                        success = false,
                        message = "Usuario objetivo no encontrado o inactivo."
                    });
                }

                var token = _jwtService.CreateImpersonationToken(
                    targetId.Value,
                    targetUser,
                    targetRoles ?? new List<long>(),
                    actorId,
                    actorUser);

                _logger.LogInformation(
                    "Impersonacion iniciada. actorId={ActorId}, actorUser={ActorUser}, targetId={TargetId}, targetUser={TargetUser}",
                    actorId, actorUser, targetId.Value, targetUser);

                return Ok(new DtoTokenResponse
                {
                    token = token,
                    usuario = targetUser,
                    success = true,
                    impersonating = true,
                    impersonatedBy = actorUser,
                    message = "Impersonación iniciada correctamente."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando impersonación");
                return StatusCode(500, new DtoTokenResponse
                {
                    success = false,
                    message = "Error iniciando impersonación."
                });
            }
        }

        [HttpPost("Refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public IActionResult Refresh([FromBody] DtoRefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Token))
                {
                    return BadRequest(new DtoTokenResponse { success = false, message = "Token requerido" });
                }

                var newToken = _jwtService.RefreshToken(request.Token);
                if (newToken == null)
                {
                    return Unauthorized(new DtoTokenResponse { success = false, message = "Token inválido o expirado" });
                }

                return Ok(new DtoTokenResponse
                {
                    token = newToken,
                    success = true,
                    message = "Token renovado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new DtoTokenResponse { success = false, message = "Error al renovar token" });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ENDPOINTS MFA (estándar institucional @policia/mfa) — requieren MfaSessionToken
        // ════════════════════════════════════════════════════════════════════════

        [HttpPost("MfaVerify")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaVerify([FromBody] DtoMfaVerifyRequest req, CancellationToken ct)
        {
            var d = _mfaSession.ValidateToken(req.MfaSessionToken);
            if (d is null)
                return Unauthorized(new DtoMfaStepResponse { Success = false, Message = "Sesión MFA expirada o inválida. Inicie sesión nuevamente." });
            if (string.IsNullOrWhiteSpace(req.Code))
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Ingrese el código de 6 dígitos." });

            var result = await _mfa.VerifyAsync(d.Identificacion, d.Usuario, req.Code, req.RememberDevice, req.DeviceId, ClientIp(), ct);
            if (result.ServiceDown)
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Servicio 2FA no disponible." });
            if (result.Blocked)
                return Ok(new DtoMfaStepResponse { Success = false, Message = $"Cuenta bloqueada temporalmente hasta {result.BloqueoHasta?.ToString("yyyy-MM-dd HH:mm:ss") ?? "próximamente"}.", BloqueoHasta = result.BloqueoHasta?.ToString("yyyy-MM-dd HH:mm:ss") });
            if (!result.Ok)
                return Ok(new DtoMfaStepResponse { Success = false, Message = result.Mensaje.Length > 0 ? result.Mensaje : "Código inválido. Verifique su app de autenticación." });

            return Ok(new DtoMfaStepResponse { Success = true, Token = BuildFinalJwt(d), Message = "Autenticación completada." });
        }

        [HttpPost("MfaEnrollConfirm")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaEnrollConfirm([FromBody] DtoMfaEnrollConfirmRequest req, CancellationToken ct)
        {
            var d = _mfaSession.ValidateToken(req.MfaSessionToken);
            if (d is null)
                return Unauthorized(new DtoMfaStepResponse { Success = false, Message = "Sesión MFA expirada. Inicie sesión nuevamente." });
            if (string.IsNullOrWhiteSpace(req.Code))
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Ingrese el código de 6 dígitos." });

            var result = await _mfa.EnrollConfirmAsync(d.Identificacion, d.Usuario, req.EnrollToken, req.Code, ClientIp(), ct);
            if (result.ServiceDown)
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Servicio 2FA no disponible." });
            if (result.Blocked)
                return Ok(new DtoMfaStepResponse { Success = false, Message = $"Cuenta bloqueada hasta {result.BloqueoHasta?.ToString("yyyy-MM-dd HH:mm:ss") ?? "próximamente"}.", BloqueoHasta = result.BloqueoHasta?.ToString("yyyy-MM-dd HH:mm:ss") });
            if (!result.Ok)
                return Ok(new DtoMfaStepResponse { Success = false, Message = result.Mensaje.Length > 0 ? result.Mensaje : "Código inválido. Intente de nuevo." });

            return Ok(new DtoMfaStepResponse { Success = true, Token = BuildFinalJwt(d), Message = "MFA activado y sesión iniciada correctamente." });
        }

        [HttpPost("MfaResetRequest")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaResetRequest([FromBody] DtoMfaResetRequest req, CancellationToken ct)
        {
            var d = _mfaSession.ValidateToken(req.MfaSessionToken);
            if (d is null)
                return Unauthorized(new DtoMfaStepResponse { Success = false, Message = "Sesión expirada. Inicie sesión nuevamente." });

            var result = await _mfa.ResetRequestAsync(d.Identificacion, d.Usuario, ClientIp(), ct);
            if (result.ServiceDown)
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Servicio 2FA no disponible." });

            // IsInfo=true → el frontend muestra el mensaje informativo sin cerrar la modal.
            return Ok(new DtoMfaStepResponse { Success = result.Ok, IsInfo = result.Ok, Message = result.Mensaje });
        }

        [HttpPost("MfaResetConfirm")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaResetConfirm([FromBody] DtoMfaResetConfirmRequest req, CancellationToken ct)
        {
            var d = _mfaSession.ValidateToken(req.MfaSessionToken);
            if (d is null)
                return Unauthorized(new DtoMfaStepResponse { Success = false, Message = "Sesión expirada. Inicie sesión nuevamente." });
            if (string.IsNullOrWhiteSpace(req.Code))
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Ingrese el código enviado al correo." });

            var ip = ClientIp();
            var confirm = await _mfa.ResetConfirmAsync(d.Identificacion, d.Usuario, req.Code, ip, ct);
            if (confirm.ServiceDown)
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Servicio 2FA no disponible." });
            if (!confirm.Ok)
                return Ok(new DtoMfaStepResponse { Success = false, Message = confirm.AvailableAt.HasValue ? $"Aún no disponible. Podrá reenrolar desde las {confirm.AvailableAt.Value:HH:mm:ss}." : confirm.Mensaje });

            var exec = await _mfa.ResetExecuteAsync(d.Identificacion, d.Usuario, ip, ct);
            if (!exec.Ok)
                return Ok(new DtoMfaStepResponse { Success = false, Message = exec.Mensaje });

            var enroll = await _mfa.EnrollStartAsync(d.Identificacion, d.Usuario, ct);
            if (!enroll.Ok)
                return Ok(new DtoMfaStepResponse { Success = false, Message = "Se restableció el MFA pero no fue posible generar el nuevo QR. Inicie sesión." });

            return Ok(new DtoMfaStepResponse { Success = true, Message = "MFA restablecido. Escanee el nuevo código QR para activar su autenticador.", QrBase64 = enroll.QrBase64, ManualKey = enroll.ManualKey, EnrollToken = enroll.EnrollToken });
        }

        // ===================== Helpers MFA =====================
        private bool MfaEnabled() => _configuration.GetValue<bool>("Mfa:Enabled");

        private string ClientIp()
        {
            var fwd = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(fwd)) return fwd.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        private DtoMfaLoginResponse BuildMfaChallenge(long idUsuario, string usuario, long identificacion, List<long> roles, string ip, string mfaMode,
            string? qrBase64 = null, string? manualKey = null, string? enrollToken = null, string? bloqueoHasta = null)
        {
            var sessionData = new MfaSessionTokenService.MfaSessionData(
                IdUsuario: idUsuario, Usuario: usuario, Identificacion: identificacion,
                RolesJson: JsonSerializer.Serialize(roles ?? new List<long> { 1 }), Ip: ip);

            return new DtoMfaLoginResponse
            {
                success = false,
                RequiresMfa = true,
                MfaMode = mfaMode,
                MfaSessionToken = _mfaSession.CreateToken(sessionData),
                MfaQrBase64 = qrBase64,
                MfaManualKey = manualKey,
                MfaEnrollToken = enrollToken,
                BloqueoHasta = bloqueoHasta,
                message = mfaMode == "svcdown" ? "El servicio de doble autenticación no está disponible."
                        : mfaMode == "blocked" ? "Cuenta bloqueada temporalmente."
                        : "Se requiere segundo factor de autenticación."
            };
        }

        private string BuildFinalJwt(MfaSessionTokenService.MfaSessionData d)
            => _jwtService.CreateToken(d.IdUsuario, d.Usuario, ParseRoles(d.RolesJson));

        private static List<long> ParseRoles(string rolesJson)
        {
            try { return JsonSerializer.Deserialize<List<long>>(rolesJson) ?? new List<long> { 1 }; }
            catch { return new List<long> { 1 }; }
        }

        private async Task<long> ResolverIdentificacionAsync(string usuario, long idUsuario, CancellationToken ct)
        {
            try
            {
                var ident = await _dbAuthRepository.GetIdentificacionAsync(usuario, ct);
                if (!string.IsNullOrWhiteSpace(ident) && long.TryParse(ident.Trim(), out var n) && n > 0)
                    return n;
            }
            catch { /* respaldo abajo */ }
            return idUsuario;
        }

        private bool CanImpersonate(long userId, ClaimsPrincipal? user)
        {
            if (_impersonationAllowedUserIds.Contains(userId))
            {
                return true;
            }

            var roleClaims = user?.FindAll(ClaimTypes.Role).Select(x => x.Value)
                ?? Enumerable.Empty<string>();

            // Permitir al rol 1 (Super administrador) impersonar usuarios.
            return roleClaims.Any(r => string.Equals(r?.Trim(), "1", StringComparison.Ordinal));
        }

        private static HashSet<long> ResolveImpersonationAllowedUserIds(IConfiguration configuration)
        {
            var ids = new HashSet<long> { 1 };
            AddIds(ids, configuration["Auth:ImpersonationAllowedUserIds"]);
            AddIds(ids, configuration["Menu:SuperUserIds"]);
            return ids;
        }

        private static void AddIds(HashSet<long> ids, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var values = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var value in values)
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
                {
                    ids.Add(id);
                }
            }
        }
    }
}
