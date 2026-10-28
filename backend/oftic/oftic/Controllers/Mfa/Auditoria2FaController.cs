using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz.Mfa;

namespace Api.Controllers.Mfa
{
    /// <summary>Explorador de auditoría del servicio de doble autenticación (SI2FA).</summary>
    [Route("api/admin/auditoria2fa")]
    public class Auditoria2FaController : Admin2FaControllerBase
    {
        private readonly IMfaAdminService _service;

        public Auditoria2FaController(IMfaAdminService service)
        {
            _service = service;
        }

        /// <summary>Consulta paginada con filtros (identificación, usuario, sistema, evento, rango de fechas).</summary>
        [HttpGet]
        public async Task<ActionResult> Buscar([FromQuery] long? identificacion, [FromQuery] string? usuario,
            [FromQuery] string? sistema, [FromQuery] string? evento, [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta, [FromQuery] int pagina = 1, [FromQuery] int tamano = 50,
            CancellationToken ct = default)
        {
            var filtro = new DtoAuditoria2FaFiltro
            {
                Identificacion = identificacion,
                Usuario = usuario,
                Sistema = sistema,
                Evento = evento,
                Desde = desde,
                Hasta = hasta,
                Pagina = pagina,
                TamanoPagina = tamano
            };
            var data = await _service.GetAuditoriaAsync(filtro, ct);
            return Ok(data);
        }

        /// <summary>Exporta a CSV el resultado del filtro (hasta 10.000 registros).</summary>
        [HttpGet("exportar")]
        public async Task<ActionResult> Exportar([FromQuery] long? identificacion, [FromQuery] string? usuario,
            [FromQuery] string? sistema, [FromQuery] string? evento, [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta, CancellationToken ct = default)
        {
            var filtro = new DtoAuditoria2FaFiltro
            {
                Identificacion = identificacion,
                Usuario = usuario,
                Sistema = sistema,
                Evento = evento,
                Desde = desde,
                Hasta = hasta,
                Pagina = 1,
                TamanoPagina = 10000
            };
            var data = await _service.GetAuditoriaAsync(filtro, ct);

            var sb = new StringBuilder();
            sb.AppendLine("IdAuditoria;Fecha;Identificacion;Usuario;Sistema;Evento;Detalle;IP");
            foreach (var a in data.Items)
            {
                sb.Append(a.IdAuditoria).Append(';')
                  .Append(a.CreadoAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(';')
                  .Append(a.Identificacion).Append(';')
                  .Append(Csv(a.UsuarioInstitucional)).Append(';')
                  .Append(Csv(a.Sistema)).Append(';')
                  .Append(Csv(a.Evento)).Append(';')
                  .Append(Csv(a.Detalle)).Append(';')
                  .Append(Csv(a.IpOrigen)).AppendLine();
            }

            var bytes = Encoding.UTF8.GetPreamble().Length > 0
                ? Concat(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes(sb.ToString()))
                : Encoding.UTF8.GetBytes(sb.ToString());

            var nombre = $"auditoria_si2fa_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", nombre);
        }

        private static string Csv(string? v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            var s = v.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return (s.Contains(';') || s.Contains('"')) ? $"\"{s}\"" : s;
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }
}
