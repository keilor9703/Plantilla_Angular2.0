namespace Servicios.ApiInterfaz
{
    // ── Resultados internos del cliente MFA ───────────────────────────────────

    public class MfaStateResult
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public bool ServiceDown { get; set; }
        public int MfaHabilitado { get; set; }   // 0=no enrolado, 1=enrolado
        public int RequireReenroll { get; set; } // 1=forzar re-enrolamiento
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueoHasta { get; set; }
    }

    public class MfaEnrollResult
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public bool ServiceDown { get; set; }
        public string? QrBase64 { get; set; }
        public string? ManualKey { get; set; }
        public string? EnrollToken { get; set; }
    }

    public class MfaVerifyResult
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public bool ServiceDown { get; set; }
        public bool Blocked { get; set; }
        public DateTime? BloqueoHasta { get; set; }
        public int CodigoError { get; set; }
    }

    public class MfaResetRequestResult
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public bool ServiceDown { get; set; }
    }

    public class MfaResetConfirmResult
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public bool ServiceDown { get; set; }
        public DateTime? AvailableAt { get; set; }
    }

    /// <summary>
    /// Resultado del chequeo de salud del servicio 2FA central.
    /// </summary>
    public class MfaHealthResult
    {
        public bool Up { get; set; }
        public long LatenciaMs { get; set; }
        public string Mensaje { get; set; } = "";
        public DateTime Consultado { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Cliente HTTP para el servicio centralizado Api2FA de la Policía Nacional.
    /// Gestiona la obtención/caché del bearer de sistema y expone los endpoints
    /// necesarios para el flujo 2FA en el login. Es el mismo en todos los sistemas.
    /// </summary>
    public interface IMfaCentralService
    {
        Task<MfaStateResult> GetStateAsync(long identificacion, string usuario, CancellationToken ct = default);
        Task<MfaEnrollResult> EnrollStartAsync(long identificacion, string usuario, CancellationToken ct = default);
        Task<MfaVerifyResult> EnrollConfirmAsync(long identificacion, string usuario, string enrollToken, string code, string ip, CancellationToken ct = default);
        Task<MfaVerifyResult> VerifyAsync(long identificacion, string usuario, string code, bool rememberDevice, string? deviceId, string ip, CancellationToken ct = default);
        Task<bool> IsTrustedAsync(long identificacion, string usuario, string deviceId, CancellationToken ct = default);
        Task<MfaResetRequestResult> ResetRequestAsync(long identificacion, string usuario, string ip, CancellationToken ct = default);
        Task<MfaResetConfirmResult> ResetConfirmAsync(long identificacion, string usuario, string code, string ip, CancellationToken ct = default);
        Task<MfaResetRequestResult> ResetExecuteAsync(long identificacion, string usuario, string ip, CancellationToken ct = default);

        /// <summary>Verifica la disponibilidad del servicio 2FA central (health).</summary>
        Task<MfaHealthResult> HealthAsync(CancellationToken ct = default);
    }
}
