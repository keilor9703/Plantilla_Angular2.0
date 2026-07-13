using Comun.Dtos.RecoleccionInformacion;

namespace Datos.Interfaz;

public interface IDbIntervinienteCoestRepository
{
    Task<DtoIntervinienteCoestResult> CreateAsync(DtoIntervinienteCoestRequest request, string maquinaAuditoria, CancellationToken ct);
    Task<DtoIntervinienteCoestResult> DeleteAsync(string intervinienteId, long identificacionModifica, string maquinaModifica, CancellationToken ct);
}
