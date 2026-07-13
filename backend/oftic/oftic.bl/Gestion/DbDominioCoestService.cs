using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion;

public class DbDominioCoestService : IDbDominioCoestService
{
    private readonly IDbDominioCoestRepository _repository;

    public DbDominioCoestService(IDbDominioCoestRepository repository)
    {
        _repository = repository;
    }

    public Task<List<DtoDominioCoest>> GetByPadreIdAsync(decimal padreId, CancellationToken ct)
        => _repository.GetByPadreIdAsync(padreId, ct);

    public Task<List<DtoDominioCoest>> GetByPadreIdAllAsync(decimal padreId, CancellationToken ct)
        => _repository.GetByPadreIdAllAsync(padreId, ct);

    public Task<bool> SaveDominioAsync(DtoDominioCoest dto, decimal padreId, CancellationToken ct)
        => _repository.SaveDominioAsync(dto, padreId, ct);

    public Task<bool> DeleteDominioAsync(decimal id, CancellationToken ct)
        => _repository.DeleteDominioAsync(id, ct);

    public Task<List<DtoCatalogoMedio>> GetMediosAsync(CancellationToken ct)
        => _repository.GetMediosAsync(ct);

    public Task<List<DtoCatalogoMedio>> GetMediosPorTipoYUnidadAsync(decimal tipoMedioId, decimal idUnidad, CancellationToken ct)
        => _repository.GetMediosPorTipoYUnidadAsync(tipoMedioId, idUnidad, ct);

    public Task<List<DtoDependencia>> GetDependenciasAsync(CancellationToken ct)
        => _repository.GetDependenciasAsync(ct);

    public Task<List<DtoDominioCoest>> GetUnidadesAsync(CancellationToken ct)
        => _repository.GetUnidadesAsync(ct);
}
