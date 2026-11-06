using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>
    /// Celulares registrados en la app 2FA (llave de login por QR). Permite al CSIRT revocarlos
    /// (celular perdido/robado). La revocación es inmediata: la API 2FA rechaza al dispositivo (401).
    /// </summary>
    [Route("api/admin/celulares")]
    public class Celulares2FaController : Admin2FaControllerBase
    {
        private const int MotivoMin = 5, MotivoMax = 300;
        private readonly IMfaAdminService _service;

        public Celulares2FaController(IMfaAdminService service) => _service = service;

        /// <summary>Busca por cédula o usuario. Parámetros: filtro, soloActivos (por defecto true).</summary>
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? filtro = null, [FromQuery] bool soloActivos = true,
            CancellationToken ct = default)
            => Ok(await _service.GetCelularesAppAsync(filtro, soloActivos, ct));

        /// <summary>Revoca un celular. Body: { "motivo": "..." } (obligatorio).</summary>
        [HttpPost("{idDispositivo:long}/revocar")]
        public async Task<ActionResult> Revocar(long idDispositivo, [FromBody] DtoRevocarCelularRequest? req, CancellationToken ct)
        {
            var motivo = ValidarMotivo(req, out var error);
            if (motivo == null) return BadRequest(new DtoAccion2FaResult { Success = false, Message = error });
            var r = await _service.RevocarCelularAppAsync(idDispositivo, motivo, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>Revoca todos los celulares activos de un funcionario (cédula). Body: { "motivo": "..." }.</summary>
        [HttpPost("usuario/{identificacion:long}/revocar")]
        public async Task<ActionResult> RevocarTodos(long identificacion, [FromBody] DtoRevocarCelularRequest? req, CancellationToken ct)
        {
            var motivo = ValidarMotivo(req, out var error);
            if (motivo == null) return BadRequest(new DtoAccion2FaResult { Success = false, Message = error });
            var r = await _service.RevocarCelularesAppUsuarioAsync(identificacion, motivo, ObtenerActor(), ObtenerIp(), ct);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        private static string? ValidarMotivo(DtoRevocarCelularRequest? req, out string error)
        {
            var m = (req?.Motivo ?? string.Empty).Trim();
            if (m.Length < MotivoMin) { error = $"Indique el motivo de la revocación (mínimo {MotivoMin} caracteres)."; return null; }
            if (m.Length > MotivoMax) m = m.Substring(0, MotivoMax);
            error = string.Empty;
            return m;
        }
    }
}
