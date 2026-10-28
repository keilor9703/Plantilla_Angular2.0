using System;

namespace Comun.Dtos.Mfa
{
    /// <summary>Registro de auditoría del servicio 2FA (tabla mfa_auditoria).</summary>
    public class DtoAuditoria2Fa
    {
        public long IdAuditoria { get; set; }
        public long? Identificacion { get; set; }
        public string? UsuarioInstitucional { get; set; }
        public string? Sistema { get; set; }
        public string? Evento { get; set; }
        public string? Detalle { get; set; }
        public string? IpOrigen { get; set; }
        public DateTime? CreadoAt { get; set; }
    }

    /// <summary>Filtros para el explorador de auditoría.</summary>
    public class DtoAuditoria2FaFiltro
    {
        public long? Identificacion { get; set; }
        public string? Usuario { get; set; }
        public string? Sistema { get; set; }
        public string? Evento { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 50;
    }

    public class DtoAuditoria2FaPagina
    {
        public long Total { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public System.Collections.Generic.List<DtoAuditoria2Fa> Items { get; set; } = new();
    }
}
