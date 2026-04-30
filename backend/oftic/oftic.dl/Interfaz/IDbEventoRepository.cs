using Comun.Dtos.Eventos;

namespace Datos.Interfaz;

public interface IDbEventoRepository
{
    Task<List<DtoEvento>> GetAllAsync(CancellationToken ct);
    Task<DtoEvento?> GetByIdAsync(long id, CancellationToken ct);
    Task<List<DtoEvento>> GetActivosAsync(DateTime? fechaReferencia, CancellationToken ct);
    Task<DtoEventoResult> CreateAsync(DtoEventoRequest request, string usuario, string maquina, CancellationToken ct);
    Task<DtoEventoResult> UpdateAsync(long id, DtoEventoRequest request, string usuario, string maquina, CancellationToken ct);
    Task<DtoEventoResult> DeleteAsync(long id, string usuario, string maquina, CancellationToken ct);
}
