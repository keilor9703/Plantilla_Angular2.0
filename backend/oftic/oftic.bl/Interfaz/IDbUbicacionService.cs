using Comun.Dtos.Ubicacion;

namespace Negocio.Interfaz
{
    public interface IDbUbicacionService
    {
        Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct);
        Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct);
    }
}
