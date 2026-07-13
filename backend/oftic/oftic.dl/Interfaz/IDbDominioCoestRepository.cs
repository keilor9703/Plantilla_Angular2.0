using Comun.Dtos.RecoleccionInformacion;

namespace Datos.Interfaz;

public interface IDbDominioCoestRepository
{
    Task<List<DtoDominioCoest>> GetByPadreIdAsync(decimal padreId, CancellationToken ct);
    Task<List<DtoDominioCoest>> GetByPadreIdAllAsync(decimal padreId, CancellationToken ct);
    Task<bool> SaveDominioAsync(DtoDominioCoest dto, decimal padreId, CancellationToken ct);
    Task<bool> DeleteDominioAsync(decimal id, CancellationToken ct);
    Task<List<DtoCatalogoMedio>> GetMediosAsync(CancellationToken ct);
    Task<List<DtoCatalogoMedio>> GetMediosPorTipoYUnidadAsync(decimal tipoMedioId, decimal idUnidad, CancellationToken ct);
    Task<List<DtoDependencia>> GetDependenciasAsync(CancellationToken ct);
    Task<List<DtoDominioCoest>> GetUnidadesAsync(CancellationToken ct);
}
