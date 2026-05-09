using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ofic.Security;
using Comun.Dtos.Administracion;
using Negocio.Gestion.Administracion;
using System.Text.Json;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class BrandingController : ControllerBase
    {
        private const long MaxLogoBytes = 5 * 1024 * 1024;
        private const long MaxFaviconBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedFaviconExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ico", ".png", ".webp"
        };

        private readonly ILogger<BrandingController> _logger;
        private readonly IDbBrandingService _brandingService;
        private readonly string _rootPath;

        public BrandingController(
            ILogger<BrandingController> logger,
            IHostEnvironment env,
            IConfiguration configuration,
            IDbBrandingService brandingService)
        {
            _logger = logger;
            _brandingService = brandingService;
            _rootPath = ResolveStoragePath(configuration, env.ContentRootPath);
        }

        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicConfig(CancellationToken ct)
        {
            try
            {
                var list = await _brandingService.GetAllAsync(ct);
                var cfg = list.FirstOrDefault(c => c.Vigente == 1) ?? new DtoBranding();

                return Ok(new
                {
                    sistema = cfg.Sistema,
                    nombreSistema = cfg.NombreSistema,
                    systemName = cfg.NombreSistema,
                    sigla = cfg.Sigla,
                    nombreUnidad = cfg.NombreUnidad,
                    logoUrl = BuildLogoUrl(cfg.LogoFileName),
                    faviconUrl = BuildFaviconUrl(cfg.FaviconFileName)
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
                var list = await _brandingService.GetAllAsync(ct);
                var cfg = list.FirstOrDefault(c => c.Vigente == 1) ?? new DtoBranding();

                return Ok(new
                {
                    idBranding = cfg.IdBranding,
                    sistema = cfg.Sistema,
                    nombreSistema = cfg.NombreSistema,
                    sigla = cfg.Sigla,
                    nombreUnidad = cfg.NombreUnidad,
                    unidadSistemaId = cfg.UnidadSistemaId,
                    logoFileName = cfg.LogoFileName,
                    logoUrl = BuildLogoUrl(cfg.LogoFileName),
                    faviconFileName = cfg.FaviconFileName,
                    faviconUrl = BuildFaviconUrl(cfg.FaviconFileName),
                    fondo1FileName = cfg.Fondo1FileName,
                    fondo2FileName = cfg.Fondo2FileName,
                    fondo3FileName = cfg.Fondo3FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando configuracion admin branding");
                return StatusCode(500, new { message = "Error consultando configuracion admin branding." });
            }
        }

        [HttpPost("save-config")]
        [Authorize]
        public async Task<IActionResult> SaveConfig([FromBody] DtoBrandingRequest request, CancellationToken ct)
        {
            try
            {
                if (request is null) return BadRequest(new { success = false, message = "Payload requerido." });

                request.UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA";
                request.MaquinaAuditoria = Environment.MachineName;

                DtoBrandingResult result;
                if (request.IdBranding > 0)
                {
                    result = await _brandingService.UpdateAsync(request, ct);
                }
                else
                {
                    request.Vigente = 1;
                    result = await _brandingService.CreateAsync(request, ct);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando configuracion branding");
                return StatusCode(500, new { success = false, message = "Error guardando configuracion branding." });
            }
        }

        [HttpPost("upload-logo")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLogo([FromForm] BrandingUploadRequest request)
        {
            return await UploadAsset(request, MaxLogoBytes, AllowedLogoExtensions, "logo");
        }

        [HttpPost("upload-favicon")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFavicon([FromForm] BrandingUploadRequest request)
        {
            return await UploadAsset(request, MaxFaviconBytes, AllowedFaviconExtensions, "favicon");
        }

        [HttpGet("logo/{fileName}")]
        [AllowAnonymous]
        public IActionResult GetLogo(string fileName) => GetAsset(fileName, AllowedLogoExtensions, "logo");

        [HttpGet("favicon/{fileName}")]
        [AllowAnonymous]
        public IActionResult GetFavicon(string fileName) => GetAsset(fileName, AllowedFaviconExtensions, "favicon");

        // --- Helpers Internos ---

        private IActionResult GetAsset(string fileName, HashSet<string> allowedExtensions, string assetType)
        {
            var safe = Path.GetFileName(fileName ?? string.Empty);
            var fullPath = Path.Combine(_rootPath, safe);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(safe, out var contentType)) contentType = "application/octet-stream";
            return PhysicalFile(fullPath, contentType);
        }

        private async Task<IActionResult> UploadAsset(BrandingUploadRequest request, long maxBytes, HashSet<string> allowedExtensions, string prefix)
        {
            var file = request?.File;
            if (file is null || file.Length <= 0) return BadRequest(new { success = false, message = "Archivo requerido." });
            if (file.Length > maxBytes) return BadRequest(new { success = false, message = "El archivo excede el tamaño permitido." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) return BadRequest(new { success = false, message = "Formato inválido." });

            Directory.CreateDirectory(_rootPath);
            var fileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_rootPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { success = true, fileName, url = prefix == "logo" ? BuildLogoUrl(fileName) : BuildFaviconUrl(fileName) });
        }

        private static string ResolveStoragePath(IConfiguration configuration, string contentRootPath)
        {
            var configuredBasePath = configuration["AppSettings:UploadsPath"];
            var basePath = string.IsNullOrWhiteSpace(configuredBasePath)
                ? Path.Combine(contentRootPath, "uploads")
                : configuredBasePath.Trim();

            if (!Path.IsPathRooted(basePath))
            {
                basePath = Path.GetFullPath(Path.Combine(contentRootPath, basePath));
            }

            return Path.GetFullPath(Path.Combine(basePath, "branding"));
        }

        private string? BuildLogoUrl(string? fileName) => string.IsNullOrWhiteSpace(fileName) ? null : $"/api/Branding/logo/{Uri.EscapeDataString(fileName)}";
        private string? BuildFaviconUrl(string? fileName) => string.IsNullOrWhiteSpace(fileName) ? null : $"/api/Branding/favicon/{Uri.EscapeDataString(fileName)}";
    }

    // Clases auxiliares para el controlador
    public class BrandingUploadRequest { public IFormFile? File { get; set; } }
}
