using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;

namespace Negocio.Interfaz.Mfa
{
    /// <summary>Lógica de negocio del panel administrativo SI2FA.</summary>
    public interface IMfaAdminService
    {
        Task<DtoMetricas2Fa> GetMetricasAsync(int ventanaDias, CancellationToken ct);

        Task<List<DtoSistema2Fa>> GetSistemasAsync(CancellationToken ct);
        Task<DtoSistema2Fa?> GetSistemaAsync(long idSistema, CancellationToken ct);
        Task<DtoSistema2FaResult> CrearSistemaAsync(DtoSistema2FaRequest req, string actor, CancellationToken ct);
        Task<DtoSistema2FaResult> ActualizarSistemaAsync(long idSistema, DtoSistema2FaRequest req, string actor, CancellationToken ct);
        Task<DtoSistema2FaResult> CambiarEstadoSistemaAsync(long idSistema, int habilitado, string actor, CancellationToken ct);
        Task<DtoSistema2FaResult> RotarSecretoAsync(long idSistema, string actor, CancellationToken ct);

        Task<List<DtoUsuario2Fa>> GetUsuariosAsync(string? filtro, int pagina, int tamano, CancellationToken ct);
        Task<DtoUsuario2FaDetalle?> GetUsuarioAsync(long idMfa, CancellationToken ct);
        Task<DtoAccion2FaResult> DesbloquearAsync(long idMfa, string actor, string ip, CancellationToken ct);
        Task<DtoAccion2FaResult> CambiarEstadoMfaAsync(long idMfa, int estado, string actor, string ip, CancellationToken ct);
        Task<DtoAccion2FaResult> ForzarReenrolAsync(long idMfa, string actor, string ip, CancellationToken ct);
        Task<DtoAccion2FaResult> ResetMfaAsync(long idMfa, string actor, string ip, CancellationToken ct);
        Task<DtoAccion2FaResult> LimpiarDispositivosAsync(long idMfa, string actor, string ip, CancellationToken ct);

        Task<List<DtoDispositivo2Fa>> GetDispositivosAsync(long? idMfa, bool soloVigentes, CancellationToken ct);
        Task<DtoAccion2FaResult> RevocarDispositivoAsync(long idDevice, string actor, string ip, CancellationToken ct);

        Task<DtoAuditoria2FaPagina> GetAuditoriaAsync(DtoAuditoria2FaFiltro filtro, CancellationToken ct);

        Task<List<DtoParametro2Fa>> GetParametrosAsync(CancellationToken ct);
        Task<DtoParametro2FaResult> ActualizarParametroAsync(string clave, string valor, string actor, CancellationToken ct);
    }
}
