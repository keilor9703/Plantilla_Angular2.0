using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Negocio.Interfaz.Mfa;
using Servicios.ApiInterfaz;

namespace Api.Controllers.Mfa
{
    /// <summary>Tablero de métricas del servicio de doble autenticación (SI2FA).</summary>
    [Route("api/admin/dashboard")]
    public class Dashboard2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;
        private readonly IMfaCentralService _mfa;
        private readonly ILogger<Dashboard2FaController> _logger;

        public Dashboard2FaController(IMfaAdminService service, IMfaCentralService mfa, ILogger<Dashboard2FaController> logger)
        {
            _service = service;
            _mfa = mfa;
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

        /// <summary>
        /// Estado de salud del servicio 2FA central (Api2FA) en tiempo real.
        /// El tablero consulta este endpoint periódicamente para mostrar si el
        /// servicio está operativo y con qué latencia responde.
        /// </summary>
        [HttpGet("salud")]
        public async Task<ActionResult> GetSalud(CancellationToken ct = default)
        {
            try
            {
                var r = await _mfa.HealthAsync(ct);
                return Ok(new
                {
                    up = r.Up,
                    estado = r.Up ? "OPERATIVO" : "CAIDO",
                    latenciaMs = r.LatenciaMs,
                    mensaje = r.Mensaje,
                    consultado = r.Consultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando salud del servicio 2FA");
                return Ok(new
                {
                    up = false,
                    estado = "CAIDO",
                    latenciaMs = 0L,
                    mensaje = "No fue posible consultar el estado del servicio.",
                    consultado = DateTime.Now
                });
            }
        }
    }
}
