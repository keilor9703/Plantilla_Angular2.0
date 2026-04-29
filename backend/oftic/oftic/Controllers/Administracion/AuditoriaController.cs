using Comun.Dtos.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;

namespace Api.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IDbAuditoriaService _service;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(IDbAuditoriaService service, ILogger<AuditoriaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("bitacora")]
        [Authorize]
        public async Task<ActionResult<DtoAuditoriaPageResult>> GetBitacora(
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta,
            [FromQuery] string? usuario,
            [FromQuery] string? modulo,
            [FromQuery] string? accion,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            try
            {
                var filtro = new DtoAuditoriaFiltroRequest
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Usuario = usuario,
                    Modulo = modulo,
                    Accion = accion,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _service.GetBitacoraAsync(filtro, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando bitácora general.");
                return StatusCode(500, new { message = "Error consultando bitácora general." });
            }
        }
    }
}

