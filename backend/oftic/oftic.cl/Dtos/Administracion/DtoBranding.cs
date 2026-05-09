namespace Comun.Dtos.Administracion
{
    public class DtoBranding
    {
        public int IdBranding { get; set; }
        public string Sistema { get; set; } = string.Empty;
        public string NombreSistema { get; set; } = string.Empty;
        public int UnidadSistemaId { get; set; }
        public string Sigla { get; set; } = string.Empty;
        public string NombreUnidad { get; set; } = string.Empty;
        public string? LogoFileName { get; set; }
        public string? FaviconFileName { get; set; }
        public string? Fondo1FileName { get; set; }
        public string? Fondo2FileName { get; set; }
        public string? Fondo3FileName { get; set; }
        public int Vigente { get; set; }
    }

    public class DtoBrandingRequest
    {
        public int IdBranding { get; set; }
        public string Sistema { get; set; } = string.Empty;
        public string NombreSistema { get; set; } = string.Empty;
        public int UnidadSistemaId { get; set; }
        public string Sigla { get; set; } = string.Empty;
        public string NombreUnidad { get; set; } = string.Empty;
        public string? LogoFileName { get; set; }
        public string? FaviconFileName { get; set; }
        public string? Fondo1FileName { get; set; }
        public string? Fondo2FileName { get; set; }
        public string? Fondo3FileName { get; set; }
        public int Vigente { get; set; }
        public string UsuarioAuditoria { get; set; } = string.Empty;
        public string MaquinaAuditoria { get; set; } = string.Empty;
    }

    public class DtoBrandingResult
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
