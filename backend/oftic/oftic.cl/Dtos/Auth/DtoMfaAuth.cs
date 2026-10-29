namespace Comun.Dtos
{
    // ════════════════════════════════════════════════════════════════════════════
    // DTOs estándar de 2FA / MFA (contrato de la librería @policia/mfa).
    // Flujo: credenciales OK → estado MFA → modal en login → token final.
    // Este contrato es el mismo en todos los sistemas de información de la Policía.
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Respuesta del endpoint POST /api/Cuenta/Token cuando el usuario requiere
    /// completar el segundo factor antes de recibir el JWT real.
    /// </summary>
    public class DtoMfaLoginResponse : DtoTokenResponse
    {
        /// <summary>true = credenciales OK pero requiere paso 2FA.</summary>
        public bool RequiresMfa { get; set; }

        /// <summary>
        /// Modo que debe mostrar el frontend:
        /// 'enroll'  → no enrolado: mostrar modal con QR
        /// 'verify'  → enrolado: mostrar modal de verificación TOTP
        /// 'blocked' → bloqueado temporalmente
        /// 'svcdown' → servicio 2FA caído
        /// </summary>
        public string? MfaMode { get; set; }

        /// <summary>
        /// Token de sesión MFA de corta vida (firmado con Jwt:MfaSessionKey, 5 min).
        /// Se envía de vuelta en cada llamada MFA posterior para identificar al usuario
        /// sin que pueda manipular los datos.
        /// </summary>
        public string? MfaSessionToken { get; set; }

        /// <summary>QR code como PNG en base64. Solo presente cuando MfaMode='enroll'.</summary>
        public string? MfaQrBase64 { get; set; }

        /// <summary>Clave manual base32. Solo presente cuando MfaMode='enroll'.</summary>
        public string? MfaManualKey { get; set; }

        /// <summary>Token de enrolamiento opaco devuelto por Api2FA.</summary>
        public string? MfaEnrollToken { get; set; }

        /// <summary>Bloqueo hasta (ISO 8601). Solo presente cuando MfaMode='blocked'.</summary>
        public string? BloqueoHasta { get; set; }
    }

    /// <summary>POST /api/Cuenta/MfaVerify — verificar código TOTP.</summary>
    public class DtoMfaVerifyRequest
    {
        public string MfaSessionToken { get; set; } = "";
        public string Code { get; set; } = "";
        public bool RememberDevice { get; set; }
        public string? DeviceId { get; set; }
    }

    /// <summary>POST /api/Cuenta/MfaEnrollConfirm — confirmar primer enrolamiento.</summary>
    public class DtoMfaEnrollConfirmRequest
    {
        public string MfaSessionToken { get; set; } = "";
        public string Code { get; set; } = "";
        public string EnrollToken { get; set; } = "";
    }

    /// <summary>POST /api/Cuenta/MfaResetRequest — solicitar código de reset por correo.</summary>
    public class DtoMfaResetRequest
    {
        public string MfaSessionToken { get; set; } = "";
    }

    /// <summary>POST /api/Cuenta/MfaResetConfirm — confirmar código de correo y re-enrolar.</summary>
    public class DtoMfaResetConfirmRequest
    {
        public string MfaSessionToken { get; set; } = "";
        public string Code { get; set; } = "";
    }

    /// <summary>Respuesta genérica de los endpoints MFA intermedios.</summary>
    public class DtoMfaStepResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? BloqueoHasta { get; set; }
        public bool IsInfo { get; set; }
        public string? QrBase64 { get; set; }
        public string? ManualKey { get; set; }
        public string? EnrollToken { get; set; }
        /// <summary>JWT final cuando el paso 2FA se completa correctamente.</summary>
        public string? Token { get; set; }
    }
}
