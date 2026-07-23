using System.Text.Json.Serialization;

namespace Comun.Dtos.Administracion
{
    public class DtoUnidadOrg
    {
        [JsonPropertyName("consecutivo")]
        public int Consecutivo { get; set; }

        [JsonPropertyName("siglaFisica")]
        public string Sigla { get; set; } = string.Empty;

        [JsonPropertyName("descripcionDependencia")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("siglaPapa")]
        public string? SiglaPapa { get; set; }

        [JsonPropertyName("vigente")]
        public string? Vigente { get; set; } = "SI";

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }
    }
}
