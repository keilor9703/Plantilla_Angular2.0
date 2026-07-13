using System.Text.Json.Serialization;

namespace Comun.Dtos.RecoleccionInformacion;

/// <summary>Ítem genérico para listas desplegables de dominios COEST.</summary>
public class DtoDominioCoest
{
    [JsonPropertyName("dominioId")]
    public decimal DominioId { get; set; }

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("vigente")]
    public int? Vigente { get; set; }
}

/// <summary>Ítem para catálogo de medios de comunicación (COEST_CTRL_MEDIOS).</summary>
public class DtoCatalogoMedio
{
    [JsonPropertyName("ctrlMediosId")]
    public string CtrlMediosId { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("tipoMedioId")]
    public decimal TipoMedioId { get; set; }

    [JsonPropertyName("tipoMedioStr")]
    public string? TipoMedioStr { get; set; }
}

/// <summary>Ítem para catálogo de dependencias policiales (COEST_DEPENDENCIA).</summary>
public class DtoDependencia
{
    [JsonPropertyName("dependenciaId")]
    public decimal DependenciaId { get; set; }

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;
}
