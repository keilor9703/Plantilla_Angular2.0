using Comun.Dtos.Ubicacion;

namespace Datos.Interfaz
{
    public interface IDbUbicacionRepository
    {
        Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct);
        Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct);
    }
}
