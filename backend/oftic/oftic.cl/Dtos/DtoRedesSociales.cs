namespace Comun.Dtos;

public class DtoRedesSociales
{
    public int Id { get; set; }
    public string RedSocial { get; set; } = string.Empty;
    public string UnidadResponsable { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string UrlCuenta { get; set; } = string.Empty;
    public string ActaAsignacion { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string TelResponsable { get; set; } = string.Empty;
    public string EmailResponsable { get; set; } = string.Empty;
    public string EmailCuenta { get; set; } = string.Empty;
    public string TelAsociado { get; set; } = string.Empty;
    public int Vigente { get; set; } = 1;
    public DateTime? FechaCreacion { get; set; }
    public string? UsuarioCreacion { get; set; }
}

public class DtoDominio
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class DtoUnidad
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class DtoRedesSocialesResult
{
    public int Id { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}