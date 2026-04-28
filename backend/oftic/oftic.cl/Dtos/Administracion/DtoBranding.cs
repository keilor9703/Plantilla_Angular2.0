namespace Comun.Dtos.Administracion
{
    public class DtoBrandingConfig
    {
        public long? idBranding { get; set; }
        public string sistema { get; set; } = "SISGE";
        public string nombreSistema { get; set; } = "SISGE";
        public int? unidadSistemaId { get; set; }
        public string? logoFileName { get; set; }
        public string? faviconFileName { get; set; }
        public string? fondo1FileName { get; set; }
        public string? fondo2FileName { get; set; }
        public string? fondo3FileName { get; set; }
    }

    public class DtoBrandingSaveRequest
    {
        public string? sistema { get; set; }
        public string? nombreSistema { get; set; }
        public int? unidadSistemaId { get; set; }
        public string? logoFileName { get; set; }
        public string? faviconFileName { get; set; }
        public string? fondo1FileName { get; set; }
        public string? fondo2FileName { get; set; }
        public string? fondo3FileName { get; set; }
    }

    public class DtoBrandingSaveResult
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class DtoBrandingUploadResult
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public string? fileName { get; set; }
    }

    public class DtoBrandingAssetResult
    {
        public bool success { get; set; }
        public bool badRequest { get; set; }
        public bool notFound { get; set; }
        public string message { get; set; } = string.Empty;
        public string? fullPath { get; set; }
        public string contentType { get; set; } = "application/octet-stream";
    }
}
