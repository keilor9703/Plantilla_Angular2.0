using Comun.Dtos.RecoleccionInformacion;

namespace Datos.Interfaz;

public interface IDbMedioCoestRepository
{
    Task<List<DtoMedioCoest>> GetByNoticiaAsync(string noticiaId, CancellationToken ct);
    Task<DtoMedioCoestResult> CreateAsync(DtoMedioCoestRequest request, string maquinaAuditoria, CancellationToken ct);
    Task<DtoMedioCoestResult> DeleteAsync(string medioId, long identificacionModifica, string maquinaModifica, CancellationToken ct);
    Task<DtoMedioCoestResult> ActualizarVoceriaAsync(string medioId, decimal idVoceria, CancellationToken ct);
}
