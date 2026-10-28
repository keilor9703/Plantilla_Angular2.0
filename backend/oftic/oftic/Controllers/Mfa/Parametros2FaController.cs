using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Parámetros configurables del servicio de doble autenticación (SI2FA).</summary>
    [Route("api/admin/parametros")]
    public class Parametros2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;

        public Parametros2FaController(IMfaAdminService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(CancellationToken ct)
        {
            var data = await _service.GetParametrosAsync(ct);
            return Ok(data);
        }

        /// <summary>Actualiza el valor de un parámetro. Body: { "valor": "..." }.</summary>
        [HttpPut("{clave}")]
        public async Task<ActionResult> Update(string clave, [FromBody] DtoParametro2FaUpdate request, CancellationToken ct)
        {
            var r = await _service.ActualizarParametroAsync(clave, request?.Valor ?? string.Empty, ObtenerActor(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
