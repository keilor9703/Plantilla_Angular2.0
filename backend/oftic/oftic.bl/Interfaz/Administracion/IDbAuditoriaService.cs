using Comun.Dtos.Auditoria;

namespace Negocio.Interfaz
{
    public interface IDbAuditoriaService
    {
        Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct);
        Task<bool> RegistrarAuditoriaAsync(DtoAuditoriaRegistrarRequest request, CancellationToken ct = default);
        bool RegistrarAuditoria(DtoAuditoriaRegistrarRequest request);
    }
}

