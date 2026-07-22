using System;
using System.Text.Json.Serialization;

namespace Comun.Dtos.Videos
{
    public class DtoVideo
    {
        public long IdVideo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string? RutaVideo { get; set; }
        public string UrlVideo { get; set; } = string.Empty;
        public string TipoVideo { get; set; } = string.Empty;
        public long Orden { get; set; }
        public long Vigente { get; set; }
        public string CodigoEmbed { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaModifica { get; set; }
        public string? UsuarioModifica { get; set; }
        public string? MaquinaModifica { get; set; }
    }

    public class DtoVideoRequest
    {
        [JsonPropertyName("Titulo")]
        public string Titulo { get; set; } = string.Empty;
        [JsonPropertyName("Descripcion")]
        public string Descripcion { get; set; } = string.Empty;
        [JsonPropertyName("Observaciones")]
        public string? Observaciones { get; set; }
        [JsonPropertyName("RutaVideo")]
        public string? RutaVideo { get; set; }
        [JsonPropertyName("UrlVideo")]
        public string UrlVideo { get; set; } = string.Empty;
        [JsonPropertyName("TipoVideo")]
        public string TipoVideo { get; set; } = string.Empty;
        [JsonPropertyName("Orden")]
        public long Orden { get; set; }
        [JsonPropertyName("Vigente")]
        public long Vigente { get; set; }
        [JsonPropertyName("CodigoEmbed")]
        public string CodigoEmbed { get; set; } = string.Empty;
    }

    public class DtoVideoResult
    {
        public long Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
