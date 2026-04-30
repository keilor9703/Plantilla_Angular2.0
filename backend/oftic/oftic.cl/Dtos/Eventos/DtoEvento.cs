using System.Text.Json.Serialization;

namespace Comun.Dtos.Eventos;

public class DtoEvento
{
    [JsonPropertyName("idEvento")]
    public long IdEvento { get; set; }

    [JsonPropertyName("nombreEvento")]
    public string NombreEvento { get; set; } = string.Empty;

    [JsonPropertyName("unidad")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("fechaInicio")]
    public DateTime FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime FechaFin { get; set; }

    [JsonPropertyName("departamento")]
    public string Departamento { get; set; } = string.Empty;

    [JsonPropertyName("ciudad")]
    public string Ciudad { get; set; } = string.Empty;

    [JsonPropertyName("direccionEvento")]
    public string DireccionEvento { get; set; } = string.Empty;

    [JsonPropertyName("cedulaResponsable")]
    public string CedulaResponsable { get; set; } = string.Empty;

    [JsonPropertyName("nombreResponsable")]
    public string NombreResponsable { get; set; } = string.Empty;

    [JsonPropertyName("correoResponsable")]
    public string? CorreoResponsable { get; set; }

    [JsonPropertyName("telefonoResponsable")]
    public string? TelefonoResponsable { get; set; }

    [JsonPropertyName("imagenEvento")]
    public string? ImagenEvento { get; set; }

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
    [JsonPropertyName("nombreEvento")]
    public string NombreEvento { get; set; } = string.Empty;

    [JsonPropertyName("unidad")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("departamento")]
    public string Departamento { get; set; } = string.Empty;

    [JsonPropertyName("ciudad")]
    public string Ciudad { get; set; } = string.Empty;

    [JsonPropertyName("direccionEvento")]
    public string DireccionEvento { get; set; } = string.Empty;

    [JsonPropertyName("cedulaResponsable")]
    public string CedulaResponsable { get; set; } = string.Empty;

    [JsonPropertyName("nombreResponsable")]
    public string NombreResponsable { get; set; } = string.Empty;

    [JsonPropertyName("correoResponsable")]
    public string? CorreoResponsable { get; set; }

    [JsonPropertyName("telefonoResponsable")]
    public string? TelefonoResponsable { get; set; }

    [JsonPropertyName("fechaInicio")]
    public DateTime FechaInicio { get; set; }

    [JsonPropertyName("fechaFin")]
    public DateTime FechaFin { get; set; }

    [JsonPropertyName("imagenEvento")]
    public string? ImagenEvento { get; set; }

    [JsonPropertyName("vigente")]
    public int Vigente { get; set; } = 1;
}

public class DtoEventoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long Id { get; set; }
}
