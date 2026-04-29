using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ofic.Security;

namespace Api.Controllers.Administracion
{
    public class DtoPersonajeMesUploadRequest
    {
        public IFormFile? File { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PublicCors")]
    public class PersonajeMesUploadController : ControllerBase
    {
        private readonly ILogger<PersonajeMesUploadController> _logger;
        private readonly string _uploadsPath;

        private const long MaxImageBytes = 2 * 1024 * 1024; // 2MB
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public PersonajeMesUploadController(ILogger<PersonajeMesUploadController> logger)
        {
            _logger = logger;
            // Ruta absoluta fija para las imágenes de personaje del mes y grupo de alto rendimiento
            // Ajustar para que apunte a: backend/oftic/oftic/uploads/personje-mes-grupo
            // Al ejecutarse desde bin/.../net8.0, subir tres niveles llega al directorio del proyecto
            _uploadsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "uploads", "personje-mes-grupo"));
        }

        [HttpPost("imagen")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImagen([FromForm] DtoPersonajeMesUploadRequest request)
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
                    return BadRequest(new { success = false, message = "El archivo excede 2MB." });
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

                // Validar y crear la carpeta si no existe
                if (!Directory.Exists(_uploadsPath))
                {
                    Directory.CreateDirectory(_uploadsPath);
                }

                var safeName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(_uploadsPath, safeName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verificar que el archivo fue guardado correctamente
                var saved = System.IO.File.Exists(fullPath);
                long savedSize = 0;
                if (saved)
                {
                    var fi = new FileInfo(fullPath);
                    savedSize = fi.Length;
                }

                var publicUrl = $"/api/PersonajeMesUpload/Imagen/{safeName}";
                if (!saved)
                {
                    _logger.LogError("Archivo subido pero no encontrado en disco: {Path}", fullPath);
                    return StatusCode(500, new { success = false, message = "Error al guardar la imagen en el servidor." });
                }

                if (savedSize != file.Length && file.Length > 0)
                {
                    _logger.LogWarning("Tamaño escrito ({SavedSize}) diferente al tamaño del upload ({UploadSize}) para {Path}", savedSize, file.Length, fullPath);
                }

                return Ok(new
                {
                    success = true,
                    url = publicUrl,
                    fileName = $"personje-mes-grupo/{safeName}",
                    message = "Imagen cargada correctamente.",
                    saved = true,
                    savedSize,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo imagen de personaje del mes");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error cargando imagen"
                });
            }
        }

        [HttpGet("Imagen/{fileName}")]
        [EnableCors("PublicCors")]
        public IActionResult GetImagen(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            var fullPath = Path.Combine(_uploadsPath, safeFileName);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { success = false, message = "Imagen no encontrada." });
            }

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
    }
}