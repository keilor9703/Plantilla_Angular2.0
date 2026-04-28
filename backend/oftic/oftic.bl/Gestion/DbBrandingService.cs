using Comun.Dtos.Administracion;
using Datos.Interfaz;
using Negocio.Interfaz;

namespace Negocio.Gestion
{
    public class DbBrandingService : IDbBrandingService
    {
        private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedFaviconExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ico", ".png", ".webp"
        };

        private readonly IDbBrandingRepository _repository;

        public DbBrandingService(IDbBrandingRepository repository)
        {
            _repository = repository;
        }

        public async Task<DtoBrandingConfig> GetConfigAsync(CancellationToken ct)
        {
            return await _repository.ReadConfigAsync(ct);
        }

        public async Task<DtoBrandingSaveResult> SaveConfigAsync(DtoBrandingSaveRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            if (request is null)
            {
                return new DtoBrandingSaveResult { success = false, message = "Payload requerido." };
            }

            var cfg = await _repository.ReadConfigAsync(ct);
            var sistema = (request.sistema ?? cfg.sistema ?? "SISGE").Trim();
            var nombreSistema = (request.nombreSistema ?? cfg.nombreSistema ?? "SISGE").Trim();
            var unidadSistemaId = request.unidadSistemaId ?? cfg.unidadSistemaId;

            cfg.sistema = sistema;
            cfg.nombreSistema = nombreSistema;
            cfg.unidadSistemaId = unidadSistemaId.HasValue && unidadSistemaId.Value > 0 ? unidadSistemaId.Value : null;

            if (!string.IsNullOrWhiteSpace(request.logoFileName))
            {
                var safeLogo = Path.GetFileName(request.logoFileName.Trim());
                var exists = await _repository.AssetExistsAsync(safeLogo, ct);
                if (!exists)
                {
                    return new DtoBrandingSaveResult { success = false, message = "El logo indicado no existe." };
                }

                cfg.logoFileName = safeLogo;
            }

            if (!string.IsNullOrWhiteSpace(request.faviconFileName))
            {
                var safeFavicon = Path.GetFileName(request.faviconFileName.Trim());
                var exists = await _repository.AssetExistsAsync(safeFavicon, ct);
                if (!exists)
                {
                    return new DtoBrandingSaveResult { success = false, message = "El favicon indicado no existe." };
                }

                cfg.faviconFileName = safeFavicon;
            }

            cfg.fondo1FileName = string.IsNullOrWhiteSpace(request.fondo1FileName) ? cfg.fondo1FileName : Path.GetFileName(request.fondo1FileName.Trim());
            cfg.fondo2FileName = string.IsNullOrWhiteSpace(request.fondo2FileName) ? cfg.fondo2FileName : Path.GetFileName(request.fondo2FileName.Trim());
            cfg.fondo3FileName = string.IsNullOrWhiteSpace(request.fondo3FileName) ? cfg.fondo3FileName : Path.GetFileName(request.fondo3FileName.Trim());

            await _repository.WriteConfigAsync(cfg, usuarioAuditoria, maquinaAuditoria, ct);
            return new DtoBrandingSaveResult { success = true, message = "Configuracion de marca guardada correctamente." };
        }

        public Task<DtoBrandingUploadResult> UploadLogoAsync(string originalFileName, byte[] bytes, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            return UploadAssetAsync(originalFileName, bytes, "logo", AllowedLogoExtensions, true, usuarioAuditoria, maquinaAuditoria, ct);
        }

        public Task<DtoBrandingUploadResult> UploadFaviconAsync(string originalFileName, byte[] bytes, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            return UploadAssetAsync(originalFileName, bytes, "favicon", AllowedFaviconExtensions, false, usuarioAuditoria, maquinaAuditoria, ct);
        }

        public Task<DtoBrandingAssetResult> GetLogoAssetAsync(string fileName, CancellationToken ct)
        {
            return GetAssetAsync(fileName, AllowedLogoExtensions, ct);
        }

        public Task<DtoBrandingAssetResult> GetFaviconAssetAsync(string fileName, CancellationToken ct)
        {
            return GetAssetAsync(fileName, AllowedFaviconExtensions, ct);
        }

        private async Task<DtoBrandingUploadResult> UploadAssetAsync(
            string originalFileName,
            byte[] bytes,
            string assetPrefix,
            HashSet<string> allowedExtensions,
            bool isLogo,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;
            if (!allowedExtensions.Contains(extension))
            {
                return new DtoBrandingUploadResult { success = false, message = "Formato invalido para el archivo." };
            }

            var cfg = await _repository.ReadConfigAsync(ct);
            var previous = isLogo ? cfg.logoFileName : cfg.faviconFileName;

            var fileName = await _repository.SaveAssetAsync(assetPrefix, extension, bytes, ct);
            await _repository.DeleteAssetIfExistsAsync(assetPrefix, previous, ct);

            if (isLogo)
            {
                cfg.logoFileName = fileName;
            }
            else
            {
                cfg.faviconFileName = fileName;
            }

            await _repository.WriteConfigAsync(cfg, usuarioAuditoria, maquinaAuditoria, ct);

            return new DtoBrandingUploadResult
            {
                success = true,
                message = isLogo ? "Logo cargado correctamente." : "Favicon cargado correctamente.",
                fileName = fileName
            };
        }

        private async Task<DtoBrandingAssetResult> GetAssetAsync(string fileName, HashSet<string> allowedExtensions, CancellationToken ct)
        {
            var safe = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safe))
            {
                return new DtoBrandingAssetResult
                {
                    success = false,
                    badRequest = true,
                    message = "Nombre de archivo invalido."
                };
            }

            var extension = Path.GetExtension(safe);
            if (!allowedExtensions.Contains(extension))
            {
                return new DtoBrandingAssetResult
                {
                    success = false,
                    badRequest = true,
                    message = "Formato de archivo invalido."
                };
            }

            var fullPath = await _repository.GetAssetPathAsync(allowedExtensions == AllowedLogoExtensions ? "logo" : "favicon", safe, ct);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return new DtoBrandingAssetResult
                {
                    success = false,
                    notFound = true
                };
            }

            return new DtoBrandingAssetResult
            {
                success = true,
                fullPath = fullPath,
                contentType = GetContentTypeByExtension(extension)
            };
        }

        private static string GetContentTypeByExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };
        }
    }
}
