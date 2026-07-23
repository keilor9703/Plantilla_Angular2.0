namespace Comun.Dtos.Administracion
{
    public class DtoRadioEmisora
    {
        public long IdEmisora { get; set; }
        public string? Nombre { get; set; }
        public string? StreamUrl { get; set; }
        public string? LogoUrl { get; set; }
        public int Orden { get; set; } = 1;
        public int Activo { get; set; } = 1;
    }
}
