using System;

namespace Comun.Dtos
{
    public class DtoNoticia
    {
        public long IdNoticia { get; set; }
        public string? Unidad { get; set; }
        public string? Titulo { get; set; }
        public string? Seccion { get; set; }
        public string? Ciudad { get; set; }
        public string? ImagenNoticia { get; set; }
        public string? Subtitulo { get; set; }
        public string? Contenido { get; set; }
        public int Vigente { get; set; } = 1;
        public string? FechaCreacion { get; set; }
        public string? UsuarioCreacion { get; set; }
        public string? MaquinaCreacion { get; set; }
        public string? FechaModifica { get; set; }
        public string? UsuarioModifica { get; set; }
        public string? MaquinaModifica { get; set; }
        public int Megusta { get; set; }
    }
}
