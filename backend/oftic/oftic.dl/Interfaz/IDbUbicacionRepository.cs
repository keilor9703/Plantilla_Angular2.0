using Comun.Dtos.Ubicacion;
using Comun.Dtos.Administracion;

namespace Datos.Interfaz
{
    public interface IDbUbicacionRepository
    {
        Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct);
        Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct);
        Task<List<DtoUnidadOrg>> GetUnidadesOrganigramaAsync(CancellationToken ct);
    }
}
