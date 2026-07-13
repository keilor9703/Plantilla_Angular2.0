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
    public class HechoNoticiosoController : ControllerBase
    {
        private readonly IDbHechoNoticiosoService _service;
        private readonly ILogger<HechoNoticiosoController> _logger;

        public HechoNoticiosoController(IDbHechoNoticiosoService service, ILogger<HechoNoticiosoController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Listado general de hechos noticiosos filtrado por año.</summary>
        [HttpGet]
        [HttpGet("listado")]
        public async Task<IActionResult> GetListado([FromQuery] int anio = 0)
        {
            try
            {
                var items = await _service.GetAllAsync(anio, CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando listado de hechos noticiosos");
                return StatusCode(500, new { success = false, message = "Error consultando listado." });
            }
        }

        /// <summary>Listado filtrado por sigla de unidad y año.</summary>
        [HttpGet("unidad/{siglaUnidad}")]
        public async Task<IActionResult> GetByUnidad(string siglaUnidad, [FromQuery] int anio = 0)
        {
            try
            {
                var items = await _service.GetByUnidadAsync(siglaUnidad, anio, CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando hechos noticiosos por unidad {Unidad}", siglaUnidad);
                return StatusCode(500, new { success = false, message = "Error consultando por unidad." });
            }
        }

        /// <summary>Hecho noticioso por ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var item = await _service.GetByIdAsync(id, CancellationToken.None);
                if (item is null)
                    return NotFound(new { success = false, message = "Registro no encontrado." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando hecho noticioso {Id}", id);
                return StatusCode(500, new { success = false, message = "Error consultando registro." });
            }
        }

        /// <summary>Lista de años con registros para el selector de filtro.</summary>
        [HttpGet("anios")]
        public async Task<IActionResult> GetAnios()
        {
            try
            {
                var anios = await _service.GetAniosAsync(CancellationToken.None);
                return Ok(anios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando años de hechos noticiosos");
                return StatusCode(500, new { success = false, message = "Error consultando años." });
            }
        }

        /// <summary>Registrar un nuevo hecho noticioso.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DtoHechoNoticiosoRequest request)
        {
            try
            {
                var (_, maquina) = ObtenerAuditoria();

                // Asignar identificación del JWT al request si no viene en el body
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
                _logger.LogError(ex, "Error creando hecho noticioso");
                return StatusCode(500, new { success = false, message = "Error creando registro." });
            }
        }

        /// <summary>Eliminar (baja lógica) un hecho noticioso y todos sus medios e intervinientes.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var (_, maquina) = ObtenerAuditoria();
                var identClaim   = User.FindFirstValue("identificacion") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                long.TryParse(identClaim, out var ident);

                var result = await _service.DeleteAsync(id, ident, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando hecho noticioso {Id}", id);
                return StatusCode(500, new { success = false, message = "Error eliminando registro." });
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
