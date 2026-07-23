using Comun.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servicios.ApiInterfaz;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NoticiaController : ControllerBase
    {
        private readonly IApiWebToken _apiWebToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NoticiaController> _logger;

        public NoticiaController(
            IApiWebToken apiWebToken,
            IConfiguration configuration,
            ILogger<NoticiaController> logger)
        {
            _apiWebToken = apiWebToken;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("activas")]
        [HttpGet]
        public async Task<IActionResult> GetActivas()
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

                var noticias = await _apiWebToken.GetNoticiasInternasAsync(request, HttpContext.RequestAborted);
                return Ok(noticias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando noticias internas desde PIP");
                return StatusCode(500, new { message = "Error consultando noticias internas" });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var noticias = await GetNoticiasListAsync();
                var noticia = noticias.FirstOrDefault(n => n.IdNoticia == id);
                if (noticia is null)
                {
                    return NotFound(new { message = "Noticia no encontrada" });
                }
                return Ok(noticia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando noticia por ID: {Id}", id);
                return StatusCode(500, new { message = "Error consultando noticia" });
            }
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> DarLike(string id)
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

                var request = new DtoLikeNoticiaRequest
                {
                    FUENTE_TRANSACCION = _configuration["ApiSettings:SiglaSistema"] ?? "SISGE",
                    SISTEMA_INFORMACION = _configuration["ApiSettings:NombreSistema"] ?? "Sistema de Gestión Especializada - SISGE",
                    IP_MAQUINA = ip,
                    USUARIO = usuario,
                    ID_NOTICIA = id
                };

                var ok = await _apiWebToken.RegistrarLikeNoticiaAsync(request, HttpContext.RequestAborted);
                if (ok)
                {
                    return Ok(new { success = true, message = "Me gusta registrado exitosamente" });
                }

                return BadRequest(new { success = false, message = "No fue posible registrar el Me Gusta en PIP" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar me gusta para la noticia {Id}", id);
                return StatusCode(500, new { success = false, message = "Error procesando el Me Gusta" });
            }
        }

        private async Task<List<DtoNoticia>> GetNoticiasListAsync()
        {
            var usuario = User.Identity?.Name?.Trim() ?? "INVITADO";
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var request = new DtoConsultaSliderPublicoRequest
            {
                FUENTE_TRANSACCION = _configuration["ApiSettings:SiglaSistema"] ?? "SISGE",
                SISTEMA_INFORMACION = _configuration["ApiSettings:NombreSistema"] ?? "Sistema de Gestión Especializada - SISGE",
                IP_MAQUINA = ip,
                USUARIO = usuario
            };

            return await _apiWebToken.GetNoticiasInternasAsync(request, HttpContext.RequestAborted);
        }
    }
}
