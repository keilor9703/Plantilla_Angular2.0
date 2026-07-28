using Comun.Dtos.Auditoria;

namespace Negocio.Interfaz
{
    public interface IDbAuditoriaService
    {
        Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct);
    }
}

