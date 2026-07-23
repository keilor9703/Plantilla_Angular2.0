using Comun.Dtos;
using Datos.Interfaz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servicios.ApiInterfaz;

namespace ofic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly IDbHomeRepository _dbHomeRepository;
        private readonly IApiWebToken _apiWebToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IDbHomeRepository dbHomeRepository,
            IApiWebToken apiWebToken,
            IConfiguration configuration,
            ILogger<HomeController> logger)
        {
            _dbHomeRepository = dbHomeRepository;
            _apiWebToken = apiWebToken;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _dbHomeRepository.GetStatsAsync(HttpContext.RequestAborted);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Base de datos no disponible para métricas del Home. Retornando métricas por defecto.");
                return Ok(new Comun.Dtos.Home.DtoHomeStats
                {
                    usuariosActivos = 0,
                    reportesGenerados = 0,
                    alertasSistema = 0,
                    modulosConfigurados = 0,
                    rolesDefinidos = 0
                });
            }
        }

        [HttpGet("sliders")]
        public async Task<IActionResult> GetSliders()
        {
            try
            {
                var usuario = User.Identity?.Name?.Trim();
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    usuario = User.FindFirst("usuario")?.Value?.Trim()
                        ?? User.FindFirst("name")?.Value?.Trim()
                        ?? "INVITADO";
                }

                var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(ip))
                {
                    ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }

                var request = new DtoConsultaSliderPublicoRequest
                {
                    FUENTE_TRANSACCION = _configuration["ApiSettings:SiglaSistema"] ?? "SISGE",
                    SISTEMA_INFORMACION = _configuration["ApiSettings:NombreSistema"] ?? "Sistema de Gestión Especializada - SISGE",
                    IP_MAQUINA = ip,
                    USUARIO = usuario
                };

                var sliders = await _apiWebToken.GetSlidersPublicosAsync(request, HttpContext.RequestAborted);
                return Ok(sliders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando sliders públicos del Home");
                return StatusCode(500, new { message = "Error consultando sliders públicos del Home" });
            }
        }

        [HttpGet("modales")]
        public async Task<IActionResult> GetModalesVigentes()
        {
            try
            {
                var usuario = User.Identity?.Name?.Trim();
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    usuario = User.FindFirst("usuario")?.Value?.Trim()
                        ?? User.FindFirst("name")?.Value?.Trim()
                        ?? "INVITADO";
                }

                var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(ip))
                {
                    ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }

                var request = new DtoConsultaSliderPublicoRequest
                {
                    FUENTE_TRANSACCION = _configuration["ApiSettings:SiglaSistema"] ?? "SISGE",
                    SISTEMA_INFORMACION = _configuration["ApiSettings:NombreSistema"] ?? "Sistema de Gestión Especializada - SISGE",
                    IP_MAQUINA = ip,
                    USUARIO = usuario
                };

                var modales = await _apiWebToken.GetModalesVigentesAsync(request, HttpContext.RequestAborted);
                return Ok(modales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando modales vigentes del Home");
                return Ok(new List<DtoModalVigente>());
            }
        }
    }
}
