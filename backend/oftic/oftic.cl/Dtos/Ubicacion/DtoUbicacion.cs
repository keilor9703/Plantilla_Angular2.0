namespace Comun.Dtos.Ubicacion
{
    public class DtoDepartamento
    {
        public long IdDepartamento { get; set; }
        public string CodigoDane { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class DtoMunicipio
    {
        public long IdMunicipio { get; set; }
        public long IdDepartamento { get; set; }
        public string CodigoDane { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal? Longitud { get; set; }
        public decimal? Latitud { get; set; }
    }
}
