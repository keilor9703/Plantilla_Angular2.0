using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Comun.Dtos.Administracion;
using Negocio.Gestion.Administracion;
using Negocio.Interfaz;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ofic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class SetupController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IDbUbicacionService _ubicacionService;
        private readonly IDbBrandingService _brandingService;

        public SetupController(
            IConfiguration configuration,
            IWebHostEnvironment env,
            IDbUbicacionService ubicacionService,
            IDbBrandingService brandingService)
        {
            _configuration = configuration;
            _env = env;
            _ubicacionService = ubicacionService;
            _brandingService = brandingService;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var cs = _configuration.GetConnectionString("DbConnection");
            var isConfigured = !string.IsNullOrWhiteSpace(cs);

            return Ok(new
            {
                configured = isConfigured,
                provider = _configuration["DatabaseProvider"] ?? "Oracle",
                connectionStringConfigured = isConfigured
            });
        }

        [HttpGet("unidades")]
        public async Task<IActionResult> GetUnidades(CancellationToken ct)
        {
            try
            {
                var unidades = await _ubicacionService.GetUnidadesOrganigramaAsync(ct);
                return Ok(unidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error cargando unidades: {ex.Message}" });
            }
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] SetupTestRequest request)
        {
            if (request == null) return BadRequest(new { success = false, message = "Datos requeridos." });

            string cs = BuildConnectionString(request);

            try
            {
                var provider = (request.Provider ?? "Oracle").Trim();
                if (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new OracleConnection(cs);
                    await conn.OpenAsync();
                    await conn.CloseAsync();
                }
                else
                {
                    // Demás proveedores si fuera necesario en el futuro
                    return Ok(new { success = false, message = $"El proveedor {provider} requiere paquete de cliente de BD específico." });
                }

                return Ok(new { success = true, message = "¡Conexión a la base de datos establecida con éxito!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error de conexión: {ex.Message}" });
            }
        }

        [HttpPost("save-config")]
        public async Task<IActionResult> SaveConfig([FromBody] SetupSaveRequest request)
        {
            if (request == null) return BadRequest(new { success = false, message = "Datos inválidos." });

            string cs = BuildConnectionString(new SetupTestRequest
            {
                Provider = request.Provider,
                Host = request.Host,
                Port = request.Port,
                DatabaseOrService = request.DatabaseOrService,
                User = request.User,
                Password = request.Password,
                ConnectionStringRaw = request.ConnectionStringRaw
            });

            try
            {
                var appsettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                if (!System.IO.File.Exists(appsettingsPath))
                {
                    return StatusCode(500, new { success = false, message = "No se encontró el archivo appsettings.json." });
                }

                var jsonString = await System.IO.File.ReadAllTextAsync(appsettingsPath);
                var jsonNode = JsonNode.Parse(jsonString);

                if (jsonNode == null) return StatusCode(500, new { success = false, message = "Error leyendo json." });

                // Set ConnectionString
                if (jsonNode["ConnectionStrings"] == null)
                {
                    jsonNode["ConnectionStrings"] = new JsonObject();
                }
                jsonNode["ConnectionStrings"]!["DbConnection"] = cs;

                // Set Provider
                jsonNode["DatabaseProvider"] = request.Provider ?? "Oracle";

                // Set PIP Credentials if provided
                if (jsonNode["ApiSettings"] == null)
                {
                    jsonNode["ApiSettings"] = new JsonObject();
                }
                if (!string.IsNullOrWhiteSpace(request.PipUser))
                {
                    jsonNode["ApiSettings"]!["UsuarioPip"] = request.PipUser.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.PipPassword))
                {
                    jsonNode["ApiSettings"]!["ClavePip"] = request.PipPassword;
                }

                // Set JWT Settings if provided
                if (jsonNode["Jwt"] == null)
                {
                    jsonNode["Jwt"] = new JsonObject();
                }
                if (!string.IsNullOrWhiteSpace(request.JwtKey))
                {
                    jsonNode["Jwt"]!["Key"] = request.JwtKey.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.JwtIssuer))
                {
                    jsonNode["Jwt"]!["Issuer"] = request.JwtIssuer.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.JwtAudience))
                {
                    jsonNode["Jwt"]!["Audience"] = request.JwtAudience.Trim();
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = jsonNode.ToJsonString(options);

                await System.IO.File.WriteAllTextAsync(appsettingsPath, updatedJson);

                // Guardar datos de Branding en base de datos si se diligenciaron
                if (!string.IsNullOrWhiteSpace(request.Sistema) || !string.IsNullOrWhiteSpace(request.NombreSistema))
                {
                    try
                    {
                        var currentList = await _brandingService.GetAllAsync(CancellationToken.None);
                        var current = currentList.FirstOrDefault(c => c.Vigente == 1);

                        var brandingReq = new DtoBrandingRequest
                        {
                            IdBranding = current?.IdBranding ?? 0,
                            Sistema = request.Sistema ?? "SISGE",
                            NombreSistema = request.NombreSistema ?? "Sistema de Gestión",
                            Sigla = request.Sigla ?? "POLICIA",
                            NombreUnidad = request.NombreUnidad ?? "",
                            UnidadSistemaId = request.UnidadSistemaId,
                            LogoFileName = !string.IsNullOrWhiteSpace(request.LogoFileName) ? request.LogoFileName : current?.LogoFileName,
                            FaviconFileName = !string.IsNullOrWhiteSpace(request.FaviconFileName) ? request.FaviconFileName : current?.FaviconFileName,
                            Fondo1FileName = !string.IsNullOrWhiteSpace(request.Fondo1FileName) ? request.Fondo1FileName : current?.Fondo1FileName,
                            Vigente = 1,
                            UsuarioAuditoria = "SETUP_WIZARD",
                            MaquinaAuditoria = Environment.MachineName
                        };

                        if (brandingReq.IdBranding > 0)
                        {
                            await _brandingService.UpdateAsync(brandingReq, CancellationToken.None);
                        }
                        else
                        {
                            await _brandingService.CreateAsync(brandingReq, CancellationToken.None);
                        }
                    }
                    catch (Exception exBranding)
                    {
                        // Si la BD aún no tiene los paquetes, loggear pero continuar con el guardado de appsettings
                        Console.WriteLine($"Aviso al guardar branding inicial: {exBranding.Message}");
                    }
                }

                return Ok(new { success = true, message = "Configuración guardada correctamente. Por favor reinicia la aplicación si es necesario." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al guardar configuración: {ex.Message}" });
            }
        }

        private static string BuildConnectionString(SetupTestRequest r)
        {
            if (!string.IsNullOrWhiteSpace(r.ConnectionStringRaw))
            {
                return r.ConnectionStringRaw.Trim();
            }

            var host = string.IsNullOrWhiteSpace(r.Host) ? "localhost" : r.Host.Trim();
            var port = r.Port <= 0 ? (r.Provider == "Oracle" ? 1521 : 5432) : r.Port;
            var db = r.DatabaseOrService?.Trim() ?? "";
            var user = r.User?.Trim() ?? "";
            var pass = r.Password ?? "";

            if (r.Provider == "Oracle")
            {
                return $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST={host})(PORT={port})))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME={db})));User ID={user};Password={pass};Pooling=true;";
            }

            return $"Server={host};Port={port};Database={db};User Id={user};Password={pass};";
        }
    }

    public class SetupTestRequest
    {
        public string? Provider { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? DatabaseOrService { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? ConnectionStringRaw { get; set; }
        public string? PipUser { get; set; }
        public string? PipPassword { get; set; }
        public string? JwtKey { get; set; }
        public string? JwtIssuer { get; set; }
        public string? JwtAudience { get; set; }
        public string? Sistema { get; set; }
        public string? NombreSistema { get; set; }
        public string? Sigla { get; set; }
        public string? NombreUnidad { get; set; }
        public int UnidadSistemaId { get; set; }
        public string? LogoFileName { get; set; }
        public string? FaviconFileName { get; set; }
        public string? Fondo1FileName { get; set; }
    }

    public class SetupSaveRequest : SetupTestRequest
    {
    }
}
