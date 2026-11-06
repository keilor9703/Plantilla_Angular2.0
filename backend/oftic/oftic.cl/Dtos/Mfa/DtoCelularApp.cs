using System;

namespace Comun.Dtos.Mfa
{
    /// <summary>Celular registrado en la app 2FA (llave de login por QR) de un funcionario.</summary>
    public class DtoCelularApp
    {
        public long IdDispositivo { get; set; }
        public long Identificacion { get; set; }
        public string? UsuarioInstitucional { get; set; }
        public string? Alias { get; set; }
        public string? Modelo { get; set; }
        public string? Plataforma { get; set; }
        public bool Activo { get; set; }
        public DateTime? CreadoAt { get; set; }
        public DateTime? UltimoUsoAt { get; set; }
        public DateTime? RevocadoAt { get; set; }
        public string? RevocadoPor { get; set; }
    }

    /// <summary>Petición de revocación: el motivo es obligatorio (queda en auditoría).</summary>
    public class DtoRevocarCelularRequest
    {
        public string? Motivo { get; set; }
    }
}
