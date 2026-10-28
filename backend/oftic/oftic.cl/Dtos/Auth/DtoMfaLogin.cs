namespace Comun.Dtos
{
    /// <summary>Paso 2 del login: verificación del código del segundo factor.</summary>
    public class DtoMfaVerifyLoginReq
    {
        public string? Challenge { get; set; }
        public string? Codigo { get; set; }
        public bool RememberDevice { get; set; }
        public string? DeviceId { get; set; }
    }

    /// <summary>Paso 2 del login (primera vez): confirmación del enrolamiento con el primer código.</summary>
    public class DtoMfaEnrollConfirmLoginReq
    {
        public string? Challenge { get; set; }
        public string? Codigo { get; set; }
        public string? EnrollToken { get; set; }
    }
}
