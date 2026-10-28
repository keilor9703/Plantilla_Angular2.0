using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Tablero de métricas del servicio de doble autenticación (SI2FA).</summary>
    [Route("api/admin/dashboard")]
    public class Dashboard2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;
        private readonly ILogger<Dashboard2FaController> _logger;

        public Dashboard2FaController(IMfaAdminService service, ILogger<Dashboard2FaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Métricas agregadas. Parámetro opcional dias (ventana de actividad, por defecto 30).</summary>
        [HttpGet("metricas")]
        public async Task<ActionResult> GetMetricas([FromQuery] int dias = 30, CancellationToken ct = default)
        {
            try
            {
                var data = await _service.GetMetricasAsync(dias, ct);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas SI2FA");
                return StatusCode(500, new { success = false, message = "No fue posible obtener las métricas." });
            }
        }
    }
}
