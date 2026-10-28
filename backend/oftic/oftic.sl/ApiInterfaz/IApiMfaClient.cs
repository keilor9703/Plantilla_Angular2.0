using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;

namespace Servicios.ApiInterfaz
{
    /// <summary>
    /// Cliente del servicio centralizado de doble autenticación (Api2FA). SI2FA lo consume como
    /// un sistema integrado más: obtiene un token de servicio (firma HMAC) y usa los endpoints
    /// ApiMfa para enrolar/verificar el segundo factor de los administradores del panel.
    /// </summary>
    public interface IApiMfaClient
    {
        /// <summary>Obtiene un token de servicio (JWT) del Api2FA firmando con el secreto del sistema SI2FA.</summary>
        Task<string?> ObtenerTokenAsync(string usuario, long identificacion, CancellationToken ct);

        Task<DtoMfaEstadoCliente> GetStateAsync(string bearer, long identificacion, string usuario, CancellationToken ct);
        Task<DtoMfaEnrolCliente> EnrollStartAsync(string bearer, long identificacion, string usuario, CancellationToken ct);
        Task<DtoMfaVerifyCliente> EnrollConfirmAsync(string bearer, long identificacion, string usuario, string enrollToken, string code, string ip, CancellationToken ct);
        Task<DtoMfaVerifyCliente> VerifyAsync(string bearer, long identificacion, string usuario, string code, string ip, CancellationToken ct);

        // ---- Restablecimiento del segundo factor (autoservicio) ----
        Task<DtoMfaVerifyCliente> ResetRequestAsync(string bearer, long identificacion, string usuario, string ip, CancellationToken ct);
        Task<DtoMfaVerifyCliente> ResetConfirmAsync(string bearer, long identificacion, string usuario, string code, string ip, CancellationToken ct);
        Task<DtoMfaVerifyCliente> ResetExecuteAsync(string bearer, long identificacion, string usuario, string ip, CancellationToken ct);
    }
}
