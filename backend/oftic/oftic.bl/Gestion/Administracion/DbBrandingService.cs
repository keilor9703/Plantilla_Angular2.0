using Comun.Dtos.Administracion;
using Datos.Interfaz;

namespace Negocio.Gestion.Administracion
{
    public interface IDbBrandingService
    {
        Task<List<DtoBranding>> GetAllAsync(CancellationToken ct);
        Task<DtoBrandingResult> CreateAsync(DtoBrandingRequest request, CancellationToken ct);
        Task<DtoBrandingResult> UpdateAsync(DtoBrandingRequest request, CancellationToken ct);
        Task<DtoBrandingResult> DeleteLogicalAsync(int id, string usuario, string maquina, CancellationToken ct);
    }

    public class DbBrandingService : IDbBrandingService
    {
        private readonly IDbBrandingRepository _repository;

        public DbBrandingService(IDbBrandingRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DtoBranding>> GetAllAsync(CancellationToken ct)
        {
            return await _repository.GetAllAsync(ct);
        }

        public async Task<DtoBrandingResult> CreateAsync(DtoBrandingRequest request, CancellationToken ct)
        {
            return await _repository.CreateAsync(request, ct);
        }

        public async Task<DtoBrandingResult> UpdateAsync(DtoBrandingRequest request, CancellationToken ct)
        {
            return await _repository.UpdateAsync(request, ct);
        }

        public async Task<DtoBrandingResult> DeleteLogicalAsync(int id, string usuario, string maquina, CancellationToken ct)
        {
            return await _repository.DeleteLogicalAsync(id, usuario, maquina, ct);
        }
    }
}
