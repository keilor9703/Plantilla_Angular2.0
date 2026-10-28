using System;

namespace Comun.Dtos.Mfa
{
    /// <summary>Resultado de consultar el estado MFA de un usuario en el servicio 2FA (Api2FA).</summary>
    public class DtoMfaEstadoCliente
    {
        public bool Exito { get; set; }
        public int MfaHabilitado { get; set; }
        public int RequireReenroll { get; set; }
        public DateTime? BloqueoHasta { get; set; }

        /// <summary>True cuando el usuario ya está enrolado (MFA habilitado y sin re-enrolamiento pendiente).</summary>
        public bool Enrolado => Exito && MfaHabilitado == 1 && RequireReenroll == 0;
    }

    /// <summary>Datos de inicio de enrolamiento devueltos por el servicio 2FA.</summary>
    public class DtoMfaEnrolCliente
    {
        public bool Exito { get; set; }
        public string QrBase64 { get; set; } = string.Empty;
        public string ManualKey { get; set; } = string.Empty;
        public string EnrollToken { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
    }

    /// <summary>Resultado de una verificación/confirmación de código en el servicio 2FA.</summary>
    public class DtoMfaVerifyCliente
    {
        public bool Ok { get; set; }
        public DateTime? BloqueoHasta { get; set; }
        public string? Mensaje { get; set; }
    }
}
