using Comun.Dtos.RecoleccionInformacion;

namespace Negocio.Interfaz;

public interface IDbMedioCoestService
{
    Task<List<DtoMedioCoest>> GetByNoticiaAsync(string noticiaId, CancellationToken ct);
    Task<DtoMedioCoestResult> CreateAsync(DtoMedioCoestRequest request, string maquinaAuditoria, CancellationToken ct);
    Task<DtoMedioCoestResult> DeleteAsync(string medioId, long identificacionModifica, string maquinaModifica, CancellationToken ct);
}
