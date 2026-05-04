using Comun.Dtos.Administracion;

namespace Datos.Interfaz
{
    public interface IDbBrandingRepository
    {
        Task<List<DtoBranding>> GetAllAsync(CancellationToken ct);
        Task<DtoBrandingResult> CreateAsync(DtoBrandingRequest request, CancellationToken ct);
        Task<DtoBrandingResult> UpdateAsync(DtoBrandingRequest request, CancellationToken ct);
        Task<DtoBrandingResult> DeleteLogicalAsync(int id, string usuario, string maquina, CancellationToken ct);
    }
}
