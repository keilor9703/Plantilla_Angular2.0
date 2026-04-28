using Comun.Dtos.Administracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;
using ofic.Models.Administracion;
using ofic.Security;
using System.Security.Claims;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class BrandingController : ControllerBase
    {
        private const long MaxLogoBytes = 5 * 1024 * 1024;
        private const long MaxFaviconBytes = 2 * 1024 * 1024;

        private readonly ILogger<BrandingController> _logger;
        private readonly IDbBrandingService _service;

        public BrandingController(ILogger<BrandingController> logger, IDbBrandingService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicConfig(CancellationToken ct)
        {
            try
            {
                var cfg = await _service.GetConfigAsync(ct);
                return Ok(new
                {
                    sistema = cfg.sistema,
                    nombreSistema = cfg.nombreSistema,
                    unidadSistemaId = cfg.unidadSistemaId,
                    systemName = cfg.nombreSistema,
                    logoUrl = BuildLogoUrl(cfg.logoFileName),
                    faviconUrl = BuildFaviconUrl(cfg.faviconFileName),
                    fondo1FileName = cfg.fondo1FileName,
                    fondo2FileName = cfg.fondo2FileName,
                    fondo3FileName = cfg.fondo3FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando configuracion branding");
                return StatusCode(500, new { message = "Error consultando configuracion branding." });
            }
        }

        [HttpGet("admin-config")]
        [Authorize]
        public async Task<IActionResult> GetAdminConfig(CancellationToken ct)
        {
            try
            {
                var cfg = await _service.GetConfigAsync(ct);
                return Ok(new
                {
                    sistema = cfg.sistema,
                    nombreSistema = cfg.nombreSistema,
                    unidadSistemaId = cfg.unidadSistemaId,
                    systemName = cfg.nombreSistema,
                    logoFileName = cfg.logoFileName,
                    logoUrl = BuildLogoUrl(cfg.logoFileName),
                    faviconFileName = cfg.faviconFileName,
                    faviconUrl = BuildFaviconUrl(cfg.faviconFileName),
                    fondo1FileName = cfg.fondo1FileName,
                    fondo2FileName = cfg.fondo2FileName,
                    fondo3FileName = cfg.fondo3FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando configuracion admin branding");
                return StatusCode(500, new { message = "Error consultando configuracion admin branding." });
            }
        }

        [HttpGet("logo/{fileName}")]
        [AllowAnonymous]
        public Task<IActionResult> GetLogo([FromRoute] string fileName, CancellationToken ct)
        {
            return GetAssetAsync(() => _service.GetLogoAssetAsync(fileName, ct), "logo");
        }

        [HttpGet("favicon/{fileName}")]
        [AllowAnonymous]
        public Task<IActionResult> GetFavicon([FromRoute] string fileName, CancellationToken ct)
        {
            return GetAssetAsync(() => _service.GetFaviconAssetAsync(fileName, ct), "favicon");
        }

        [HttpPost("upload-logo")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxLogoBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxLogoBytes)]
        public Task<IActionResult> UploadLogo([FromForm] BrandingUploadRequest request, CancellationToken ct)
        {
            var (usuario, maquina) = ObtenerAuditoria();
            return UploadAssetAsync(
                request,
                MaxLogoBytes,
                isLogo: true,
                ct,
                (originalName, bytes, token) => _service.UploadLogoAsync(originalName, bytes, usuario, maquina, token));
        }

        [HttpPost("upload-favicon")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFaviconBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFaviconBytes)]
        public Task<IActionResult> UploadFavicon([FromForm] BrandingUploadRequest request, CancellationToken ct)
        {
            var (usuario, maquina) = ObtenerAuditoria();
            return UploadAssetAsync(
                request,
                MaxFaviconBytes,
                isLogo: false,
                ct,
                (originalName, bytes, token) => _service.UploadFaviconAsync(originalName, bytes, usuario, maquina, token));
        }

        [HttpPost("save-config")]
        [Authorize]
        public async Task<IActionResult> SaveConfig([FromBody] DtoBrandingSaveRequest request, CancellationToken ct)
        {
            try
            {
                if (request is null)
                {
                    return BadRequest(new { success = false, message = "Payload requerido." });
                }

                var sistema = (request.sistema ?? string.Empty).Trim();
                var nombreSistema = (request.nombreSistema ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(sistema))
                {
                    if (sistema.Length > 10)
                    {
                        return BadRequest(new { success = false, message = "El campo Sistema solo permite hasta 10 caracteres." });
                    }

                    if (!SecurityGuards.IsSafeText(sistema, 10))
                    {
                        return BadRequest(new { success = false, message = "El campo Sistema contiene caracteres no permitidos." });
                    }
                }

                if (!string.IsNullOrWhiteSpace(nombreSistema))
                {
                    if (nombreSistema.Length > 90)
                    {
                        return BadRequest(new { success = false, message = "Nombre del sistema solo permite hasta 90 caracteres." });
                    }

                    if (!SecurityGuards.IsSafeText(nombreSistema, 90))
                    {
                        return BadRequest(new { success = false, message = "Nombre del sistema contiene caracteres no permitidos." });
                    }
                }

                if (request.unidadSistemaId.HasValue && request.unidadSistemaId.Value < 0)
                {
                    return BadRequest(new { success = false, message = "La Unidad del sistema es invalida." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.SaveConfigAsync(request, usuario, maquina, ct);
                if (!result.success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando configuracion branding");
                return StatusCode(500, new { success = false, message = "Error guardando configuracion branding." });
            }
        }

        private async Task<IActionResult> GetAssetAsync(
            Func<Task<DtoBrandingAssetResult>> assetTask,
            string assetType)
        {
            try
            {
                var asset = await assetTask();
                if (!asset.success)
                {
                    if (asset.notFound)
                    {
                        return NotFound();
                    }

                    if (asset.badRequest)
                    {
                        return BadRequest(new { message = asset.message });
                    }

                    return StatusCode(500, new { message = $"Error devolviendo {assetType}." });
                }

                return PhysicalFile(asset.fullPath!, asset.contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error devolviendo {AssetType} branding", assetType);
                return StatusCode(500, new { message = $"Error devolviendo {assetType}." });
            }
        }

        private async Task<IActionResult> UploadAssetAsync(
            BrandingUploadRequest request,
            long maxBytes,
            bool isLogo,
            CancellationToken ct,
            Func<string, byte[], CancellationToken, Task<DtoBrandingUploadResult>> uploadHandler)
        {
            try
            {
                var file = request?.File;
                if (file is null || file.Length <= 0)
                {
                    return BadRequest(new { success = false, message = "Archivo requerido." });
                }

                if (file.Length > maxBytes)
                {
                    return BadRequest(new { success = false, message = "El archivo excede el tamano permitido." });
                }

                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
                if (!SecurityGuards.HasValidImageSignature(file, extension))
                {
                    return BadRequest(new { success = false, message = "Firma de archivo invalida para la imagen." });
                }

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream, ct);
                var bytes = stream.ToArray();

                var result = await uploadHandler(file.FileName, bytes, ct);
                if (!result.success)
                {
                    return BadRequest(result);
                }

                return Ok(new
                {
                    success = true,
                    fileName = result.fileName,
                    logoUrl = isLogo && result.fileName is not null
                        ? BuildLogoUrl(result.fileName)
                        : null,
                    faviconUrl = !isLogo && result.fileName is not null
                        ? BuildFaviconUrl(result.fileName)
                        : null,
                    message = result.message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando asset branding");
                return StatusCode(500, new { success = false, message = "Error cargando archivo." });
            }
        }

        private static string? BuildLogoUrl(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var safe = Path.GetFileName(fileName);
            return $"/api/Branding/logo/{Uri.EscapeDataString(safe)}";
        }

        private static string? BuildFaviconUrl(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var safe = Path.GetFileName(fileName);
            return $"/api/Branding/favicon/{Uri.EscapeDataString(safe)}";
        }

        private (string usuario, string maquina) ObtenerAuditoria()
        {
            var usuario = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("unique_name")
                ?? User.FindFirstValue("nameid")
                ?? "sistema";

            var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? Environment.MachineName
                ?? "N/A";

            return (usuario, maquina);
        }
    }
}
