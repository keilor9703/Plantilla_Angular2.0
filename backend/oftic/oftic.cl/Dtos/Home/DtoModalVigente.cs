namespace Comun.Dtos
{
    public class DtoModalVigente
    {
        public long IdModal { get; set; }
        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? ImagenUrl { get; set; }
        public string? RedireccionUrl { get; set; }
        public int Orden { get; set; } = 1;
        public int Vigente { get; set; } = 1;
    }
}
