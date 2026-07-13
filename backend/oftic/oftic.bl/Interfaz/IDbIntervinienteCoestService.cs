using Comun.Dtos.RecoleccionInformacion;

namespace Negocio.Interfaz;

public interface IDbIntervinienteCoestService
{
    Task<DtoIntervinienteCoestResult> CreateAsync(DtoIntervinienteCoestRequest request, string maquinaAuditoria, CancellationToken ct);
    Task<DtoIntervinienteCoestResult> DeleteAsync(string intervinienteId, long identificacionModifica, string maquinaModifica, CancellationToken ct);
}
