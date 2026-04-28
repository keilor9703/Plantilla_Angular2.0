using Comun.Dtos.Administracion;

namespace Datos.Interfaz
{
    public interface IDbBrandingRepository
    {
        Task<DtoBrandingConfig> ReadConfigAsync(CancellationToken ct);
        Task WriteConfigAsync(DtoBrandingConfig config, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct);
        Task<string> SaveAssetAsync(string assetPrefix, string extension, byte[] bytes, CancellationToken ct);
        Task<bool> AssetExistsAsync(string fileName, CancellationToken ct);
        Task<string?> GetAssetPathAsync(string assetPrefix, string fileName, CancellationToken ct);
        Task DeleteAssetIfExistsAsync(string assetPrefix, string? fileName, CancellationToken ct);
    }
}
