using Comun.Dtos.Administracion;

namespace Negocio.Interfaz
{
    public interface IDbBrandingService
    {
        Task<DtoBrandingConfig> GetConfigAsync(CancellationToken ct);
        Task<DtoBrandingSaveResult> SaveConfigAsync(DtoBrandingSaveRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoBrandingUploadResult> UploadLogoAsync(string originalFileName, byte[] bytes, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoBrandingUploadResult> UploadFaviconAsync(string originalFileName, byte[] bytes, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<DtoBrandingAssetResult> GetLogoAssetAsync(string fileName, CancellationToken ct);
        Task<DtoBrandingAssetResult> GetFaviconAssetAsync(string fileName, CancellationToken ct);
    }
}
