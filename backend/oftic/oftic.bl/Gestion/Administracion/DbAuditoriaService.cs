using Comun.Dtos.Auditoria;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion
{
    public class DbAuditoriaService : IDbAuditoriaService
    {
        private readonly IDbAuditoriaRepository _repository;

        public DbAuditoriaService(IDbAuditoriaRepository repository)
        {
            _repository = repository;
        }

        public async Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct)
        {
            filtro ??= new DtoAuditoriaFiltroRequest();
            if (filtro.Page <= 0) filtro.Page = 1;
            if (filtro.PageSize <= 0) filtro.PageSize = 10;
            if (filtro.PageSize > 100) filtro.PageSize = 100;
            return await _repository.GetBitacoraAsync(filtro, ct);
        }

        public Task<bool> RegistrarAuditoriaAsync(DtoAuditoriaRegistrarRequest request, CancellationToken ct = default)
            => _repository.RegistrarAuditoriaAsync(request, ct);

        public bool RegistrarAuditoria(DtoAuditoriaRegistrarRequest request)
            => _repository.RegistrarAuditoria(request);
    }
}

