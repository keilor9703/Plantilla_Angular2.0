using System.Text.Json.Serialization;

namespace Comun.Dtos.RecoleccionInformacion;

/// <summary>DTO de lectura para un medio de comunicación asociado a un hecho noticioso (COEST_MEDIO).</summary>
public class DtoMedioCoest
{
    [JsonPropertyName("medioId")]
    public string MedioId { get; set; } = string.Empty;

    [JsonPropertyName("noticiaId")]
    public string NoticiaId { get; set; } = string.Empty;

    [JsonPropertyName("idMedio")]
    public decimal? IdMedio { get; set; }

    [JsonPropertyName("medioStr")]
    public string? MedioStr { get; set; }

    [JsonPropertyName("idDescMedio")]
    public string? IdDescMedio { get; set; }

    [JsonPropertyName("descMedioStr")]
    public string? DescMedioStr { get; set; }

    [JsonPropertyName("titular")]
    public string? Titular { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("fecha")]
    public DateTime? Fecha { get; set; }

    [JsonPropertyName("idVoceria")]
    public decimal? IdVoceria { get; set; }

    [JsonPropertyName("vocer\u00edaStr")]
    public string? VoceriaStr { get; set; }

    [JsonPropertyName("idReplica")]
    public decimal? IdReplica { get; set; }

    [JsonPropertyName("replicaStr")]
    public string? ReplicaStr { get; set; }

    [JsonPropertyName("cantidadReplica")]
    public decimal? CantidadReplica { get; set; }

    [JsonPropertyName("idEvidencia")]
    public decimal? IdEvidencia { get; set; }

    [JsonPropertyName("evidenciaStr")]
    public string? EvidenciaStr { get; set; }

    [JsonPropertyName("duracion")]
    public decimal? Duracion { get; set; }

    [JsonPropertyName("vigente")]
    public bool? Vigente { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }

    [JsonPropertyName("maquinaCreacion")]
    public string? MaquinaCreacion { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime? FechaCreacion { get; set; }

    [JsonPropertyName("intervinientes")]
    public List<DtoIntervinienteCoest> Intervinientes { get; set; } = [];
}

/// <summary>DTO de entrada para registrar un medio asociado a un hecho noticioso.</summary>
public class DtoMedioCoestRequest
{
    [JsonPropertyName("noticiaId")]
    public string NoticiaId { get; set; } = string.Empty;

    [JsonPropertyName("idMedio")]
    public decimal? IdMedio { get; set; }

    [JsonPropertyName("idDescMedio")]
    public string? IdDescMedio { get; set; }

    [JsonPropertyName("titular")]
    public string Titular { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("fecha")]
    public DateTime? Fecha { get; set; }

    [JsonPropertyName("idEvidencia")]
    public decimal? IdEvidencia { get; set; }

    [JsonPropertyName("duracion")]
    public decimal? Duracion { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }
}

/// <summary>Resultado estándar de operaciones sobre medios.</summary>
public class DtoMedioCoestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Id { get; set; }
}
