using Comun.Dtos.Eventos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;
using System.Security.Claims;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class EventoController : ControllerBase
    {
        private readonly IDbEventoService _service;
        private readonly ILogger<EventoController> _logger;

        public EventoController(
            IDbEventoService service,
            ILogger<EventoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var eventos = await _service.GetAllAsync(CancellationToken.None);
                return Ok(eventos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando todos los eventos");
                return StatusCode(500, new { success = false, message = "Error consultando eventos." });
            }
        }

        [HttpGet("activos")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivos([FromQuery] DateTime? fecha)
        {
            try
            {
                var eventos = await _service.GetActivosAsync(fecha, CancellationToken.None);
                return Ok(eventos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando eventos activos");
                return StatusCode(500, new { success = false, message = "Error consultando eventos activos." });
            }
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var evento = await _service.GetByIdAsync(id, CancellationToken.None);
                if (evento is null)
                    return NotFound(new { success = false, message = "Evento no encontrado." });

                return Ok(evento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando evento {Id}", id);
                return StatusCode(500, new { success = false, message = "Error consultando evento." });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] DtoEventoRequest request)
        {
            try
            {
                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.CreateAsync(request, usuario, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando evento");
                return StatusCode(500, new { success = false, message = "Error creando evento." });
            }
        }

        [HttpPut("{id:long}")]
        [Authorize]
        public async Task<IActionResult> Update(long id, [FromBody] DtoEventoRequest request)
        {
            try
            {
                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.UpdateAsync(id, request, usuario, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando evento {Id}", id);
                return StatusCode(500, new { success = false, message = "Error actualizando evento." });
            }
        }

        [HttpDelete("{id:long}")]
        [Authorize]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.DeleteAsync(id, usuario, maquina, CancellationToken.None);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando evento {Id}", id);
                return StatusCode(500, new { success = false, message = "Error eliminando evento." });
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
