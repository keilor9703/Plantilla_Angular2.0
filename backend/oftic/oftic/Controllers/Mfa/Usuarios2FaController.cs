using System;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Gestión centralizada de los usuarios del servicio de doble autenticación.</summary>
    [Route("api/admin/usuarios2fa")]
    public class Usuarios2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;
        private readonly ILogger<Usuarios2FaController> _logger;

        public Usuarios2FaController(IMfaAdminService service, ILogger<Usuarios2FaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Lista/busca usuarios. Parámetros: filtro (identificación o usuario), pagina, tamano.</summary>
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? filtro = null, [FromQuery] int pagina = 1,
            [FromQuery] int tamano = 50, CancellationToken ct = default)
        {
            var data = await _service.GetUsuariosAsync(filtro, pagina, tamano, ct);
            return Ok(data);
        }

        [HttpGet("{idMfa:long}")]
        public async Task<ActionResult> GetOne(long idMfa, CancellationToken ct)
        {
            var data = await _service.GetUsuarioAsync(idMfa, ct);
            if (data == null) return NotFound(new { success = false, message = "Usuario no encontrado." });
            return Ok(data);
        }

        [HttpPost("{idMfa:long}/desbloquear")]
        public async Task<ActionResult> Desbloquear(long idMfa, CancellationToken ct)
        {
            var r = await _service.DesbloquearAsync(idMfa, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>Habilita o deshabilita el MFA del usuario. Body: { "estado": 0|1 }.</summary>
        [HttpPost("{idMfa:long}/estado")]
        public async Task<ActionResult> CambiarEstado(long idMfa, [FromBody] DtoCambioEstado2FaRequest request, CancellationToken ct)
        {
            var r = await _service.CambiarEstadoMfaAsync(idMfa, request?.Estado ?? 1, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{idMfa:long}/forzar-reenrolamiento")]
        public async Task<ActionResult> ForzarReenrol(long idMfa, CancellationToken ct)
        {
            var r = await _service.ForzarReenrolAsync(idMfa, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{idMfa:long}/restablecer")]
        public async Task<ActionResult> Restablecer(long idMfa, CancellationToken ct)
        {
            var r = await _service.ResetMfaAsync(idMfa, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{idMfa:long}/limpiar-dispositivos")]
        public async Task<ActionResult> LimpiarDispositivos(long idMfa, CancellationToken ct)
        {
            var r = await _service.LimpiarDispositivosAsync(idMfa, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
