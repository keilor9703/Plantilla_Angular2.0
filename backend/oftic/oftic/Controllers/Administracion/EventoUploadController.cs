using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ofic.Security;

namespace ofic.Controllers.Administracion
{
    public class DtoEventoUploadRequest
    {
        public IFormFile? File { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class EventoUploadController : ControllerBase
    {
        private readonly ILogger<EventoUploadController> _logger;
        private readonly string _uploadsPath;

        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public EventoUploadController(IConfiguration configuration, IWebHostEnvironment environment, ILogger<EventoUploadController> logger)
        {
            _logger = logger;
            // Mantener la misma convención local del proyecto:
            // backend/oftic/oftic/uploads/eventos/yyyy-MM/archivo.ext
            _uploadsPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "uploads", "eventos"));
        }

        [HttpPost("imagen")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadImagen([FromForm] DtoEventoUploadRequest request)
        {
            try
            {
                var file = request?.File;
                if (file is null || file.Length <= 0)
                {
                    return BadRequest(new { success = false, message = "Archivo requerido." });
                }

                if (file.Length > MaxImageBytes)
                {
                    return BadRequest(new { success = false, message = "La imagen excede 5MB." });
                }

                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
                if (!AllowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Formato inválido. Use JPG, JPEG, PNG o WEBP." });
                }

                if (!SecurityGuards.HasValidImageSignature(file, extension))
                {
                    return BadRequest(new { success = false, message = "Firma de archivo inválida para imagen." });
                }

                Directory.CreateDirectory(_uploadsPath);
                var monthFolder = DateTime.Now.ToString("yyyy-MM");
                var monthPath = Path.Combine(_uploadsPath, monthFolder);
                Directory.CreateDirectory(monthPath);

                var safeName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(monthPath, safeName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var publicUrl = $"/uploads/eventos/{monthFolder}/{safeName}";
                return Ok(new
                {
                    success = true,
                    url = publicUrl,
                    fileName = safeName,
                    message = "Imagen cargada correctamente."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo imagen de evento");
                return StatusCode(500, new { success = false, message = "Error cargando imagen." });
            }
        }

        [HttpGet("Imagen/{fileName}")]
        [EnableCors("PublicCors")]
        public IActionResult GetImagen(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            var fullPath = FindImagePath(safeFileName);

            if (string.IsNullOrWhiteSpace(fullPath) || !System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "Imagen no encontrada." });

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }

        private string? FindImagePath(string safeFileName)
        {
            var rootCandidate = Path.Combine(_uploadsPath, safeFileName);
            if (System.IO.File.Exists(rootCandidate))
            {
                return rootCandidate;
            }

            var matches = Directory.GetFiles(_uploadsPath, safeFileName, SearchOption.AllDirectories);
            return matches.Length == 0 ? null : matches[0];
        }
    }
}
