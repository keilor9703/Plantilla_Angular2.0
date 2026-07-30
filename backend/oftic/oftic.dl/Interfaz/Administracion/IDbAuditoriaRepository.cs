using Comun.Dtos.Auditoria;

namespace Datos.Interfaz
{
    public interface IDbAuditoriaRepository
    {
        Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct);
        Task<bool> RegistrarAuditoriaAsync(DtoAuditoriaRegistrarRequest request, CancellationToken ct = default);
        bool RegistrarAuditoria(DtoAuditoriaRegistrarRequest request);
    }
}

