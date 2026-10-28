using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Datos.Interfaz.Mfa;
using Negocio.Interfaz.Mfa;

namespace Negocio.Gestion.Mfa
{
    /// <summary>Implementación de la lógica de negocio del panel SI2FA.</summary>
    public class MfaAdminService : IMfaAdminService
    {
        private readonly IMfaAdminRepository _repo;

        public MfaAdminService(IMfaAdminRepository repo)
        {
            _repo = repo;
        }

        public Task<DtoMetricas2Fa> GetMetricasAsync(int ventanaDias, CancellationToken ct) =>
            _repo.GetMetricasAsync(ventanaDias, ct);

        public Task<List<DtoSistema2Fa>> GetSistemasAsync(CancellationToken ct) =>
            _repo.GetSistemasAsync(ct);

        public Task<DtoSistema2Fa?> GetSistemaAsync(long idSistema, CancellationToken ct) =>
            _repo.GetSistemaAsync(idSistema, ct);

        public Task<DtoSistema2FaResult> CrearSistemaAsync(DtoSistema2FaRequest req, string actor, CancellationToken ct) =>
            _repo.CrearSistemaAsync(req, actor, ct);

        public Task<DtoSistema2FaResult> ActualizarSistemaAsync(long idSistema, DtoSistema2FaRequest req, string actor, CancellationToken ct) =>
            _repo.ActualizarSistemaAsync(idSistema, req, actor, ct);

        public Task<DtoSistema2FaResult> CambiarEstadoSistemaAsync(long idSistema, int habilitado, string actor, CancellationToken ct) =>
            _repo.CambiarEstadoSistemaAsync(idSistema, habilitado, actor, ct);

        public Task<DtoSistema2FaResult> RotarSecretoAsync(long idSistema, string actor, CancellationToken ct) =>
            _repo.RotarSecretoAsync(idSistema, actor, ct);

        public Task<List<DtoUsuario2Fa>> GetUsuariosAsync(string? filtro, int pagina, int tamano, CancellationToken ct) =>
            _repo.GetUsuariosAsync(filtro, pagina, tamano, ct);

        public Task<DtoUsuario2FaDetalle?> GetUsuarioAsync(long idMfa, CancellationToken ct) =>
            _repo.GetUsuarioAsync(idMfa, ct);

        public Task<DtoAccion2FaResult> DesbloquearAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            _repo.DesbloquearAsync(idMfa, actor, ip, ct);

        public Task<DtoAccion2FaResult> CambiarEstadoMfaAsync(long idMfa, int estado, string actor, string ip, CancellationToken ct) =>
            _repo.CambiarEstadoMfaAsync(idMfa, estado, actor, ip, ct);

        public Task<DtoAccion2FaResult> ForzarReenrolAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            _repo.ForzarReenrolAsync(idMfa, actor, ip, ct);

        public Task<DtoAccion2FaResult> ResetMfaAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            _repo.ResetMfaAsync(idMfa, actor, ip, ct);

        public Task<DtoAccion2FaResult> LimpiarDispositivosAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            _repo.LimpiarDispositivosAsync(idMfa, actor, ip, ct);

        public Task<List<DtoDispositivo2Fa>> GetDispositivosAsync(long? idMfa, bool soloVigentes, CancellationToken ct) =>
            _repo.GetDispositivosAsync(idMfa, soloVigentes, ct);

        public Task<DtoAccion2FaResult> RevocarDispositivoAsync(long idDevice, string actor, string ip, CancellationToken ct) =>
            _repo.RevocarDispositivoAsync(idDevice, actor, ip, ct);

        public Task<DtoAuditoria2FaPagina> GetAuditoriaAsync(DtoAuditoria2FaFiltro filtro, CancellationToken ct) =>
            _repo.GetAuditoriaAsync(filtro, ct);

        public Task<List<DtoParametro2Fa>> GetParametrosAsync(CancellationToken ct) =>
            _repo.GetParametrosAsync(ct);

        public Task<DtoParametro2FaResult> ActualizarParametroAsync(string clave, string valor, string actor, CancellationToken ct) =>
            _repo.ActualizarParametroAsync(clave, valor, actor, ct);
    }
}
