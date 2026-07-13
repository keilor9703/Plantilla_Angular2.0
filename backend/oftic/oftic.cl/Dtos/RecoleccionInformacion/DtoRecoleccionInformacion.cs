using System.Text.Json.Serialization;

namespace Comun.Dtos.RecoleccionInformacion;

/// <summary>DTO de lectura para un hecho noticioso (COEST_NOTICIA).</summary>
public class DtoHechoNoticioso
{
    [JsonPropertyName("noticiaId")]
    public string NoticiaId { get; set; } = string.Empty;

    [JsonPropertyName("idUnidad")]
    public decimal IdUnidad { get; set; }

    [JsonPropertyName("unidadStr")]
    public string? UnidadStr { get; set; }

    [JsonPropertyName("titular")]
    public string Titular { get; set; } = string.Empty;

    [JsonPropertyName("fecha")]
    public DateTime? Fecha { get; set; }

    [JsonPropertyName("idCaracter")]
    public decimal? IdCaracter { get; set; }

    [JsonPropertyName("caracterStr")]
    public string? CaracterStr { get; set; }

    [JsonPropertyName("idTema")]
    public decimal? IdTema { get; set; }

    [JsonPropertyName("temaStr")]
    public string? TemaStr { get; set; }

    [JsonPropertyName("idDescTema")]
    public decimal? IdDescTema { get; set; }

    [JsonPropertyName("descTemaStr")]
    public string? DescTemaStr { get; set; }

    [JsonPropertyName("idCobertura")]
    public decimal? IdCobertura { get; set; }

    [JsonPropertyName("coberturaStr")]
    public string? CoberturaStr { get; set; }

    [JsonPropertyName("idTendencia")]
    public decimal? IdTendencia { get; set; }

    [JsonPropertyName("tendenciaStr")]
    public string? TendenciaStr { get; set; }

    [JsonPropertyName("idDependencia")]
    public decimal? IdDependencia { get; set; }

    [JsonPropertyName("dependenciaStr")]
    public string? DependenciaStr { get; set; }

    [JsonPropertyName("siglaDependenciaStr")]
    public string? SiglaDependenciaStr { get; set; }

    [JsonPropertyName("idModalidad")]
    public decimal? IdModalidad { get; set; }

    [JsonPropertyName("modalidadStr")]
    public string? ModalidadStr { get; set; }

    [JsonPropertyName("idEstacion")]
    public decimal? IdEstacion { get; set; }

    [JsonPropertyName("estacionStr")]
    public string? EstacionStr { get; set; }

    [JsonPropertyName("vigente")]
    public bool? Vigente { get; set; }

    [JsonPropertyName("contadorMedios")]
    public int ContadorMedios { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }

    [JsonPropertyName("maquinaCreacion")]
    public string? MaquinaCreacion { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime? FechaCreacion { get; set; }

    [JsonPropertyName("identificacionModifica")]
    public long? IdentificacionModifica { get; set; }

    [JsonPropertyName("maquinaModifica")]
    public string? MaquinaModifica { get; set; }

    [JsonPropertyName("fechaModifica")]
    public DateTime? FechaModifica { get; set; }
}

/// <summary>DTO de entrada para registrar un hecho noticioso.</summary>
public class DtoHechoNoticiosoRequest
{
    [JsonPropertyName("idUnidad")]
    public decimal IdUnidad { get; set; }

    [JsonPropertyName("titular")]
    public string Titular { get; set; } = string.Empty;

    [JsonPropertyName("fecha")]
    public DateTime? Fecha { get; set; }

    [JsonPropertyName("idCaracter")]
    public decimal? IdCaracter { get; set; }

    [JsonPropertyName("idTema")]
    public decimal? IdTema { get; set; }

    [JsonPropertyName("idDescTema")]
    public decimal? IdDescTema { get; set; }

    [JsonPropertyName("idCobertura")]
    public decimal? IdCobertura { get; set; }

    [JsonPropertyName("idTendencia")]
    public decimal? IdTendencia { get; set; }

    [JsonPropertyName("idDependencia")]
    public decimal? IdDependencia { get; set; }

    [JsonPropertyName("idModalidad")]
    public decimal? IdModalidad { get; set; }

    [JsonPropertyName("idEstacion")]
    public decimal? IdEstacion { get; set; }

    [JsonPropertyName("identificacionCreacion")]
    public long? IdentificacionCreacion { get; set; }
}

/// <summary>Resultado estándar de operaciones sobre hechos noticiosos.</summary>
public class DtoHechoNoticiosoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Id { get; set; }
}

/// <summary>Ítem para el selector de años con registros.</summary>
public class DtoAnioItem
{
    [JsonPropertyName("anio")]
    public int Anio { get; set; }
}
