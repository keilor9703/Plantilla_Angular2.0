using System.Text.Json.Serialization;

namespace Comun.Dtos.Eventos;

public class DtoEvento
{
    [JsonPropertyName("idEvento")]
    public long IdEvento { get; set; }

    [JsonPropertyName("unidad")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("subtitulo")]
    public string? Subtitulo { get; set; }

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("lugar")]
    public string? Lugar { get; set; }

    [JsonPropertyName("ciudad")]
    public string? Ciudad { get; set; }

    [JsonPropertyName("fechaInicio")]
    public DateTime FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime FechaFin { get; set; }

    [JsonPropertyName("horaInicio")]
    public string? HoraInicio { get; set; }

    [JsonPropertyName("horaFin")]
    public string? HoraFin { get; set; }

    [JsonPropertyName("imagenPortada")]
    public string? ImagenPortada { get; set; }

    [JsonPropertyName("enlaceEvento")]
    public string? EnlaceEvento { get; set; }

    [JsonPropertyName("tipoEvento")]
    public string? TipoEvento { get; set; }

    [JsonPropertyName("orden")]
    public int Orden { get; set; }

    [JsonPropertyName("vigente")]
    public int Vigente { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("usuarioCreacion")]
    public string UsuarioCreacion { get; set; } = string.Empty;

    [JsonPropertyName("maquinaCreacion")]
    public string? MaquinaCreacion { get; set; }

    [JsonPropertyName("fechaModifica")]
    public DateTime? FechaModifica { get; set; }

    [JsonPropertyName("usuarioModifica")]
    public string? UsuarioModifica { get; set; }

    [JsonPropertyName("maquinaModifica")]
    public string? MaquinaModifica { get; set; }
}

public class DtoEventoRequest
{
    [JsonPropertyName("unidad")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("subtitulo")]
    public string? Subtitulo { get; set; }

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("lugar")]
    public string? Lugar { get; set; }

    [JsonPropertyName("ciudad")]
    public string? Ciudad { get; set; }

    [JsonPropertyName("fechaInicio")]
    public DateTime FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime FechaFin { get; set; }

    [JsonPropertyName("horaInicio")]
    public string? HoraInicio { get; set; }

    [JsonPropertyName("horaFin")]
    public string? HoraFin { get; set; }

    [JsonPropertyName("imagenPortada")]
    public string? ImagenPortada { get; set; }

    [JsonPropertyName("enlaceEvento")]
    public string? EnlaceEvento { get; set; }

    [JsonPropertyName("tipoEvento")]
    public string? TipoEvento { get; set; }

    [JsonPropertyName("orden")]
    public int Orden { get; set; } = 1;

    [JsonPropertyName("vigente")]
    public int Vigente { get; set; } = 1;
}

public class DtoEventoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long Id { get; set; }
}
