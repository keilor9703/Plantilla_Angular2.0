using System.Text.Json.Serialization;

namespace Comun.Dtos.RecoleccionInformacion;

/// <summary>DTO de lectura para un interviniente (vocero uniformado) en un medio (COEST_INTERVINIENTE).</summary>
public class DtoIntervinienteCoest
{
    [JsonPropertyName("intervinienteId")]
    public string IntervinienteId { get; set; } = string.Empty;

    [JsonPropertyName("medioId")]
    public string MedioId { get; set; } = string.Empty;

    [JsonPropertyName("grado")]
    public decimal? Grado { get; set; }

    [JsonPropertyName("gradoStr")]
    public string? GradoStr { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("cargo")]
    public string? Cargo { get; set; }

    [JsonPropertyName("vigente")]
    public bool? Vigente { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }

    [JsonPropertyName("maquinaCreacion")]
    public string? MaquinaCreacion { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime? FechaCreacion { get; set; }
}

/// <summary>DTO de entrada para registrar un interviniente/vocero.</summary>
public class DtoIntervinienteCoestRequest
{
    [JsonPropertyName("medioId")]
    public string MedioId { get; set; } = string.Empty;

    [JsonPropertyName("grado")]
    public decimal? Grado { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("cargo")]
    public string? Cargo { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }
}

/// <summary>Resultado estándar de operaciones sobre intervinientes.</summary>
public class DtoIntervinienteCoestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Id { get; set; }
}
