using System;

namespace Comun.Dtos.Mfa
{
    /// <summary>Parámetro configurable del servicio 2FA (tabla mfa_parametros).</summary>
    public class DtoParametro2Fa
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Tipo { get; set; } = "INT";
        public string? Descripcion { get; set; }
        public string? Unidad { get; set; }
        public string? Grupo { get; set; }
        public int? MinValor { get; set; }
        public int? MaxValor { get; set; }
        public DateTime? ActualizadoAt { get; set; }
        public string? UsuarioModifica { get; set; }
    }

    public class DtoParametro2FaUpdate
    {
        public string Valor { get; set; } = string.Empty;
    }

    public class DtoParametro2FaResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
