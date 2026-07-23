using System;

namespace Comun.Dtos
{
    public class DtoSliderPublico
    {
        public long Id { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public string? RedireccionUrl { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
