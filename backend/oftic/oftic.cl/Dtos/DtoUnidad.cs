namespace Comun.Dtos
{
    public class DtoUnidad
    {
        public long IdUnidad { get; set; }
        public string Sigla { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public long Value { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
