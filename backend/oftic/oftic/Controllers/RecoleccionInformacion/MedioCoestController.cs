using Comun.Dtos.RecoleccionInformacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;
using System.Security.Claims;

namespace ofic.Controllers.RecoleccionInformacion
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MedioCoestController : ControllerBase
    {
        private readonly IDbMedioCoestService _service;
        private readonly ILogger<MedioCoestController> _logger;

        public MedioCoestController(IDbMedioCoestService service, ILogger<MedioCoestController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Medios de comunicación asociados a un hecho noticioso (incluye intervinientes).</summary>
        [HttpGet("noticia/{noticiaId}")]
        public async Task<IActionResult> GetByNoticia(string noticiaId)
        {
            try
            {
                var items = await _service.GetByNoticiaAsync(noticiaId, CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando medios de noticia {NoticiaId}", noticiaId);
                return StatusCode(500, new { success = false, message = "Error consultando medios." });
            }
        }

        /// <summary>Registrar un medio de comunicación asociado a un hecho noticioso.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DtoMedioCoestRequest request)
        {
            try
            {
                var (_, maquina) = ObtenerAuditoria();

                if (request.IdentificacionCreacion is null)
                {
                    var identClaim = User.FindFirstValue("identificacion") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (long.TryParse(identClaim, out var ident))
                        request.IdentificacionCreacion = ident;
                }

                var result = await _service.CreateAsync(request, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando medio COEST");
                return StatusCode(500, new { success = false, message = "Error creando medio." });
            }
        }

        /// <summary>Eliminar (baja lógica) un medio y sus intervinientes.</summary>
        [HttpDelete("{medioId}")]
        public async Task<IActionResult> Delete(string medioId)
        {
            try
            {
                var (_, maquina) = ObtenerAuditoria();
                var identClaim   = User.FindFirstValue("identificacion") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                long.TryParse(identClaim, out var ident);

                var result = await _service.DeleteAsync(medioId, ident, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando medio COEST {Id}", medioId);
                return StatusCode(500, new { success = false, message = "Error eliminando medio." });
            }
        }

        private (string usuario, string maquina) ObtenerAuditoria()
        {
            var usuario = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("unique_name")
                ?? User.FindFirstValue("nameid")
                ?? "sistema";

            var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? Environment.MachineName
                ?? "N/A";

            return (usuario, maquina);
        }
    }
}
