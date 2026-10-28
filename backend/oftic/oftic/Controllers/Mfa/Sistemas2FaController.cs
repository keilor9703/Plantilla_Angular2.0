using System;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Gestión de los sistemas de información integrados al servicio 2FA.</summary>
    [Route("api/admin/sistemas")]
    public class Sistemas2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;
        private readonly ILogger<Sistemas2FaController> _logger;

        public Sistemas2FaController(IMfaAdminService service, ILogger<Sistemas2FaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(CancellationToken ct)
        {
            var data = await _service.GetSistemasAsync(ct);
            return Ok(data);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult> GetOne(long id, CancellationToken ct)
        {
            var data = await _service.GetSistemaAsync(id, ct);
            if (data == null) return NotFound(new { success = false, message = "Sistema no encontrado." });
            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] DtoSistema2FaRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.CrearSistemaAsync(request, ObtenerActor(), ct);
                if (!result.Success) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando sistema 2FA");
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult> Update(long id, [FromBody] DtoSistema2FaRequest request, CancellationToken ct)
        {
            var result = await _service.ActualizarSistemaAsync(id, request, ObtenerActor(), ct);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Habilita o deshabilita un sistema. Body: { "habilitado": 0|1 }.</summary>
        [HttpPut("{id:long}/estado")]
        public async Task<ActionResult> CambiarEstado(long id, [FromBody] DtoSistema2FaRequest request, CancellationToken ct)
        {
            var result = await _service.CambiarEstadoSistemaAsync(id, request?.Habilitado ?? 1, ObtenerActor(), ct);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Genera y asigna un nuevo secreto HMAC. Devuelve el secreto una sola vez.</summary>
        [HttpPost("{id:long}/rotar-secreto")]
        public async Task<ActionResult> RotarSecreto(long id, CancellationToken ct)
        {
            var result = await _service.RotarSecretoAsync(id, ObtenerActor(), ct);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
