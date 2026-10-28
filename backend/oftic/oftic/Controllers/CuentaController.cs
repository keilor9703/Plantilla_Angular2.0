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
        private readonly IApiMfaClient _apiMfa;
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
            IApiMfaClient apiMfa)
        {
            _apiWebOud = apiWebOud;
            _apiWebToken = apiWebToken;
            _jwtService = jwtService;
            _dbAuthRepository = dbAuthRepository;
            _dbUsuarioRepository = dbUsuarioRepository;
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _apiMfa = apiMfa;
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

                // 3. Gate de segundo factor (SI2FA). Si está habilitado, no se emite el JWT aún:
                //    se exige el código del segundo factor (enrolamiento la primera vez).
                if (_configuration.GetValue<bool>("Mfa:Habilitado"))
                {
                    var ident = await ResolverIdentificacionAsync(request.Usuario, idUsuario.Value, CancellationToken.None);

                    var svcToken = await _apiMfa.ObtenerTokenAsync(request.Usuario, ident, CancellationToken.None);
                    if (string.IsNullOrWhiteSpace(svcToken))
                    {
                        // El segundo factor es obligatorio: si el servicio 2FA no responde, NO se omite.
                        return StatusCode(503, new DtoTokenResponse
                        {
                            success = false,
                            message = "El servicio de doble autenticación no está disponible. Intente más tarde."
                        });
                    }

                    var estado = await _apiMfa.GetStateAsync(svcToken, ident, request.Usuario, CancellationToken.None);
                    var challenge = CrearChallenge(idUsuario.Value, request.Usuario, rolesFinal, ident);

                    if (!estado.Enrolado)
                    {
                        var enrol = await _apiMfa.EnrollStartAsync(svcToken, ident, request.Usuario, CancellationToken.None);
                        if (!enrol.Exito)
                        {
                            return StatusCode(503, new DtoTokenResponse
                            {
                                success = false,
                                message = "No fue posible iniciar el enrolamiento del segundo factor."
                            });
                        }
                        return Ok(new DtoTokenResponse
                        {
                            success = true,
                            usuario = request.Usuario,
                            requiere2Fa = true,
                            enrolar = true,
                            challenge = challenge,
                            qr = enrol.QrBase64,
                            claveManual = enrol.ManualKey,
                            enrollToken = enrol.EnrollToken,
                            message = "Debe enrolar su segundo factor de autenticación."
                        });
                    }

                    return Ok(new DtoTokenResponse
                    {
                        success = true,
                        usuario = request.Usuario,
                        requiere2Fa = true,
                        enrolar = false,
                        challenge = challenge,
                        message = "Ingrese el código de verificación."
                    });
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

        [HttpPost("Mfa/Verify")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaVerify([FromBody] DtoMfaVerifyLoginReq req)
        {
            var ch = LeerChallenge(req?.Challenge);
            if (ch == null)
                return Unauthorized(new DtoTokenResponse { success = false, message = "Sesión de verificación inválida o expirada. Inicie sesión de nuevo." });
            if (string.IsNullOrWhiteSpace(req?.Codigo))
                return BadRequest(new DtoTokenResponse { success = false, message = "Código requerido." });

            var svcToken = await _apiMfa.ObtenerTokenAsync(ch.Usuario, ch.Ident, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(svcToken))
                return StatusCode(503, new DtoTokenResponse { success = false, message = "El servicio de doble autenticación no está disponible." });

            var r = await _apiMfa.VerifyAsync(svcToken, ch.Ident, ch.Usuario, req.Codigo.Trim(), ObtenerIp(), CancellationToken.None);
            if (!r.Ok)
                return Unauthorized(new DtoTokenResponse
                {
                    success = false,
                    message = r.BloqueoHasta.HasValue
                        ? "Cuenta bloqueada temporalmente por intentos fallidos."
                        : "Código inválido."
                });

            return Ok(EmitirTokenFinal(ch));
        }

        [HttpPost("Mfa/EnrollConfirm")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaEnrollConfirm([FromBody] DtoMfaEnrollConfirmLoginReq req)
        {
            var ch = LeerChallenge(req?.Challenge);
            if (ch == null)
                return Unauthorized(new DtoTokenResponse { success = false, message = "Sesión de verificación inválida o expirada. Inicie sesión de nuevo." });
            if (string.IsNullOrWhiteSpace(req?.Codigo) || string.IsNullOrWhiteSpace(req?.EnrollToken))
                return BadRequest(new DtoTokenResponse { success = false, message = "Código y token de enrolamiento requeridos." });

            var svcToken = await _apiMfa.ObtenerTokenAsync(ch.Usuario, ch.Ident, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(svcToken))
                return StatusCode(503, new DtoTokenResponse { success = false, message = "El servicio de doble autenticación no está disponible." });

            var r = await _apiMfa.EnrollConfirmAsync(svcToken, ch.Ident, ch.Usuario, req.EnrollToken, req.Codigo.Trim(), ObtenerIp(), CancellationToken.None);
            if (!r.Ok)
                return Unauthorized(new DtoTokenResponse { success = false, message = "Código de enrolamiento inválido." });

            return Ok(EmitirTokenFinal(ch));
        }

        /// <summary>Autoservicio: solicita el restablecimiento del segundo factor (envía código al correo).</summary>
        [HttpPost("Mfa/ResetRequest")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaResetRequest([FromBody] DtoMfaVerifyLoginReq req)
        {
            var ch = LeerChallenge(req?.Challenge);
            if (ch == null)
                return Unauthorized(new DtoTokenResponse { success = false, message = "Sesión de verificación inválida o expirada. Inicie sesión de nuevo." });

            var svcToken = await _apiMfa.ObtenerTokenAsync(ch.Usuario, ch.Ident, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(svcToken))
                return StatusCode(503, new DtoTokenResponse { success = false, message = "El servicio de doble autenticación no está disponible." });

            var r = await _apiMfa.ResetRequestAsync(svcToken, ch.Ident, ch.Usuario, ObtenerIp(), CancellationToken.None);
            return Ok(new DtoTokenResponse
            {
                success = r.Ok,
                usuario = ch.Usuario,
                challenge = req!.Challenge, // el mismo challenge sigue vigente (5 min) para el paso de confirmación
                message = r.Ok
                    ? "Se envió un código de verificación a su correo institucional."
                    : (r.Mensaje ?? "No fue posible iniciar el restablecimiento.")
            });
        }

        /// <summary>
        /// Autoservicio: confirma el código enviado al correo, ejecuta el restablecimiento y
        /// devuelve los datos para volver a enrolar el segundo factor.
        /// </summary>
        [HttpPost("Mfa/ResetConfirm")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MfaResetConfirm([FromBody] DtoMfaVerifyLoginReq req)
        {
            var ch = LeerChallenge(req?.Challenge);
            if (ch == null)
                return Unauthorized(new DtoTokenResponse { success = false, message = "Sesión de verificación inválida o expirada. Inicie sesión de nuevo." });
            if (string.IsNullOrWhiteSpace(req?.Codigo))
                return BadRequest(new DtoTokenResponse { success = false, message = "Código requerido." });

            var svcToken = await _apiMfa.ObtenerTokenAsync(ch.Usuario, ch.Ident, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(svcToken))
                return StatusCode(503, new DtoTokenResponse { success = false, message = "El servicio de doble autenticación no está disponible." });

            var conf = await _apiMfa.ResetConfirmAsync(svcToken, ch.Ident, ch.Usuario, req.Codigo.Trim(), ObtenerIp(), CancellationToken.None);
            if (!conf.Ok)
                return Unauthorized(new DtoTokenResponse { success = false, message = conf.Mensaje ?? "Código de restablecimiento inválido." });

            var exec = await _apiMfa.ResetExecuteAsync(svcToken, ch.Ident, ch.Usuario, ObtenerIp(), CancellationToken.None);
            if (!exec.Ok)
                return StatusCode(503, new DtoTokenResponse { success = false, message = exec.Mensaje ?? "No fue posible ejecutar el restablecimiento." });

            var enrol = await _apiMfa.EnrollStartAsync(svcToken, ch.Ident, ch.Usuario, CancellationToken.None);
            if (!enrol.Exito)
                return StatusCode(503, new DtoTokenResponse { success = false, message = "No fue posible iniciar el nuevo enrolamiento." });

            var fresh = CrearChallenge(ch.IdUsuario, ch.Usuario, ch.Roles, ch.Ident);
            return Ok(new DtoTokenResponse
            {
                success = true,
                usuario = ch.Usuario,
                requiere2Fa = true,
                enrolar = true,
                challenge = fresh,
                qr = enrol.QrBase64,
                claveManual = enrol.ManualKey,
                enrollToken = enrol.EnrollToken,
                message = "Segundo factor restablecido. Enrole nuevamente su autenticador."
            });
        }

        // ===================== Helpers 2FA =====================
        private sealed class Challenge2Fa
        {
            public long IdUsuario { get; set; }
            public string Usuario { get; set; } = string.Empty;
            public List<long> Roles { get; set; } = new();
            public long Ident { get; set; }
            public long Exp { get; set; }
        }

        private string CrearChallenge(long idUsuario, string usuario, List<long> roles, long ident)
        {
            var payload = new Challenge2Fa
            {
                IdUsuario = idUsuario,
                Usuario = usuario,
                Roles = roles ?? new List<long> { 1 },
                Ident = ident,
                Exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            return Comun.Security.CryptoUtility.Encrypt(json, _configuration);
        }

        private Challenge2Fa? LeerChallenge(string? challenge)
        {
            if (string.IsNullOrWhiteSpace(challenge)) return null;
            try
            {
                var json = Comun.Security.CryptoUtility.Decrypt(challenge, _configuration);
                var ch = System.Text.Json.JsonSerializer.Deserialize<Challenge2Fa>(json);
                if (ch == null || string.IsNullOrWhiteSpace(ch.Usuario)) return null;
                if (ch.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
                return ch;
            }
            catch
            {
                return null;
            }
        }

        private DtoTokenResponse EmitirTokenFinal(Challenge2Fa ch)
        {
            var token = _jwtService.CreateToken(ch.IdUsuario, ch.Usuario, ch.Roles ?? new List<long> { 1 });
            return new DtoTokenResponse
            {
                token = token,
                usuario = ch.Usuario,
                success = true,
                message = "Login exitoso"
            };
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

        private string ObtenerIp()
        {
            var fwd = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(fwd)) return fwd.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A";
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
