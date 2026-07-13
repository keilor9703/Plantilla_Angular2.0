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
    public class IntervinienteCoestController : ControllerBase
    {
        private readonly IDbIntervinienteCoestService _service;
        private readonly ILogger<IntervinienteCoestController> _logger;

        public IntervinienteCoestController(IDbIntervinienteCoestService service, ILogger<IntervinienteCoestController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Registrar un interviniente (vocero uniformado) asociado a un medio.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DtoIntervinienteCoestRequest request)
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
                _logger.LogError(ex, "Error creando interviniente COEST");
                return StatusCode(500, new { success = false, message = "Error creando interviniente." });
            }
        }

        /// <summary>Eliminar (baja lógica) un interviniente.</summary>
        [HttpDelete("{intervinienteId}")]
        public async Task<IActionResult> Delete(string intervinienteId)
        {
            try
            {
                var (_, maquina) = ObtenerAuditoria();
                var identClaim   = User.FindFirstValue("identificacion") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                long.TryParse(identClaim, out var ident);

                var result = await _service.DeleteAsync(intervinienteId, ident, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando interviniente COEST {Id}", intervinienteId);
                return StatusCode(500, new { success = false, message = "Error eliminando interviniente." });
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
