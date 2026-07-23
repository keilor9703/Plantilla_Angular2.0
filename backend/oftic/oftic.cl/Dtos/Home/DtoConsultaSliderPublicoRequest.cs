using System;

namespace Comun.Dtos
{
    public class DtoConsultaSliderPublicoRequest
    {
        public string FUENTE_TRANSACCION { get; set; } = string.Empty;
        public string SISTEMA_INFORMACION { get; set; } = string.Empty;
        public string IP_MAQUINA { get; set; } = string.Empty;
        public string USUARIO { get; set; } = string.Empty;
    }
}
