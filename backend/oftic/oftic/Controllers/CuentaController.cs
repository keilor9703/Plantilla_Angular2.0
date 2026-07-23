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
        private readonly IJwtService _jwtService;
        private readonly IDbAuthRepository _dbAuthRepository;
        private readonly IDbUsuarioRepository _dbUsuarioRepository;
        private readonly ILogger<CuentaController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly HashSet<long> _impersonationAllowedUserIds;

        public CuentaController(
            IApiWebOud apiWebOud,
            IJwtService jwtService,
            IDbAuthRepository dbAuthRepository,
            IDbUsuarioRepository dbUsuarioRepository,
            ILogger<CuentaController> logger,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _apiWebOud = apiWebOud;
            _jwtService = jwtService;
            _dbAuthRepository = dbAuthRepository;
            _dbUsuarioRepository = dbUsuarioRepository;
            _logger = logger;
            _env = env;
            _configuration = configuration;
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

                var validateOudInDevelopment = _configuration.GetValue<bool>("Auth:ValidateOudInDevelopment");
                var shouldValidateExternal = !_env.IsDevelopment() || validateOudInDevelopment;

                // 1. Validación de credenciales en OUD / PIP si aplica
                if (shouldValidateExternal)
                {
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
                    // Auto-aprovisionar usuario autenticado si aún no existe en la base de datos local
                    idUsuario = await _dbUsuarioRepository.EnsureUsuarioExistsAsync(request.Usuario.Trim(), request.Usuario.Trim(), CancellationToken.None);
                    roles = new List<long> { 1 };
                }

                // 3. Generar token JWT local
                var jwtToken = _jwtService.CreateToken(idUsuario.Value, request.Usuario, roles ?? new List<long> { 1 });

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
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new DtoTokenResponse 
                { 
                    success = false, 
                    message = "Error al iniciar sesión" 
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
