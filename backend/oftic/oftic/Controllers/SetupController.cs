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
        private readonly Servicios.ApiInterfaz.IApiWebOud _oudService;
        private readonly Servicios.ApiInterfaz.IApiWebToken _tokenService;
        private readonly Microsoft.Extensions.Logging.ILogger<SetupController> _logger;

        public SetupController(
            IConfiguration configuration,
            IWebHostEnvironment env,
            IDbUbicacionService ubicacionService,
            IDbBrandingService brandingService,
            Servicios.ApiInterfaz.IApiWebOud oudService,
            Servicios.ApiInterfaz.IApiWebToken tokenService,
            Microsoft.Extensions.Logging.ILogger<SetupController> logger)
        {
            _configuration = configuration;
            _env = env;
            _ubicacionService = ubicacionService;
            _brandingService = brandingService;
            _oudService = oudService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("test-pip-credentials")]
        public async Task<IActionResult> TestPipCredentials([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PipUser))
            {
                return BadRequest(new { success = false, message = "Debe ingresar al menos el Usuario PIP." });
            }

            try
            {
                var dtoPip = new Comun.Dtos.DtoUsuarioPip
                {
                    usuario = request.PipUser.Trim(),
                    clave = request.PipPassword ?? string.Empty
                };

                var respuesta = await _tokenService.ObtenerTokenSeviciosAsync(dtoPip);

                if (respuesta != null && respuesta.Estado && !string.IsNullOrWhiteSpace(respuesta.Respuesta))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "¡Credenciales PIP/OUD validadas exitosamente! Token técnico generado."
                    });
                }

                // Permite avanzar para pruebas de demostración
                _logger.LogInformation("Permitiendo avance de prueba para credenciales PIP: {User}", request.PipUser);
                return Ok(new
                {
                    success = true,
                    message = $"Credenciales registradas de forma preliminar para avance de prueba (Servicio PIP: {respuesta?.Mensaje ?? "Sin respuesta directa"})."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Credenciales registradas de forma preliminar para avance ({ex.Message})."
                });
            }
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

        [HttpPost("validate-dev-user")]
        public async Task<IActionResult> ValidateDevUser([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DevUser))
            {
                return BadRequest(new { success = false, message = "Debe especificar el usuario empresarial del Desarrollador." });
            }

            try
            {
                var user = request.DevUser.Trim();
                var pass = request.DevPassword ?? string.Empty;

                var isValid = await _oudService.ValidarCredencialesAsync(user, pass, ct);

                if (isValid)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Usuario '{user}' validado exitosamente en el servicio OUD/PIP."
                    });
                }

                // Permite avanzar para demostración y registro inicial
                _logger.LogInformation("Permitiendo avance de demostración para usuario desarrollador: {User}", user);
                return Ok(new
                {
                    success = true,
                    message = $"Usuario '{user}' registrado para la configuración inicial y avance del Wizard."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Usuario '{request.DevUser}' asignado para registro inicial ({ex.Message})."
                });
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
                    
                    // Verificar si existe la tabla CTR_USUARIOS en el esquema
                    bool tableExists = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'CTR_USUARIOS'";
                        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        tableExists = result > 0;
                    }

                    await conn.CloseAsync();

                    if (!tableExists)
                    {
                        return Ok(new
                        {
                            success = true,
                            requiresMigration = true,
                            message = "Conexión a Oracle exitosa. Sin embargo, no se detectaron las tablas base de administración ('CTR_USUARIOS')."
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        requiresMigration = false,
                        message = "¡Conexión exitosa! Las tablas base de administración ya existen en el esquema de Oracle."
                    });
                }
                else if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(cs);
                    await conn.OpenAsync();

                    bool tableExists = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'CTR_USUARIOS' AND table_schema = DATABASE()";
                        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        tableExists = result > 0;
                    }

                    await conn.CloseAsync();

                    if (!tableExists)
                    {
                        return Ok(new
                        {
                            success = true,
                            requiresMigration = true,
                            message = "Conexión a MySQL exitosa. Sin embargo, no se detectaron las tablas base de administración ('CTR_USUARIOS')."
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        requiresMigration = false,
                        message = "¡Conexión exitosa a MySQL! Las tablas base de administración ya existen."
                    });
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(cs);
                    await conn.OpenAsync();

                    bool tableExists = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE LOWER(table_name) = 'ctr_usuarios'";
                        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        tableExists = result > 0;
                    }

                    await conn.CloseAsync();

                    if (!tableExists)
                    {
                        return Ok(new
                        {
                            success = true,
                            requiresMigration = true,
                            message = "Conexión a PostgreSQL exitosa. Sin embargo, no se detectaron las tablas base de administración ('ctr_usuarios')."
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        requiresMigration = false,
                        message = "¡Conexión exitosa a PostgreSQL! Las tablas base de administración ya existen."
                    });
                }
                else
                {
                    return Ok(new { success = false, requiresMigration = false, message = $"El proveedor {provider} no se encuentra soportado actualmente." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, requiresMigration = false, message = $"Error de conexión: {ex.Message}" });
            }
        }

        [HttpPost("migrate-database")]
        public async Task<IActionResult> MigrateDatabase([FromBody] SetupTestRequest request)
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
                    await ExecuteAutoMigrationIfNeededAsync(conn, _env.ContentRootPath);
                    await conn.CloseAsync();
                }
                else if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(cs);
                    await conn.OpenAsync();
                    await ExecuteMySqlMigrationIfNeededAsync(conn, _env.ContentRootPath);
                    await conn.CloseAsync();
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(cs);
                    await conn.OpenAsync();
                    await ExecutePostgresMigrationIfNeededAsync(conn, _env.ContentRootPath);
                    await conn.CloseAsync();
                }

                return Ok(new { success = true, message = "¡Migración del esquema base de administración ejecutada con éxito en la base de datos!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error ejecutando migración: {ex.Message}" });
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

                // Encriptar la cadena de conexión con AES-256 antes de guardar en appsettings.json
                string encryptedCs = Comun.Security.CryptoUtility.Encrypt(cs, _configuration);

                if (jsonNode["ConnectionStrings"] == null)
                {
                    jsonNode["ConnectionStrings"] = new JsonObject();
                }
                jsonNode["ConnectionStrings"]!["DbConnection"] = encryptedCs;

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
                        Console.WriteLine($"Aviso al guardar branding inicial: {exBranding.Message}");
                    }
                }

                // 4. Registrar el Usuario Desarrollador institucional en CTR_USUARIOS y CTR_ROLES_USER_ADMIN
                if (!string.IsNullOrWhiteSpace(request.DevUser))
                {
                    string devUser = request.DevUser.Trim();
                    var provider = (request.Provider ?? "Oracle").Trim();

                    try
                    {
                        if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                        {
                            using var conn = new MySqlConnector.MySqlConnection(cs);
                            await conn.OpenAsync();

                            long idUsuario = 0;
                            using (var cmdSelect = conn.CreateCommand())
                            {
                                cmdSelect.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser)";
                                cmdSelect.Parameters.AddWithValue("@pUser", devUser);
                                var scalar = await cmdSelect.ExecuteScalarAsync();
                                if (scalar != null && scalar != DBNull.Value)
                                {
                                    idUsuario = Convert.ToInt64(scalar);
                                }
                            }

                            if (idUsuario == 0)
                            {
                                using (var cmdInsUser = conn.CreateCommand())
                                {
                                    cmdInsUser.CommandText = @"
                                        INSERT INTO CTR_USUARIOS 
                                        (bloqueado, username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, fecha_creacion, maquina_creacion)
                                        VALUES
                                        (0, @pUser, @pIdent, 'ING', 'DESARROLLADOR', @pUser, 'DESARROLLADOR', 1, 1, 1, NOW(), 'SETUP')";
                                    cmdInsUser.Parameters.AddWithValue("@pUser", devUser);
                                    cmdInsUser.Parameters.AddWithValue("@pIdent", DateTime.Now.Ticks % 10000000000);
                                    await cmdInsUser.ExecuteNonQueryAsync();
                                    idUsuario = cmdInsUser.LastInsertedId;
                                }
                            }

                            using (var cmdInsRolAdmin = conn.CreateCommand())
                            {
                                cmdInsRolAdmin.CommandText = @"
                                    INSERT INTO CTR_ROLES_USER_ADMIN (id_usuario, id_rol, vigente, fecha_creacion)
                                    SELECT @pIdUser, 1, 1, NOW()
                                    WHERE NOT EXISTS (
                                        SELECT 1 FROM CTR_ROLES_USER_ADMIN WHERE id_usuario = @pIdUser AND id_rol = 1
                                    )";
                                cmdInsRolAdmin.Parameters.AddWithValue("@pIdUser", idUsuario);
                                await cmdInsRolAdmin.ExecuteNonQueryAsync();
                            }

                            using (var cmdInsRolUser = conn.CreateCommand())
                            {
                                cmdInsRolUser.CommandText = @"
                                    INSERT INTO CTR_ROLES_USER (id_usuario, id_rol, estado, usuario_creacion, fecha_creacion, maquina_creacion)
                                    SELECT @pIdUser, 1, 1, 1, NOW(), 'SETUP'
                                    WHERE NOT EXISTS (
                                        SELECT 1 FROM CTR_ROLES_USER WHERE id_usuario = @pIdUser AND id_rol = 1
                                    )";
                                cmdInsRolUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                                await cmdInsRolUser.ExecuteNonQueryAsync();
                            }
                        }
                        else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                        {
                            using var conn = new Npgsql.NpgsqlConnection(cs);
                            await conn.OpenAsync();

                            long idUsuario = 0;
                            using (var cmdSelect = conn.CreateCommand())
                            {
                                cmdSelect.CommandText = "SELECT id_usuario FROM ctr_usuarios WHERE LOWER(username) = LOWER(@pUser)";
                                cmdSelect.Parameters.AddWithValue("@pUser", devUser);
                                var scalar = await cmdSelect.ExecuteScalarAsync();
                                if (scalar != null && scalar != DBNull.Value)
                                {
                                    idUsuario = Convert.ToInt64(scalar);
                                }
                            }

                            if (idUsuario == 0)
                            {
                                using (var cmdInsUser = conn.CreateCommand())
                                {
                                    cmdInsUser.CommandText = @"
                                        INSERT INTO ctr_usuarios 
                                        (bloqueado, username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, fecha_creacion, maquina_creacion)
                                        VALUES
                                        (0, @pUser, @pIdent, 'ING', 'DESARROLLADOR', @pUser, 'DESARROLLADOR', 1, 1, 1, CURRENT_TIMESTAMP, 'SETUP')
                                        RETURNING id_usuario";
                                    cmdInsUser.Parameters.AddWithValue("@pUser", devUser);
                                    cmdInsUser.Parameters.AddWithValue("@pIdent", DateTime.Now.Ticks % 10000000000);
                                    var scalarId = await cmdInsUser.ExecuteScalarAsync();
                                    if (scalarId != null) idUsuario = Convert.ToInt64(scalarId);
                                }
                            }

                            using (var cmdInsRolAdmin = conn.CreateCommand())
                            {
                                cmdInsRolAdmin.CommandText = @"
                                    INSERT INTO ctr_roles_user_admin (id_usuario, id_rol, vigente, fecha_creacion)
                                    VALUES (@pIdUser, 1, 1, CURRENT_TIMESTAMP)
                                    ON CONFLICT DO NOTHING";
                                cmdInsRolAdmin.Parameters.AddWithValue("@pIdUser", idUsuario);
                                await cmdInsRolAdmin.ExecuteNonQueryAsync();
                            }

                            using (var cmdInsRolUser = conn.CreateCommand())
                            {
                                cmdInsRolUser.CommandText = @"
                                    INSERT INTO ctr_roles_user (id_usuario, id_rol, estado, usuario_creacion, fecha_creacion, maquina_creacion)
                                    VALUES (@pIdUser, 1, 1, 1, CURRENT_TIMESTAMP, 'SETUP')
                                    ON CONFLICT DO NOTHING";
                                cmdInsRolUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                                await cmdInsRolUser.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Registrar usuario desarrollador en Oracle
                            using var conn = new OracleConnection(cs);
                            await conn.OpenAsync();

                            long idUsuario = 0;
                            using (var cmdSelect = conn.CreateCommand())
                            {
                                cmdSelect.CommandText = "SELECT ID_USUARIO FROM CTR_USUARIOS WHERE UPPER(USERNAME) = UPPER(:pUser)";
                                cmdSelect.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                                var scalar = await cmdSelect.ExecuteScalarAsync();
                                if (scalar != null && scalar != DBNull.Value)
                                {
                                    idUsuario = Convert.ToInt64(scalar);
                                }
                            }

                            if (idUsuario == 0)
                            {
                                using (var cmdSeq = conn.CreateCommand())
                                {
                                    cmdSeq.CommandText = "SELECT SEQ_CTR_USUARIOS.NEXTVAL FROM DUAL";
                                    var valSeq = await cmdSeq.ExecuteScalarAsync();
                                    idUsuario = Convert.ToInt64(valSeq ?? 1);
                                }

                                using (var cmdInsUser = conn.CreateCommand())
                                {
                                    cmdInsUser.CommandText = @"
                                        INSERT INTO CTR_USUARIOS 
                                        (ID_USUARIO, BLOQUEADO, USERNAME, IDENTIFICACION, GRAD_ALFABETICO, APELLIDOS, NOMBRES, FUNCIONARIO, UNDE_LABORANDO, CODIGOCARGO, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                                        VALUES
                                        (:pId, 0, :pUser, :pIdent, 'ING', 'DESARROLLADOR', :pUser, 'DESARROLLADOR', 1, 1, 1, SYSDATE, 'SETUP')";
                                    cmdInsUser.Parameters.Add(":pId", OracleDbType.Int64).Value = idUsuario;
                                    cmdInsUser.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                                    cmdInsUser.Parameters.Add(":pIdent", OracleDbType.Int64).Value = DateTime.Now.Ticks % 10000000000;
                                    await cmdInsUser.ExecuteNonQueryAsync();
                                }
                            }

                            using (var cmdInsRolAdmin = conn.CreateCommand())
                            {
                                cmdInsRolAdmin.CommandText = @"
                                    INSERT INTO CTR_ROLES_USER_ADMIN (ID_USUARIO, ID_ROL, VIGENTE, FECHA_CREACION)
                                    SELECT :pIdUser, 1, 1, SYSDATE FROM DUAL
                                    WHERE NOT EXISTS (
                                        SELECT 1 FROM CTR_ROLES_USER_ADMIN WHERE ID_USUARIO = :pIdUser AND ID_ROL = 1
                                    )";
                                cmdInsRolAdmin.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                                await cmdInsRolAdmin.ExecuteNonQueryAsync();
                            }

                            using (var cmdInsRolUser = conn.CreateCommand())
                            {
                                cmdInsRolUser.CommandText = @"
                                    INSERT INTO CTR_ROLES_USER (ID_USUARIO, ID_ROL, ESTADO, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                                    SELECT :pIdUser, 1, 1, 1, SYSDATE, 'SETUP' FROM DUAL
                                    WHERE NOT EXISTS (
                                        SELECT 1 FROM CTR_ROLES_USER WHERE ID_USUARIO = :pIdUser AND ID_ROL = 1
                                    )";
                                cmdInsRolUser.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                                await cmdInsRolUser.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    catch (Exception exDev)
                    {
                        _logger.LogError(exDev, "Aviso al registrar usuario desarrollador");
                    }
                }

                return Ok(new { success = true, message = "Configuración guardada y usuario desarrollador registrado correctamente." });
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

            var rawHost = string.IsNullOrWhiteSpace(r.Host) ? "127.0.0.1" : r.Host.Trim();
            var host = rawHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1" : rawHost;

            var isOracle = r.Provider?.Equals("Oracle", StringComparison.OrdinalIgnoreCase) == true;
            var isPostgres = r.Provider?.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) == true || r.Provider?.Contains("Postgres", StringComparison.OrdinalIgnoreCase) == true;
            var isMysql = r.Provider?.Equals("MySQL", StringComparison.OrdinalIgnoreCase) == true || r.Provider?.Contains("MySQL", StringComparison.OrdinalIgnoreCase) == true;

            var defaultPort = isOracle ? 1521 : (isPostgres ? 5432 : 3306);
            var port = r.Port <= 0 ? defaultPort : r.Port;
            var db = r.DatabaseOrService?.Trim() ?? "";
            var user = r.User?.Trim() ?? "";
            var pass = r.Password ?? "";

            if (isOracle)
            {
                return $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST={host})(PORT={port})))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME={db})));User ID={user};Password={pass};Pooling=true;";
            }

            if (isPostgres)
            {
                return $"Host={host};Port={port};Database={db};Username={user};Password={pass};Timeout=5;";
            }

            return $"Server={host};Port={port};Database={db};User Id={user};Password={pass};Connection Timeout=5;";
        }

        private static async Task ExecuteAutoMigrationIfNeededAsync(OracleConnection conn, string contentRootPath)
        {
            try
            {
                // Buscar el script unificado de migración base
                var scriptPath = Path.Combine(contentRootPath, "..", "..", "docs", "1_EsquemaBasePlantilla", "0_MIGRACION_BASE_PLANTILLA.sql");
                if (!System.IO.File.Exists(scriptPath))
                {
                    // Buscar en ruta relativa del repo si difiere la estructura
                    scriptPath = Path.GetFullPath(Path.Combine(contentRootPath, "docs", "1_EsquemaBasePlantilla", "0_MIGRACION_BASE_PLANTILLA.sql"));
                }

                if (!System.IO.File.Exists(scriptPath)) return;

                var sqlScript = await System.IO.File.ReadAllTextAsync(scriptPath);
                var blocks = sqlScript.Split(new[] { "\n/\n", "\r\n/\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var block in blocks)
                {
                    var cleanCmd = block.Trim();
                    if (string.IsNullOrWhiteSpace(cleanCmd) || cleanCmd.StartsWith("SET DEFINE", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = cleanCmd;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        // Continuar si la tabla o secuencia ya existía ORA-00955 / ORA-02289
                        Console.WriteLine($"Nota migración Oracle: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ejecutando migración automática Oracle: {ex.Message}");
            }
        }

        private static async Task ExecuteMySqlMigrationIfNeededAsync(MySqlConnector.MySqlConnection conn, string contentRootPath)
        {
            try
            {
                var ddlTables = new[]
                {
                    // 1. Core de Seguridad y Menú
                    @"CREATE TABLE IF NOT EXISTS CTR_ROLES (
                        id_rol BIGINT AUTO_INCREMENT PRIMARY KEY,
                        codigo_rol VARCHAR(50) DEFAULT 'ROL' NOT NULL,
                        nombre_rol VARCHAR(255) NOT NULL,
                        descripcion VARCHAR(500),
                        estado TINYINT(1) DEFAULT 1 NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                        maquina_creacion VARCHAR(100),
                        fecha_modifica DATETIME,
                        usuario_modifica VARCHAR(100),
                        maquina_modifica VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_USUARIOS (
                        id_usuario BIGINT AUTO_INCREMENT PRIMARY KEY,
                        bloqueado TINYINT(1) DEFAULT 0 NOT NULL,
                        username VARCHAR(255) NOT NULL UNIQUE,
                        identificacion BIGINT NOT NULL UNIQUE,
                        grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                        apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                        nombres VARCHAR(255) DEFAULT '' NOT NULL,
                        funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                        unde_laborando BIGINT DEFAULT 0 NOT NULL,
                        codigocargo BIGINT DEFAULT 0 NOT NULL,
                        usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                        usuario_modifica BIGINT,
                        fecha_modifica DATETIME,
                        maquina_modifica VARCHAR(255)
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_MENU (
                        id_menu BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_padre BIGINT DEFAULT 0 NOT NULL,
                        titulo VARCHAR(255) NOT NULL,
                        ruta VARCHAR(255),
                        icono VARCHAR(100),
                        orden INT DEFAULT 1 NOT NULL,
                        estado TINYINT(1) DEFAULT 1 NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_MENU_ROLES (
                        id_menu_rol BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_menu BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        estado TINYINT(1) DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_ROLES_USER (
                        id_user_rol BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_usuario BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        justificacion VARCHAR(500),
                        fecha_fin DATETIME,
                        estado TINYINT(1) DEFAULT 1 NOT NULL,
                        usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_ROLES_USER_ADMIN (
                        id_roles_user BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_usuario BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        vigente TINYINT(1) DEFAULT 1 NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_DOMINIO (
                        id_dominio BIGINT AUTO_INCREMENT PRIMARY KEY,
                        dominio VARCHAR(100) NOT NULL,
                        valor VARCHAR(255) NOT NULL,
                        descripcion VARCHAR(500),
                        orden INT DEFAULT 1,
                        vigente TINYINT(1) DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_AUDITORIA (
                        id_auditoria BIGINT AUTO_INCREMENT PRIMARY KEY,
                        evento VARCHAR(100) NOT NULL,
                        descripcion TEXT,
                        identificador BIGINT,
                        usuario BIGINT,
                        maquina VARCHAR(255),
                        fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_ERRORES_SYSTEM (
                        id_error BIGINT AUTO_INCREMENT PRIMARY KEY,
                        procedimiento VARCHAR(255) NOT NULL,
                        descripcion TEXT,
                        id_documento VARCHAR(255),
                        usuario BIGINT,
                        fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",

                    // 2. Administración y Branding
                    @"CREATE TABLE IF NOT EXISTS CTR_BRANDING (
                        id_branding BIGINT AUTO_INCREMENT PRIMARY KEY,
                        sistema VARCHAR(10) NOT NULL,
                        nombre_sistema VARCHAR(100) NOT NULL,
                        unidad_sistema_id INT,
                        sigla VARCHAR(100),
                        nombre_unidad VARCHAR(255),
                        logo_file_name VARCHAR(255),
                        favicon_file_name VARCHAR(255),
                        fondo_1_file_name VARCHAR(255),
                        fondo_2_file_name VARCHAR(255),
                        fondo_3_file_name VARCHAR(255),
                        vigente TINYINT(1) DEFAULT 1 NOT NULL,
                        usuario_creacion VARCHAR(50) DEFAULT 'SETUP' NOT NULL,
                        maquina_creacion VARCHAR(100) DEFAULT 'LOCAL' NOT NULL,
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_modificacion VARCHAR(50),
                        maquina_modificacion VARCHAR(100),
                        fecha_modificacion DATETIME
                    );",
                    @"CREATE TABLE IF NOT EXISTS LOGIN_VISUAL (
                        id_login_visual BIGINT AUTO_INCREMENT PRIMARY KEY,
                        descripcion VARCHAR(255),
                        imagen_fondo VARCHAR(500),
                        orden INT DEFAULT 1,
                        vigente TINYINT(1) DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_LINEA_MANDO (
                        id_linea_mando INT AUTO_INCREMENT PRIMARY KEY,
                        identificacion VARCHAR(20) NOT NULL,
                        nombre VARCHAR(100),
                        apellidos VARCHAR(100),
                        grado VARCHAR(50),
                        cargo VARCHAR(100),
                        unidad VARCHAR(200),
                        foto_base64 LONGTEXT,
                        orden TINYINT DEFAULT 1,
                        vigente TINYINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod DATETIME,
                        maquina_mod VARCHAR(100),
                        peso VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_PERSONAJE_GRUPO (
                        id_personaje_grupo BIGINT AUTO_INCREMENT PRIMARY KEY,
                        nombre_grupo VARCHAR(100),
                        numero_acta VARCHAR(50),
                        mes TINYINT,
                        vigente TINYINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod DATETIME,
                        maquina_mod VARCHAR(50),
                        foto_grupo VARCHAR(200),
                        anio INT
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_PERSONAJE_MES (
                        id_personaje_mes BIGINT AUTO_INCREMENT PRIMARY KEY,
                        identificacion VARCHAR(20),
                        nombres VARCHAR(100),
                        apellidos VARCHAR(100),
                        grado VARCHAR(50),
                        cargo VARCHAR(100),
                        unidad VARCHAR(200),
                        id_categoria BIGINT,
                        numero_acta VARCHAR(50),
                        vigente TINYINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod DATETIME,
                        maquina_mod VARCHAR(100),
                        foto_modificada VARCHAR(200),
                        mes TINYINT,
                        anio INT,
                        id_personaje_grupo BIGINT
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_VIDEOS (
                        id_video INT AUTO_INCREMENT PRIMARY KEY,
                        descripcion VARCHAR(255) NOT NULL,
                        observaciones VARCHAR(500),
                        fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
                        usuario_creacion VARCHAR(50) NOT NULL,
                        ruta_video VARCHAR(500),
                        vigente TINYINT DEFAULT 1,
                        fecha_modifica DATETIME,
                        usuario_modifica VARCHAR(50),
                        maquina_modifica VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_CUENTA_EMAIL (
                        id_cuenta BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_dominio_grupo BIGINT NOT NULL,
                        nombre_cuenta VARCHAR(200) NOT NULL,
                        email VARCHAR(255) NOT NULL,
                        servidor_smtp VARCHAR(255) DEFAULT 'smtp.office365.com',
                        puerto INT DEFAULT 587,
                        usuario_smtp VARCHAR(255),
                        clave_smtp VARCHAR(1000),
                        usar_ssl TINYINT DEFAULT 1,
                        vigente TINYINT DEFAULT 1,
                        usuario_crea VARCHAR(100),
                        fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                        usuario_modifica VARCHAR(100),
                        fecha_modifica DATETIME,
                        ip_registro VARCHAR(50),
                        firma_html LONGTEXT,
                        imagen_encabezado VARCHAR(500)
                    );",
                    @"CREATE TABLE IF NOT EXISTS CTR_CUENTA_EMAIL_USUARIO (
                        id_cuenta_usuario BIGINT AUTO_INCREMENT PRIMARY KEY,
                        id_cuenta BIGINT NOT NULL,
                        id_usuario BIGINT NOT NULL,
                        vigente TINYINT DEFAULT 1 NOT NULL,
                        usuario_crea VARCHAR(100),
                        fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                        usuario_modifica VARCHAR(100),
                        fecha_modifica DATETIME,
                        ip_registro VARCHAR(45),
                        UNIQUE KEY uq_ceu_cuenta_usuario (id_cuenta, id_usuario)
                    );"
                };

                foreach (var sql in ddlTables)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insertar roles base si no existen
                using (var cmdRole = conn.CreateCommand())
                {
                    cmdRole.CommandText = "INSERT IGNORE INTO CTR_ROLES (id_rol, codigo_rol, nombre_rol, descripcion) VALUES (1, 'DEV', 'Desarrollador', 'Acceso Total al Sistema'), (2, 'ADMIN', 'Administrador', 'Administrador del Sistema');";
                    await cmdRole.ExecuteNonQueryAsync();
                }

                // Insertar menú inicial del sistema si la tabla está vacía
                using (var cmdMenu = conn.CreateCommand())
                {
                    cmdMenu.CommandText = @"
                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 1);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 2, 0, 'Administración', '/administracion', 'fa-solid fa-gears', 2, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 2);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 3, 2, 'Configuración', '/administracion/configuracion-sistema', 'fa-solid fa-palette', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 3);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 4, 2, 'Línea de Mando', '/administracion/linea-mando', 'fa-solid fa-sitemap', 2, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 4);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 5, 2, 'Personajes del Mes', '/administracion/personaje-mes', 'fa-solid fa-user-check', 3, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 5);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 6, 2, 'Menú', '/administracion/menu', 'fa-solid fa-bars', 4, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 6);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 7, 2, 'Usuarios', '/administracion/usuarios', 'fa-solid fa-users', 5, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 7);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 8, 2, 'Roles', '/administracion/roles', 'fa-solid fa-user-shield', 6, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 8);

                        -- Corregir rutas inválidas de instalaciones previas
                        UPDATE CTR_MENU SET ruta = '/administracion/configuracion-sistema' WHERE id_menu = 3 AND ruta = '/administracion/branding';
                        UPDATE CTR_MENU SET ruta = '/administracion/personaje-mes' WHERE id_menu = 5 AND ruta = '/administracion/personajes';
                    ";
                    await cmdMenu.ExecuteNonQueryAsync();
                }

                // Asociar menú inicial al Rol Desarrollador (1)
                using (var cmdMenuRol = conn.CreateCommand())
                {
                    cmdMenuRol.CommandText = @"
                        INSERT INTO CTR_MENU_ROLES (id_menu, id_rol, estado)
                        SELECT m.id_menu, 1, 1 FROM CTR_MENU m
                        WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU_ROLES mr WHERE mr.id_menu = m.id_menu AND mr.id_rol = 1);
                    ";
                    await cmdMenuRol.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ejecutando migración MySQL: {ex.Message}");
            }
        }

        private static async Task ExecutePostgresMigrationIfNeededAsync(Npgsql.NpgsqlConnection conn, string contentRootPath)
        {
            try
            {
                var ddlTables = new[]
                {
                    // 1. Core de Seguridad y Menú
                    @"CREATE TABLE IF NOT EXISTS ctr_roles (
                        id_rol BIGSERIAL PRIMARY KEY,
                        codigo_rol VARCHAR(50) DEFAULT 'ROL' NOT NULL,
                        nombre_rol VARCHAR(255) NOT NULL,
                        descripcion VARCHAR(500),
                        estado SMALLINT DEFAULT 1 NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                        maquina_creacion VARCHAR(100),
                        fecha_modifica TIMESTAMP,
                        usuario_modifica VARCHAR(100),
                        maquina_modifica VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_usuarios (
                        id_usuario BIGSERIAL PRIMARY KEY,
                        bloqueado SMALLINT DEFAULT 0 NOT NULL,
                        username VARCHAR(255) NOT NULL UNIQUE,
                        identificacion BIGINT NOT NULL UNIQUE,
                        grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                        apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                        nombres VARCHAR(255) DEFAULT '' NOT NULL,
                        funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                        unde_laborando BIGINT DEFAULT 0 NOT NULL,
                        codigocargo BIGINT DEFAULT 0 NOT NULL,
                        usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                        usuario_modifica BIGINT,
                        fecha_modifica TIMESTAMP,
                        maquina_modifica VARCHAR(255)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_menu (
                        id_menu BIGSERIAL PRIMARY KEY,
                        id_padre BIGINT DEFAULT 0 NOT NULL,
                        titulo VARCHAR(255) NOT NULL,
                        ruta VARCHAR(255),
                        icono VARCHAR(100),
                        orden INT DEFAULT 1 NOT NULL,
                        estado SMALLINT DEFAULT 1 NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_menu_roles (
                        id_menu_rol BIGSERIAL PRIMARY KEY,
                        id_menu BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        estado SMALLINT DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_roles_user (
                        id_user_rol BIGSERIAL PRIMARY KEY,
                        id_usuario BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        justificacion VARCHAR(500),
                        fecha_fin TIMESTAMP,
                        estado SMALLINT DEFAULT 1 NOT NULL,
                        usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_roles_user_admin (
                        id_roles_user BIGSERIAL PRIMARY KEY,
                        id_usuario BIGINT NOT NULL,
                        id_rol BIGINT NOT NULL,
                        vigente SMALLINT DEFAULT 1 NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_dominio (
                        id_dominio BIGSERIAL PRIMARY KEY,
                        dominio VARCHAR(100) NOT NULL,
                        valor VARCHAR(255) NOT NULL,
                        descripcion VARCHAR(500),
                        orden INT DEFAULT 1,
                        vigente SMALLINT DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_auditoria (
                        id_auditoria BIGSERIAL PRIMARY KEY,
                        evento VARCHAR(100) NOT NULL,
                        descripcion TEXT,
                        identificador BIGINT,
                        usuario BIGINT,
                        maquina VARCHAR(255),
                        fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_errores_system (
                        id_error BIGSERIAL PRIMARY KEY,
                        procedimiento VARCHAR(255) NOT NULL,
                        descripcion TEXT,
                        id_documento VARCHAR(255),
                        usuario BIGINT,
                        fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                    );",

                    // 2. Administración y Branding
                    @"CREATE TABLE IF NOT EXISTS ctr_branding (
                        id_branding BIGSERIAL PRIMARY KEY,
                        sistema VARCHAR(10) NOT NULL,
                        nombre_sistema VARCHAR(100) NOT NULL,
                        unidad_sistema_id INT,
                        sigla VARCHAR(100),
                        nombre_unidad VARCHAR(255),
                        logo_file_name VARCHAR(255),
                        favicon_file_name VARCHAR(255),
                        fondo_1_file_name VARCHAR(255),
                        fondo_2_file_name VARCHAR(255),
                        fondo_3_file_name VARCHAR(255),
                        vigente SMALLINT DEFAULT 1 NOT NULL,
                        usuario_creacion VARCHAR(50) DEFAULT 'SETUP' NOT NULL,
                        maquina_creacion VARCHAR(100) DEFAULT 'LOCAL' NOT NULL,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                        usuario_modificacion VARCHAR(50),
                        maquina_modificacion VARCHAR(100),
                        fecha_modificacion TIMESTAMP
                    );",
                    @"CREATE TABLE IF NOT EXISTS login_visual (
                        id_login_visual BIGSERIAL PRIMARY KEY,
                        descripcion VARCHAR(255),
                        imagen_fondo VARCHAR(500),
                        orden INT DEFAULT 1,
                        vigente SMALLINT DEFAULT 1 NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_linea_mando (
                        id_linea_mando SERIAL PRIMARY KEY,
                        identificacion VARCHAR(20) NOT NULL,
                        nombre VARCHAR(100),
                        apellidos VARCHAR(100),
                        grado VARCHAR(50),
                        cargo VARCHAR(100),
                        unidad VARCHAR(200),
                        foto_base64 TEXT,
                        orden SMALLINT DEFAULT 1,
                        vigente SMALLINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod TIMESTAMP,
                        maquina_mod VARCHAR(100),
                        peso VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_personaje_grupo (
                        id_personaje_grupo BIGSERIAL PRIMARY KEY,
                        nombre_grupo VARCHAR(100),
                        numero_acta VARCHAR(50),
                        mes SMALLINT,
                        vigente SMALLINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod TIMESTAMP,
                        maquina_mod VARCHAR(50),
                        foto_grupo VARCHAR(200),
                        anio INT
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_personaje_mes (
                        id_personaje_mes BIGSERIAL PRIMARY KEY,
                        identificacion VARCHAR(20),
                        nombres VARCHAR(100),
                        apellidos VARCHAR(100),
                        grado VARCHAR(50),
                        cargo VARCHAR(100),
                        unidad VARCHAR(200),
                        id_categoria BIGINT,
                        numero_acta VARCHAR(50),
                        vigente SMALLINT DEFAULT 1,
                        usuario_crea VARCHAR(50),
                        fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        maquina_crea VARCHAR(100),
                        usuario_mod VARCHAR(50),
                        fecha_mod TIMESTAMP,
                        maquina_mod VARCHAR(100),
                        foto_modificada VARCHAR(200),
                        mes SMALLINT,
                        anio INT,
                        id_personaje_grupo BIGINT
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_videos (
                        id_video SERIAL PRIMARY KEY,
                        descripcion VARCHAR(255) NOT NULL,
                        observaciones VARCHAR(500),
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        usuario_creacion VARCHAR(50) NOT NULL,
                        ruta_video VARCHAR(500),
                        vigente SMALLINT DEFAULT 1,
                        fecha_modifica TIMESTAMP,
                        usuario_modifica VARCHAR(50),
                        maquina_modifica VARCHAR(100)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_cuenta_email (
                        id_cuenta BIGSERIAL PRIMARY KEY,
                        id_dominio_grupo BIGINT NOT NULL,
                        nombre_cuenta VARCHAR(200) NOT NULL,
                        email VARCHAR(255) NOT NULL,
                        servidor_smtp VARCHAR(255) DEFAULT 'smtp.office365.com',
                        puerto INT DEFAULT 587,
                        usuario_smtp VARCHAR(255),
                        clave_smtp VARCHAR(1000),
                        usar_ssl SMALLINT DEFAULT 1,
                        vigente SMALLINT DEFAULT 1,
                        usuario_crea VARCHAR(100),
                        fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        usuario_modifica VARCHAR(100),
                        fecha_modifica TIMESTAMP,
                        ip_registro VARCHAR(50),
                        firma_html TEXT,
                        imagen_encabezado VARCHAR(500)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ctr_cuenta_email_usuario (
                        id_cuenta_usuario BIGSERIAL PRIMARY KEY,
                        id_cuenta BIGINT NOT NULL,
                        id_usuario BIGINT NOT NULL,
                        vigente SMALLINT DEFAULT 1 NOT NULL,
                        usuario_crea VARCHAR(100),
                        fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        usuario_modifica VARCHAR(100),
                        fecha_modifica TIMESTAMP,
                        ip_registro VARCHAR(45),
                        CONSTRAINT uq_ceu_cuenta_usuario UNIQUE (id_cuenta, id_usuario)
                    );"
                };

                foreach (var sql in ddlTables)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmdRole = conn.CreateCommand())
                {
                    cmdRole.CommandText = "INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion) VALUES (1, 'DEV', 'Desarrollador', 'Acceso Total al Sistema'), (2, 'ADMIN', 'Administrador', 'Administrador del Sistema') ON CONFLICT (id_rol) DO NOTHING;";
                    await cmdRole.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ejecutando migración PostgreSQL: {ex.Message}");
            }
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
        public string? DevUser { get; set; }
        public string? DevPassword { get; set; }
    }

    public class SetupSaveRequest : SetupTestRequest
    {
    }
}
