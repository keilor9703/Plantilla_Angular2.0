using Comun.Dtos.RecoleccionInformacion;

namespace Datos.Interfaz;

public interface IDbHechoNoticiosoRepository
{
    Task<List<DtoHechoNoticioso>> GetAllAsync(int anio, CancellationToken ct);
    Task<List<DtoHechoNoticioso>> GetByUnidadAsync(string siglaUnidad, int anio, CancellationToken ct);
    Task<DtoHechoNoticioso?> GetByIdAsync(string id, CancellationToken ct);
    Task<List<DtoAnioItem>> GetAniosAsync(CancellationToken ct);
    Task<DtoHechoNoticiosoResult> CreateAsync(DtoHechoNoticiosoRequest request, string maquinaAuditoria, CancellationToken ct);
    Task<DtoHechoNoticiosoResult> DeleteAsync(string id, long identificacionModifica, string maquinaModifica, CancellationToken ct);
}
