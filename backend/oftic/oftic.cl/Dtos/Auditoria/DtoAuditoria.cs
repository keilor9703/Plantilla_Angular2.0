namespace Comun.Dtos.Auditoria
{
    public class DtoAuditoriaItem
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Registro { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Maquina { get; set; } = string.Empty;
    }

    public class DtoAuditoriaFiltroRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? Usuario { get; set; }
        public string? Modulo { get; set; }
        public string? Accion { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DtoAuditoriaPageResult
    {
        public List<DtoAuditoriaItem> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}

