using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Gestión de los dispositivos confiables registrados en el servicio 2FA.</summary>
    [Route("api/admin/dispositivos")]
    public class Dispositivos2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;

        public Dispositivos2FaController(IMfaAdminService service)
        {
            _service = service;
        }

        /// <summary>Lista dispositivos. Parámetros: idMfa (opcional), soloVigentes (por defecto true).</summary>
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] long? idMfa = null, [FromQuery] bool soloVigentes = true,
            CancellationToken ct = default)
        {
            var data = await _service.GetDispositivosAsync(idMfa, soloVigentes, ct);
            return Ok(data);
        }

        [HttpDelete("{idDevice:long}")]
        public async Task<ActionResult> Revocar(long idDevice, CancellationToken ct)
        {
            var r = await _service.RevocarDispositivoAsync(idDevice, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
