using Comun.Dtos.RecoleccionInformacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;

namespace ofic.Controllers.RecoleccionInformacion
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DominioCoestController : ControllerBase
    {
        private readonly IDbDominioCoestService _service;
        private readonly ILogger<DominioCoestController> _logger;

        public DominioCoestController(IDbDominioCoestService service, ILogger<DominioCoestController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Obtener lista de dominios por su ID padre.</summary>
        [HttpGet("{padreId}")]
        public async Task<IActionResult> GetByPadreId(decimal padreId)
        {
            try
            {
                var items = await _service.GetByPadreIdAsync(padreId, CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dominios para padreId {PadreId}", padreId);
                return StatusCode(500, new { success = false, message = "Error obteniendo dominios." });
            }
        }

        /// <summary>Obtener catálogo completo de medios de comunicación.</summary>
        [HttpGet("medios")]
        public async Task<IActionResult> GetMedios()
        {
            try
            {
                var items = await _service.GetMediosAsync(CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo catálogo de medios");
                return StatusCode(500, new { success = false, message = "Error obteniendo catálogo de medios." });
            }
        }

        /// <summary>Obtener catálogo de medios filtrado por tipo de medio y unidad policial.</summary>
        [HttpGet("medios/tipo/{tipoMedioId}/unidad/{idUnidad}")]
        public async Task<IActionResult> GetMediosPorTipoYUnidad(decimal tipoMedioId, decimal idUnidad)
        {
            try
            {
                var items = await _service.GetMediosPorTipoYUnidadAsync(tipoMedioId, idUnidad, CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo medios para tipo {TipoMedioId} y unidad {IdUnidad}", tipoMedioId, idUnidad);
                return StatusCode(500, new { success = false, message = "Error obteniendo medios por tipo y unidad." });
            }
        }

        /// <summary>Obtener lista de dependencias.</summary>
        [HttpGet("dependencias")]
        public async Task<IActionResult> GetDependencias()
        {
            try
            {
                var items = await _service.GetDependenciasAsync(CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dependencias");
                return StatusCode(500, new { success = false, message = "Error obteniendo dependencias." });
            }
        }

        /// <summary>Obtener lista de unidades policiales ( departamentos / metropolitanas ).</summary>
        [HttpGet("unidades")]
        public async Task<IActionResult> GetUnidades()
        {
            try
            {
                var items = await _service.GetUnidadesAsync(CancellationToken.None);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo unidades policiales");
                return StatusCode(500, new { success = false, message = "Error obteniendo unidades." });
            }
        }
    }
}
