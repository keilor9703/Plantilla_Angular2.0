using Comun.Dtos.Ubicacion;
using Datos.Interfaz;
using Negocio.Interfaz;

using Comun.Dtos.Administracion;

namespace Negocio.Gestion
{
    public class DbUbicacionService : IDbUbicacionService
    {
        private readonly IDbUbicacionRepository _repository;

        public DbUbicacionService(IDbUbicacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DtoUnidadOrg>> GetUnidadesOrganigramaAsync(CancellationToken ct)
        {
            return await _repository.GetUnidadesOrganigramaAsync(ct);
        }

        public async Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct)
        {
            return await _repository.GetDepartamentosAsync(ct);
        }

        public async Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct)
        {
            if (idDepartamento.HasValue && idDepartamento.Value <= 0)
            {
                return new List<DtoMunicipio>();
            }

            return await _repository.GetMunicipiosByDepartamentoAsync(idDepartamento, ct);
        }
    }
}
