using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Comun.Dtos;
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
        private readonly Microsoft.Extensions.Hosting.IHostApplicationLifetime _appLifetime;
        private readonly Microsoft.Extensions.Logging.ILogger<SetupController> _logger;

        public SetupController(
            IConfiguration configuration,
            IWebHostEnvironment env,
            IDbUbicacionService ubicacionService,
            IDbBrandingService brandingService,
            Servicios.ApiInterfaz.IApiWebOud oudService,
            Servicios.ApiInterfaz.IApiWebToken tokenService,
            Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime,
            Microsoft.Extensions.Logging.ILogger<SetupController> logger)
        {
            _configuration = configuration;
            _env = env;
            _ubicacionService = ubicacionService;
            _brandingService = brandingService;
            _oudService = oudService;
            _tokenService = tokenService;
            _appLifetime = appLifetime;
            _logger = logger;
        }

        [HttpPost("test-pip-credentials")]
        public async Task<IActionResult> TestPipCredentials([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PipUser) || string.IsNullOrWhiteSpace(request.PipPassword))
            {
                return Ok(new { success = false, message = "Debe ingresar el Usuario y la Clave PIP de la aplicación." });
            }

            try
            {
                var dtoPip = new Comun.Dtos.DtoUsuarioPip
                {
                    usuario = request.PipUser.Trim(),
                    clave = request.PipPassword.Trim()
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

                _logger.LogWarning("Validación PIP fallida para usuario: {User}. Mensaje: {Msg}", request.PipUser, respuesta?.Mensaje);
                return Ok(new
                {
                    success = false,
                    message = $"Error de validación PIP: {respuesta?.Mensaje ?? "Credenciales inválidas o servicio PIP no disponible."}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error excepcionando validación PIP para usuario: {User}", request.PipUser);
                return Ok(new
                {
                    success = false,
                    message = $"No fue posible validar las credenciales PIP en el servicio: {ex.Message}"
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(CancellationToken ct)
        {
            var cs = _configuration.GetConnectionString("DbConnection");
            var isConfigured = !string.IsNullOrWhiteSpace(cs);
            var provider = _configuration["DatabaseProvider"] ?? "Oracle";
            var pipUser = _configuration["ApiSettings:UsuarioPip"] ?? "";
            var nombreSistema = _configuration["ApiSettings:NombreSistema"] ?? "";
            var siglaSistema = _configuration["ApiSettings:SiglaSistema"] ?? "";

            string devUser = "";
            string devIdentificacion = "";
            string devNombres = "";
            string devApellidos = "";
            string devGrad = "";
            string dbOrService = "";

            if (isConfigured)
            {
                try
                {
                    var decryptedCs = Comun.Security.CryptoUtility.Decrypt(cs, _configuration);
                    if (string.IsNullOrWhiteSpace(decryptedCs)) decryptedCs = cs;

                    if (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
                    {
                        if (decryptedCs.Contains("SERVICE_NAME=", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = decryptedCs.Split(new[] { "SERVICE_NAME=" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                dbOrService = parts[1].Split(')', ';')[0].Trim();
                            }
                        }

                        using var conn = new OracleConnection(decryptedCs);
                        await conn.OpenAsync(ct);

                        using var cmdUser = conn.CreateCommand();
                        cmdUser.CommandText = "SELECT USERNAME, IDENTIFICACION, NOMBRES, APELLIDOS, GRAD_ALFABETICO FROM CTR_USUARIOS WHERE ROWNUM = 1 ORDER BY ID_USUARIO ASC";
                        using var reader = await cmdUser.ExecuteReaderAsync(ct);
                        if (await reader.ReadAsync(ct))
                        {
                            devUser = reader["USERNAME"]?.ToString() ?? "";
                            devIdentificacion = reader["IDENTIFICACION"]?.ToString() ?? "";
                            devNombres = reader["NOMBRES"]?.ToString() ?? "";
                            devApellidos = reader["APELLIDOS"]?.ToString() ?? "";
                            devGrad = reader["GRAD_ALFABETICO"]?.ToString() ?? "";
                        }
                    }
                    else if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new MySqlConnector.MySqlConnection(decryptedCs);
                        await conn.OpenAsync(ct);
                        dbOrService = conn.Database;

                        using var cmdUser = conn.CreateCommand();
                        cmdUser.CommandText = "SELECT username, identificacion, nombres, apellidos, grad_alfabetico FROM CTR_USUARIOS ORDER BY id_usuario ASC LIMIT 1";
                        using var reader = await cmdUser.ExecuteReaderAsync(ct);
                        if (await reader.ReadAsync(ct))
                        {
                            devUser = reader["username"]?.ToString() ?? "";
                            devIdentificacion = reader["identificacion"]?.ToString() ?? "";
                            devNombres = reader["nombres"]?.ToString() ?? "";
                            devApellidos = reader["apellidos"]?.ToString() ?? "";
                            devGrad = reader["grad_alfabetico"]?.ToString() ?? "";
                        }
                    }
                    else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new Npgsql.NpgsqlConnection(decryptedCs);
                        await conn.OpenAsync(ct);
                        dbOrService = conn.Database;

                        using var cmdUser = conn.CreateCommand();
                        cmdUser.CommandText = "SELECT username, identificacion, nombres, apellidos, grad_alfabetico FROM ctr_usuarios ORDER BY id_usuario ASC LIMIT 1";
                        using var reader = await cmdUser.ExecuteReaderAsync(ct);
                        if (await reader.ReadAsync(ct))
                        {
                            devUser = reader["username"]?.ToString() ?? "";
                            devIdentificacion = reader["identificacion"]?.ToString() ?? "";
                            devNombres = reader["nombres"]?.ToString() ?? "";
                            devApellidos = reader["apellidos"]?.ToString() ?? "";
                            devGrad = reader["grad_alfabetico"]?.ToString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetStatus: No se pudo obtener el detalle de instalación desde la BD.");
                }
            }

            return Ok(new
            {
                configured = isConfigured,
                provider = provider,
                connectionStringConfigured = isConfigured,
                pipUser = pipUser,
                nombreSistema = nombreSistema,
                siglaSistema = siglaSistema,
                databaseOrService = dbOrService,
                devUser = devUser,
                devIdentificacion = devIdentificacion,
                devNombres = devNombres,
                devApellidos = devApellidos,
                devGrad = devGrad
            });
        }

        [HttpGet("unidades")]
        public async Task<IActionResult> GetUnidades(CancellationToken ct)
        {
            try
            {
                var unidadesPip = await _tokenService.GetUnidadesPipAsync(ct);
                if (unidadesPip != null && unidadesPip.Count > 0)
                {
                    return Ok(unidadesPip);
                }

                _logger.LogInformation("Consulta PIP de unidades no retornó registros; buscando en la base de datos local.");
                var unidadesDb = await _ubicacionService.GetUnidadesOrganigramaAsync(ct);
                if (unidadesDb != null && unidadesDb.Count > 0)
                {
                    return Ok(unidadesDb);
                }

                // Fallback institucional en caso de que PIP no esté disponible y la BD sea nueva
                var fallbacks = new System.Collections.Generic.List<object>
                {
                    new { id = 1, consecutivo = 1, nombre = "Oficina de Tecnologías de la Información y las Comunicaciones", sigla = "OFTIC" },
                    new { id = 2, consecutivo = 2, nombre = "Dirección General Policía Nacional", sigla = "DIPON" },
                    new { id = 3, consecutivo = 3, nombre = "Inspección General y Responsabilidad Profesional", sigla = "INSGE" },
                    new { id = 4, consecutivo = 4, nombre = "Dirección de Seguridad Ciudadana", sigla = "DISEC" },
                    new { id = 5, consecutivo = 5, nombre = "Dirección de Investigación Criminal e INTERPOL", sigla = "DIJIN" },
                    new { id = 6, consecutivo = 6, nombre = "Dirección de Inteligencia Policial", sigla = "DIPOL" },
                    new { id = 7, consecutivo = 7, nombre = "Dirección Antinarcóticos", sigla = "DIRAN" },
                    new { id = 8, consecutivo = 8, nombre = "Dirección de Protección y Servicios Especiales", sigla = "DIPRO" },
                    new { id = 9, consecutivo = 9, nombre = "Dirección de Educación Policial", sigla = "DINAE" },
                    new { id = 10, consecutivo = 10, nombre = "Dirección de Incorporación", sigla = "DINCO" },
                    new { id = 11, consecutivo = 11, nombre = "Comando de Estudios Superiores", sigla = "COEST" }
                };

                return Ok(fallbacks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando unidades PIP/DB");
                var fallbacks = new System.Collections.Generic.List<object>
                {
                    new { id = 1, consecutivo = 1, nombre = "Oficina de Tecnologías de la Información y las Comunicaciones", sigla = "OFTIC" },
                    new { id = 2, consecutivo = 2, nombre = "Dirección General Policía Nacional", sigla = "DIPON" }
                };
                return Ok(fallbacks);
            }
        }

        [HttpPost("validate-dev-user")]
        public async Task<IActionResult> ValidateDevUser([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            var identificacion = (request?.DevIdentificacion ?? request?.DevUser ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return Ok(new { success = false, message = "Debe ingresar el número de cédula/identificación del Desarrollador." });
            }

            try
            {
                string tokenPip = "";
                if (!string.IsNullOrWhiteSpace(request?.PipUser) && !string.IsNullOrWhiteSpace(request?.PipPassword))
                {
                    var tokenRespServ = await _tokenService.ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                    {
                        usuario = request.PipUser.Trim(),
                        clave = request.PipPassword.Trim()
                    });
                    tokenPip = tokenRespServ?.Respuesta ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(tokenPip))
                {
                    var tokenResp = await _tokenService.ObtenerTokenPipAsync(ct);
                    tokenPip = tokenResp?.Respuesta ?? string.Empty;
                }

                DtoFuncionario? funcionario = null;

                if (!string.IsNullOrWhiteSpace(tokenPip))
                {
                    funcionario = await _tokenService.GetFuncionarioAsync(tokenPip, identificacion);
                }

                if (funcionario != null && (!string.IsNullOrWhiteSpace(funcionario.identificacion) || !string.IsNullOrWhiteSpace(funcionario.nombres)))
                {
                    string siglaFisica = (funcionario.siglaFisica ?? funcionario.siglaDependencia ?? funcionario.siglaLaborando ?? "").Trim();
                    bool isOftic = siglaFisica.Contains("OFTIC", StringComparison.OrdinalIgnoreCase) ||
                                   siglaFisica.Equals("OFTIC", StringComparison.OrdinalIgnoreCase);

                    if (!isOftic)
                    {
                        return Ok(new
                        {
                            success = false,
                            isGusof = false,
                            message = $"El funcionario ({(!string.IsNullOrWhiteSpace(funcionario.nombreGrado) ? funcionario.nombreGrado : funcionario.gradAlfabetico)} {funcionario.nombres} {funcionario.apellidos}) pertenece a la sigla física '{(!string.IsNullOrWhiteSpace(siglaFisica) ? siglaFisica : funcionario.dependencia ?? "DESCONOCIDA")}', la cual no corresponde a OFTIC. Por favor contáctese con el administrador del sistema.",
                            funcionario
                        });
                    }

                    return Ok(new
                    {
                        success = true,
                        isGusof = true,
                        message = $"Funcionario '{(!string.IsNullOrWhiteSpace(funcionario.nombreGrado) ? funcionario.nombreGrado : funcionario.gradAlfabetico)} {funcionario.nombres} {funcionario.apellidos}' verificado exitosamente como integrante de OFTIC.",
                        funcionario
                    });
                }

                return Ok(new
                {
                    success = false,
                    isGusof = false,
                    message = $"No se encontró ningún funcionario registrado en PIP con la cédula/identificación '{identificacion}'. Por favor verifique el número e intente de nuevo."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando identificación de desarrollador: {Identificacion}", identificacion);
                return Ok(new
                {
                    success = false,
                    isGusof = false,
                    message = $"No fue posible consultar PIP para la identificación '{identificacion}'. Verifique las credenciales PIP/OUD y la conectividad a la red institucional."
                });
            }
        }

        [HttpPost("confirm-dev-user")]
        public async Task<IActionResult> ConfirmDevUser([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest(new { success = false, message = "Datos requeridos." });

            try
            {
                string cs = BuildConnectionString(request);
                var provider = (request.Provider ?? "Oracle").Trim();

                DtoFuncionario? pipFunc = null;
                string searchIdent = !string.IsNullOrWhiteSpace(request.DevIdentificacion) 
                    ? request.DevIdentificacion.Trim() 
                    : (!string.IsNullOrWhiteSpace(request.DevUser) ? request.DevUser.Trim() : "");

                if (!string.IsNullOrWhiteSpace(searchIdent))
                {
                    try
                    {
                        string tokenPip = "";
                        if (!string.IsNullOrWhiteSpace(request.PipUser) && !string.IsNullOrWhiteSpace(request.PipPassword))
                        {
                            var tokenRespServ = await _tokenService.ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                            {
                                usuario = request.PipUser.Trim(),
                                clave = request.PipPassword.Trim()
                            });
                            tokenPip = tokenRespServ?.Respuesta ?? string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(tokenPip))
                        {
                            var tokenResp = await _tokenService.ObtenerTokenPipAsync(ct);
                            tokenPip = tokenResp?.Respuesta ?? string.Empty;
                        }

                        if (!string.IsNullOrWhiteSpace(tokenPip))
                        {
                            pipFunc = await _tokenService.GetFuncionarioAsync(tokenPip, searchIdent);
                        }
                    }
                    catch (Exception exPip)
                    {
                        _logger.LogWarning(exPip, "No se pudo consultar PIP para '{SearchIdent}' al confirmar usuario", searchIdent);
                    }
                }

                string devIdent = !string.IsNullOrWhiteSpace(pipFunc?.identificacion) ? pipFunc.identificacion.Trim() : (!string.IsNullOrWhiteSpace(request.DevIdentificacion) ? request.DevIdentificacion.Trim() : searchIdent);
                string devUser = !string.IsNullOrWhiteSpace(pipFunc?.usuario) ? pipFunc.usuario.Trim() : (!string.IsNullOrWhiteSpace(request.DevUser) ? request.DevUser.Trim() : devIdent);
                string devNombres = !string.IsNullOrWhiteSpace(pipFunc?.nombres) ? pipFunc.nombres.Trim() : (!string.IsNullOrWhiteSpace(request.DevNombres) ? request.DevNombres.Trim() : "");
                string devApellidos = !string.IsNullOrWhiteSpace(pipFunc?.apellidos) ? pipFunc.apellidos.Trim() : (!string.IsNullOrWhiteSpace(request.DevApellidos) ? request.DevApellidos.Trim() : "");
                string devGrad = !string.IsNullOrWhiteSpace(pipFunc?.gradAlfabetico) ? pipFunc.gradAlfabetico.Trim() : (!string.IsNullOrWhiteSpace(pipFunc?.nombreGrado) ? pipFunc.nombreGrado.Trim() : (!string.IsNullOrWhiteSpace(request.DevGrad) ? request.DevGrad.Trim() : ""));
                string devFuncStr = !string.IsNullOrWhiteSpace(pipFunc?.funcionario) ? pipFunc.funcionario.Trim() : $"OFTIC {devGrad} {devNombres} {devApellidos}".Trim();
                long devUndeLaborando = long.TryParse(pipFunc?.undeLaborandoCodigo, out var undeCode) ? undeCode : 67066L;
                long devCodigoCargo = long.TryParse(pipFunc?.codigoCargo, out var cargoCode) ? cargoCode : 35L;
                long devIdentNum = long.TryParse(devIdent, out var devIdVal) && devIdVal > 0 ? devIdVal : 0L;

                await SaveDevUserToDbAsync(cs, provider, devUser, devIdent, devIdentNum, devNombres, devApellidos, devGrad, devFuncStr, devUndeLaborando, devCodigoCargo, ct);

                return Ok(new { success = true, message = $"¡Usuario Desarrollador '{devUser}' (Cédula: {devIdent}) insertado exitosamente en CTR_USUARIOS!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando usuario en confirm-dev-user");
                return Ok(new { success = false, message = $"Aviso al registrar usuario desarrollador en BD: {ex.Message}" });
            }
        }

        private async Task SaveDevUserToDbAsync(string cs, string provider, string devUser, string devIdent, long devIdentNum, string devNombres, string devApellidos, string devGrad, string devFuncStr, long devUndeLaborando, long devCodigoCargo, CancellationToken ct)
        {
            if (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new OracleConnection(cs);
                await conn.OpenAsync(ct);

                long idUsuario = 0;
                using (var cmdSelect = conn.CreateCommand())
                {
                    cmdSelect.CommandText = "SELECT ID_USUARIO FROM CTR_USUARIOS WHERE UPPER(USERNAME) = UPPER(:pUser) OR (IDENTIFICACION > 0 AND IDENTIFICACION = :pIdent)";
                    cmdSelect.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                    cmdSelect.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                    var scalar = await cmdSelect.ExecuteScalarAsync(ct);
                    if (scalar != null && scalar != DBNull.Value) idUsuario = Convert.ToInt64(scalar);
                }

                if (idUsuario == 0)
                {
                    try
                    {
                        using var cmdSeq = conn.CreateCommand();
                        cmdSeq.CommandText = "SELECT SEQ_CTR_USUARIOS.NEXTVAL FROM DUAL";
                        var valSeq = await cmdSeq.ExecuteScalarAsync(ct);
                        idUsuario = Convert.ToInt64(valSeq ?? 1);
                    }
                    catch
                    {
                        idUsuario = 1;
                    }

                    using var cmdInsUser = conn.CreateCommand();
                    cmdInsUser.CommandText = @"
                        INSERT INTO CTR_USUARIOS 
                        (ID_USUARIO, BLOQUEADO, USERNAME, IDENTIFICACION, GRAD_ALFABETICO, APELLIDOS, NOMBRES, FUNCIONARIO, UNDE_LABORANDO, CODIGOCARGO, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                        VALUES
                        (:pId, 0, :pUser, :pIdent, :pGrad, :pApellidos, :pNombres, :pFuncStr, :pUnde, :pCargo, 1, SYSDATE, 'SETUP')";
                    cmdInsUser.Parameters.Add(":pId", OracleDbType.Int64).Value = idUsuario;
                    cmdInsUser.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                    cmdInsUser.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                    cmdInsUser.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = devGrad;
                    cmdInsUser.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = devApellidos;
                    cmdInsUser.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = devNombres;
                    cmdInsUser.Parameters.Add(":pFuncStr", OracleDbType.Varchar2).Value = devFuncStr;
                    cmdInsUser.Parameters.Add(":pUnde", OracleDbType.Int64).Value = devUndeLaborando;
                    cmdInsUser.Parameters.Add(":pCargo", OracleDbType.Int64).Value = devCodigoCargo;
                    await cmdInsUser.ExecuteNonQueryAsync(ct);
                }
                else
                {
                    using var cmdUpdUser = conn.CreateCommand();
                    cmdUpdUser.CommandText = @"
                        UPDATE CTR_USUARIOS 
                        SET USERNAME = :pUser, IDENTIFICACION = :pIdent, GRAD_ALFABETICO = :pGrad, APELLIDOS = :pApellidos, NOMBRES = :pNombres, FUNCIONARIO = :pFuncStr, UNDE_LABORANDO = :pUnde, CODIGOCARGO = :pCargo, BLOQUEADO = 0
                        WHERE ID_USUARIO = :pIdUser";
                    cmdUpdUser.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                    cmdUpdUser.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                    cmdUpdUser.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = devGrad;
                    cmdUpdUser.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = devApellidos;
                    cmdUpdUser.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = devNombres;
                    cmdUpdUser.Parameters.Add(":pFuncStr", OracleDbType.Varchar2).Value = devFuncStr;
                    cmdUpdUser.Parameters.Add(":pUnde", OracleDbType.Int64).Value = devUndeLaborando;
                    cmdUpdUser.Parameters.Add(":pCargo", OracleDbType.Int64).Value = devCodigoCargo;
                    cmdUpdUser.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                    await cmdUpdUser.ExecuteNonQueryAsync(ct);
                }

                using (var cmdInsRolAdmin = conn.CreateCommand())
                {
                    long nextUserRolId = 1;
                    try
                    {
                        using var cmdSeqRol = conn.CreateCommand();
                        cmdSeqRol.CommandText = "SELECT SEQ_CTR_ROLES_USER_ADMIN.NEXTVAL FROM DUAL";
                        var valSeqRol = await cmdSeqRol.ExecuteScalarAsync(ct);
                        nextUserRolId = Convert.ToInt64(valSeqRol ?? 1);
                    }
                    catch { }

                    cmdInsRolAdmin.CommandText = @"
                        MERGE INTO CTR_ROLES_USER_ADMIN r USING (SELECT :pIdUser AS u, 1 AS r FROM DUAL) src ON (r.ID_USUARIO = src.u AND r.ID_ROL = src.r)
                        WHEN NOT MATCHED THEN INSERT (ID_USER_ROL, ID_USUARIO, ID_ROL, VIGENTE, FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION) VALUES (:pNextId, :pIdUser, 1, 1, SYSDATE, 'SETUP', 'LOCAL')";
                    cmdInsRolAdmin.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                    cmdInsRolAdmin.Parameters.Add(":pNextId", OracleDbType.Int64).Value = nextUserRolId;
                    await cmdInsRolAdmin.ExecuteNonQueryAsync(ct);
                }
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
        public async Task<IActionResult> MigrateDatabase([FromBody] SetupTestRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest(new { success = false, message = "Datos requeridos." });

            try
            {
                string cs = BuildConnectionString(request);
                
                var dict = new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["ConnectionStrings:DbConnection"] = cs,
                    ["DatabaseProvider"] = request.Provider ?? "Oracle",
                    ["OracleSettings:TablespaceData"] = request.TablespaceData?.Trim() ?? "",
                    ["OracleSettings:TablespaceIndexes"] = request.TablespaceIndexes?.Trim() ?? ""
                };

                var tempConfig = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

                var migrationRes = await Datos.Gestion.DatabaseMigration.RunAsync(tempConfig, _logger, ct);
                return Ok(new { 
                    success = migrationRes.Success, 
                    message = migrationRes.Message, 
                    errors = migrationRes.Errors 
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error ejecutando migración: {ex.Message}", errors = new[] { ex.Message } });
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
                // Blindar: En entorno de desarrollo se escribe únicamente a appsettings.Development.json (ignorado en .gitignore)
                // para evitar modificar appsettings.json y exponer credenciales locales en el repositorio Git.
                string targetFileName = _env.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";
                var targetPath = Path.Combine(_env.ContentRootPath, targetFileName);

                if (!System.IO.File.Exists(targetPath))
                {
                    var templatePath = Path.Combine(_env.ContentRootPath, "appsettings.template.json");
                    if (System.IO.File.Exists(templatePath))
                    {
                        System.IO.File.Copy(templatePath, targetPath);
                    }
                    else
                    {
                        var basePath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                        if (System.IO.File.Exists(basePath))
                        {
                            System.IO.File.Copy(basePath, targetPath);
                        }
                    }
                }

                var jsonString = await System.IO.File.ReadAllTextAsync(targetPath);
                var jsonNode = JsonNode.Parse(jsonString);

                if (jsonNode == null) return StatusCode(500, new { success = false, message = "Error leyendo json." });

                // Encriptar la cadena de conexión con AES-256 antes de guardar
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

                // Configurar NombreSistema y SiglaSistema
                string nombreSistemaVal = !string.IsNullOrWhiteSpace(request.NombreSistema) 
                    ? request.NombreSistema.Trim() 
                    : "Sistema de Gestión Institucional";
                    
                string siglaSistemaVal = !string.IsNullOrWhiteSpace(request.Sistema) 
                    ? request.Sistema.Trim() 
                    : (!string.IsNullOrWhiteSpace(request.Sigla) ? request.Sigla.Trim() : "GUSOF");

                jsonNode["ApiSettings"]!["NombreSistema"] = nombreSistemaVal;
                jsonNode["ApiSettings"]!["SiglaSistema"] = siglaSistemaVal;

                // Configurar Oracle Tablespaces (Datos e Índices)
                if (jsonNode["OracleSettings"] == null)
                {
                    jsonNode["OracleSettings"] = new JsonObject();
                }
                jsonNode["OracleSettings"]!["TablespaceData"] = request.TablespaceData?.Trim() ?? "";
                jsonNode["OracleSettings"]!["TablespaceIndexes"] = request.TablespaceIndexes?.Trim() ?? "";

                // Set JWT Settings if provided
                if (jsonNode["Jwt"] == null)
                {
                    jsonNode["Jwt"] = new JsonObject();
                }
                if (!string.IsNullOrWhiteSpace(request.JwtKey))
                {
                    jsonNode["Jwt"]!["Key"] = request.JwtKey.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.JwtAudience))
                {
                    jsonNode["Jwt"]!["Audience"] = request.JwtAudience.Trim();
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = jsonNode.ToJsonString(options);

                await System.IO.File.WriteAllTextAsync(targetPath, updatedJson);

                // --- PERSISTENCIA DIRECTA DE DATOS EN LA BASE DE DATOS RECIÉN CONFIGURADA ---
                var provider = (request.Provider ?? "Oracle").Trim();

                // Consulta dinámica de PIP para obtener datos reales del Usuario Desarrollador Semilla
                DtoFuncionario? pipDevFunc = null;
                string searchIdent = !string.IsNullOrWhiteSpace(request.DevIdentificacion) 
                    ? request.DevIdentificacion.Trim() 
                    : (!string.IsNullOrWhiteSpace(request.DevUser) ? request.DevUser.Trim() : "");

                if (!string.IsNullOrWhiteSpace(searchIdent))
                {
                    try
                    {
                        string tokenPip = "";
                        if (!string.IsNullOrWhiteSpace(request.PipUser) && !string.IsNullOrWhiteSpace(request.PipPassword))
                        {
                            var tokenRespServ = await _tokenService.ObtenerTokenSeviciosAsync(new DtoUsuarioPip
                            {
                                usuario = request.PipUser.Trim(),
                                clave = request.PipPassword.Trim()
                            });
                            tokenPip = tokenRespServ?.Respuesta ?? string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(tokenPip))
                        {
                            var tokenResp = await _tokenService.ObtenerTokenPipAsync(HttpContext.RequestAborted);
                            tokenPip = tokenResp?.Respuesta ?? string.Empty;
                        }

                        if (!string.IsNullOrWhiteSpace(tokenPip))
                        {
                            pipDevFunc = await _tokenService.GetFuncionarioAsync(tokenPip, searchIdent);
                        }
                    }
                    catch (Exception exPip)
                    {
                        _logger.LogWarning(exPip, "No se pudo obtener información de PIP para '{SearchIdent}' durante SaveConfig", searchIdent);
                    }
                }

                string devIdent = !string.IsNullOrWhiteSpace(pipDevFunc?.identificacion)
                    ? pipDevFunc.identificacion.Trim()
                    : (!string.IsNullOrWhiteSpace(request.DevIdentificacion) ? request.DevIdentificacion.Trim() : searchIdent);

                string devUser = !string.IsNullOrWhiteSpace(pipDevFunc?.usuario)
                    ? pipDevFunc.usuario.Trim()
                    : (!string.IsNullOrWhiteSpace(request.DevUser) ? request.DevUser.Trim() : devIdent);

                string devNombres = !string.IsNullOrWhiteSpace(pipDevFunc?.nombres)
                    ? pipDevFunc.nombres.Trim()
                    : (!string.IsNullOrWhiteSpace(request.DevNombres) ? request.DevNombres.Trim() : "");

                string devApellidos = !string.IsNullOrWhiteSpace(pipDevFunc?.apellidos)
                    ? pipDevFunc.apellidos.Trim()
                    : (!string.IsNullOrWhiteSpace(request.DevApellidos) ? request.DevApellidos.Trim() : "");

                string devGrad = !string.IsNullOrWhiteSpace(pipDevFunc?.gradAlfabetico)
                    ? pipDevFunc.gradAlfabetico.Trim()
                    : (!string.IsNullOrWhiteSpace(pipDevFunc?.nombreGrado) 
                        ? pipDevFunc.nombreGrado.Trim() 
                        : (!string.IsNullOrWhiteSpace(request.DevGrad) ? request.DevGrad.Trim() : ""));

                string devFuncStr = !string.IsNullOrWhiteSpace(pipDevFunc?.funcionario)
                    ? pipDevFunc.funcionario.Trim()
                    : $"{devGrad} {devNombres} {devApellidos}".Trim();

                long devUndeLaborando = long.TryParse(pipDevFunc?.undeLaborandoCodigo, out var uL) ? uL : 1L;
                long devCodigoCargo = long.TryParse(pipDevFunc?.codigoCargo, out var cC) ? cC : 1L;

                try
                {
                    if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new MySqlConnector.MySqlConnection(cs);
                        await conn.OpenAsync();

                        // 1. Branding (SOLO insertar si no existe ningún registro previo)
                        if (!string.IsNullOrWhiteSpace(request.Sistema) || !string.IsNullOrWhiteSpace(request.NombreSistema))
                        {
                            using var cmdBranding = conn.CreateCommand();
                            cmdBranding.CommandText = @"
                                INSERT INTO CTR_BRANDING (id_branding, sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, vigente, usuario_creacion, maquina_creacion)
                                SELECT 1, @s, @ns, @uId, @sigla, @nu, @logo, @fav, @f1, 1, 'SETUP', 'LOCAL'
                                WHERE NOT EXISTS (SELECT 1 FROM CTR_BRANDING);";
                            cmdBranding.Parameters.AddWithValue("@s", request.Sistema ?? "GUSOF");
                            cmdBranding.Parameters.AddWithValue("@ns", request.NombreSistema ?? "Sistema de Gestión Institucional");
                            cmdBranding.Parameters.AddWithValue("@uId", request.UnidadSistemaId);
                            cmdBranding.Parameters.AddWithValue("@sigla", request.Sigla ?? "POLICIA");
                            cmdBranding.Parameters.AddWithValue("@nu", request.NombreUnidad ?? "");
                            cmdBranding.Parameters.AddWithValue("@logo", request.LogoFileName ?? "");
                            cmdBranding.Parameters.AddWithValue("@fav", request.FaviconFileName ?? "");
                            cmdBranding.Parameters.AddWithValue("@f1", request.Fondo1FileName ?? "");
                            await cmdBranding.ExecuteNonQueryAsync();
                        }

                        // 2. Dev User (Registrar o actualizar con datos de PIP)
                        long idUsuario = 0;
                        using (var cmdSelect = conn.CreateCommand())
                        {
                            cmdSelect.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser) OR identificacion = @pIdent";
                            cmdSelect.Parameters.AddWithValue("@pUser", devUser);
                            cmdSelect.Parameters.AddWithValue("@pIdent", devIdent);
                            var scalar = await cmdSelect.ExecuteScalarAsync();
                            if (scalar != null && scalar != DBNull.Value) idUsuario = Convert.ToInt64(scalar);
                        }

                        if (idUsuario == 0)
                        {
                            using var cmdInsUser = conn.CreateCommand();
                            cmdInsUser.CommandText = @"
                                INSERT INTO CTR_USUARIOS 
                                (bloqueado, username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, fecha_creacion, maquina_creacion)
                                VALUES
                                (0, @pUser, @pIdent, @pGrad, @pApellidos, @pNombres, @pFuncStr, @pUnde, @pCargo, 1, NOW(), 'SETUP')";
                            cmdInsUser.Parameters.AddWithValue("@pUser", devUser);
                            cmdInsUser.Parameters.AddWithValue("@pIdent", devIdent);
                            cmdInsUser.Parameters.AddWithValue("@pGrad", devGrad);
                            cmdInsUser.Parameters.AddWithValue("@pApellidos", devApellidos);
                            cmdInsUser.Parameters.AddWithValue("@pNombres", devNombres);
                            cmdInsUser.Parameters.AddWithValue("@pFuncStr", devFuncStr);
                            cmdInsUser.Parameters.AddWithValue("@pUnde", devUndeLaborando);
                            cmdInsUser.Parameters.AddWithValue("@pCargo", devCodigoCargo);
                            await cmdInsUser.ExecuteNonQueryAsync();
                            idUsuario = cmdInsUser.LastInsertedId;
                        }
                        else
                        {
                            using var cmdUpdUser = conn.CreateCommand();
                            cmdUpdUser.CommandText = @"
                                UPDATE CTR_USUARIOS 
                                SET username = @pUser, identificacion = @pIdent, grad_alfabetico = @pGrad, apellidos = @pApellidos, nombres = @pNombres, funcionario = @pFuncStr, unde_laborando = @pUnde, codigocargo = @pCargo, bloqueado = 0
                                WHERE id_usuario = @pIdUser";
                            cmdUpdUser.Parameters.AddWithValue("@pUser", devUser);
                            cmdUpdUser.Parameters.AddWithValue("@pIdent", devIdent);
                            cmdUpdUser.Parameters.AddWithValue("@pGrad", devGrad);
                            cmdUpdUser.Parameters.AddWithValue("@pApellidos", devApellidos);
                            cmdUpdUser.Parameters.AddWithValue("@pNombres", devNombres);
                            cmdUpdUser.Parameters.AddWithValue("@pFuncStr", devFuncStr);
                            cmdUpdUser.Parameters.AddWithValue("@pUnde", devUndeLaborando);
                            cmdUpdUser.Parameters.AddWithValue("@pCargo", devCodigoCargo);
                            cmdUpdUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdUpdUser.ExecuteNonQueryAsync();
                        }

                        using (var cmdInsRolAdmin = conn.CreateCommand())
                        {
                            cmdInsRolAdmin.CommandText = "INSERT IGNORE INTO CTR_ROLES_USER_ADMIN (id_usuario, id_rol, vigente, fecha_creacion) VALUES (@pIdUser, 1, 1, NOW());";
                            cmdInsRolAdmin.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdInsRolAdmin.ExecuteNonQueryAsync();
                        }

                        using (var cmdInsRolUser = conn.CreateCommand())
                        {
                            cmdInsRolUser.CommandText = "INSERT IGNORE INTO CTR_ROLES_USER (id_usuario, id_rol, estado, usuario_creacion, fecha_creacion, maquina_creacion) VALUES (@pIdUser, 1, 1, 1, NOW(), 'SETUP');";
                            cmdInsRolUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdInsRolUser.ExecuteNonQueryAsync();
                        }

                        // 3. Dominio Base Seeds
                        using (var cmdDom = conn.CreateCommand())
                        {
                            cmdDom.CommandText = @"
                                INSERT INTO CTR_DOMINIO (id_dominio, descripcion, id_padre, abreviatura, observacion, vigente)
                                SELECT 1, 'General', 0, 'GEN', 'Dominio Base General', 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_DOMINIO WHERE id_dominio = 1);
                                INSERT INTO CTR_DOMINIO (id_dominio, descripcion, id_padre, abreviatura, observacion, vigente)
                                SELECT 2, 'Administración', 1, 'ADM', 'Dominio Administrativo', 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_DOMINIO WHERE id_dominio = 2);";
                            await cmdDom.ExecuteNonQueryAsync();
                        }

                        // 4. Cuenta Email Base Seeds
                        using (var cmdMail = conn.CreateCommand())
                        {
                            cmdMail.CommandText = @"
                                INSERT INTO CTR_CUENTA_EMAIL (id_cuenta, id_dominio_grupo, nombre_cuenta, email, servidor_smtp, puerto, usuario_smtp, clave_smtp, usar_ssl, vigente, usuario_crea)
                                SELECT 1, 1, 'Notificaciones Sistema', 'notificaciones@policia.gov.co', 'smtp.office365.com', 587, 'notificaciones@policia.gov.co', '', 1, 1, 'SETUP'
                                WHERE NOT EXISTS (SELECT 1 FROM CTR_CUENTA_EMAIL WHERE id_cuenta = 1);";
                            await cmdMail.ExecuteNonQueryAsync();
                        }
                    }
                    else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new Npgsql.NpgsqlConnection(cs);
                        await conn.OpenAsync();

                        // 1. Branding
                        if (!string.IsNullOrWhiteSpace(request.Sistema) || !string.IsNullOrWhiteSpace(request.NombreSistema))
                        {
                            using var cmdBranding = conn.CreateCommand();
                            cmdBranding.CommandText = @"
                                INSERT INTO ctr_branding (id_branding, sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, vigente, usuario_creacion, maquina_creacion)
                                VALUES (1, @s, @ns, @uId, @sigla, @nu, @logo, @fav, @f1, 1, 'SETUP', 'LOCAL')
                                ON CONFLICT (id_branding) DO NOTHING;";
                            cmdBranding.Parameters.AddWithValue("@s", request.Sistema ?? "GUSOF");
                            cmdBranding.Parameters.AddWithValue("@ns", request.NombreSistema ?? "Sistema de Gestión Institucional");
                            cmdBranding.Parameters.AddWithValue("@uId", request.UnidadSistemaId);
                            cmdBranding.Parameters.AddWithValue("@sigla", request.Sigla ?? "POLICIA");
                            cmdBranding.Parameters.AddWithValue("@nu", request.NombreUnidad ?? "");
                            cmdBranding.Parameters.AddWithValue("@logo", request.LogoFileName ?? "");
                            cmdBranding.Parameters.AddWithValue("@fav", request.FaviconFileName ?? "");
                            cmdBranding.Parameters.AddWithValue("@f1", request.Fondo1FileName ?? "");
                            await cmdBranding.ExecuteNonQueryAsync();
                        }

                        // 2. Dev User
                        long idUsuario = 0;
                        using (var cmdSelect = conn.CreateCommand())
                        {
                            cmdSelect.CommandText = "SELECT id_usuario FROM ctr_usuarios WHERE LOWER(username) = LOWER(@pUser) OR identificacion = @pIdent";
                            cmdSelect.Parameters.AddWithValue("@pUser", devUser);
                            cmdSelect.Parameters.AddWithValue("@pIdent", devIdent);
                            var scalar = await cmdSelect.ExecuteScalarAsync();
                            if (scalar != null && scalar != DBNull.Value) idUsuario = Convert.ToInt64(scalar);
                        }

                        if (idUsuario == 0)
                        {
                            using var cmdInsUser = conn.CreateCommand();
                            cmdInsUser.CommandText = @"
                                INSERT INTO ctr_usuarios 
                                (bloqueado, username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, fecha_creacion, maquina_creacion)
                                VALUES
                                (0, @pUser, @pIdent, @pGrad, @pApellidos, @pNombres, @pFuncStr, @pUnde, @pCargo, 1, CURRENT_TIMESTAMP, 'SETUP')
                                RETURNING id_usuario";
                            cmdInsUser.Parameters.AddWithValue("@pUser", devUser);
                            cmdInsUser.Parameters.AddWithValue("@pIdent", devIdent);
                            cmdInsUser.Parameters.AddWithValue("@pGrad", devGrad);
                            cmdInsUser.Parameters.AddWithValue("@pApellidos", devApellidos);
                            cmdInsUser.Parameters.AddWithValue("@pNombres", devNombres);
                            cmdInsUser.Parameters.AddWithValue("@pFuncStr", devFuncStr);
                            cmdInsUser.Parameters.AddWithValue("@pUnde", devUndeLaborando);
                            cmdInsUser.Parameters.AddWithValue("@pCargo", devCodigoCargo);
                            var scalarId = await cmdInsUser.ExecuteScalarAsync();
                            if (scalarId != null) idUsuario = Convert.ToInt64(scalarId);
                        }
                        else
                        {
                            using var cmdUpdUser = conn.CreateCommand();
                            cmdUpdUser.CommandText = @"
                                UPDATE ctr_usuarios 
                                SET username = @pUser, identificacion = @pIdent, grad_alfabetico = @pGrad, apellidos = @pApellidos, nombres = @pNombres, funcionario = @pFuncStr, unde_laborando = @pUnde, codigocargo = @pCargo, bloqueado = 0
                                WHERE id_usuario = @pIdUser";
                            cmdUpdUser.Parameters.AddWithValue("@pUser", devUser);
                            cmdUpdUser.Parameters.AddWithValue("@pIdent", devIdent);
                            cmdUpdUser.Parameters.AddWithValue("@pGrad", devGrad);
                            cmdUpdUser.Parameters.AddWithValue("@pApellidos", devApellidos);
                            cmdUpdUser.Parameters.AddWithValue("@pNombres", devNombres);
                            cmdUpdUser.Parameters.AddWithValue("@pFuncStr", devFuncStr);
                            cmdUpdUser.Parameters.AddWithValue("@pUnde", devUndeLaborando);
                            cmdUpdUser.Parameters.AddWithValue("@pCargo", devCodigoCargo);
                            cmdUpdUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdUpdUser.ExecuteNonQueryAsync();
                        }

                        using (var cmdInsRolAdmin = conn.CreateCommand())
                        {
                            cmdInsRolAdmin.CommandText = "INSERT INTO ctr_roles_user_admin (id_usuario, id_rol, vigente, fecha_creacion) VALUES (@pIdUser, 1, 1, CURRENT_TIMESTAMP) ON CONFLICT DO NOTHING;";
                            cmdInsRolAdmin.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdInsRolAdmin.ExecuteNonQueryAsync();
                        }

                        using (var cmdInsRolUser = conn.CreateCommand())
                        {
                            cmdInsRolUser.CommandText = "INSERT INTO ctr_roles_user (id_usuario, id_rol, estado, usuario_creacion, fecha_creacion, maquina_creacion) VALUES (@pIdUser, 1, 1, 1, CURRENT_TIMESTAMP, 'SETUP') ON CONFLICT DO NOTHING;";
                            cmdInsRolUser.Parameters.AddWithValue("@pIdUser", idUsuario);
                            await cmdInsRolUser.ExecuteNonQueryAsync();
                        }

                        // 3. Dominio Base Seeds
                        using (var cmdDom = conn.CreateCommand())
                        {
                            cmdDom.CommandText = @"
                                INSERT INTO ctr_dominio (id_dominio, descripcion, id_padre, abreviatura, observacion, vigente) VALUES (1, 'General', 0, 'GEN', 'Dominio Base General', 1) ON CONFLICT (id_dominio) DO NOTHING;
                                INSERT INTO ctr_dominio (id_dominio, descripcion, id_padre, abreviatura, observacion, vigente) VALUES (2, 'Administración', 1, 'ADM', 'Dominio Administrativo', 1) ON CONFLICT (id_dominio) DO NOTHING;";
                            await cmdDom.ExecuteNonQueryAsync();
                        }

                        // 4. Cuenta Email Base Seeds
                        using (var cmdMail = conn.CreateCommand())
                        {
                            cmdMail.CommandText = @"
                                INSERT INTO ctr_cuenta_email (id_cuenta, id_dominio_grupo, nombre_cuenta, email, servidor_smtp, puerto, usuario_smtp, clave_smtp, usar_ssl, vigente, usuario_crea)
                                VALUES (1, 1, 'Notificaciones Sistema', 'notificaciones@policia.gov.co', 'smtp.office365.com', 587, 'notificaciones@policia.gov.co', '', 1, 1, 'SETUP') ON CONFLICT (id_cuenta) DO NOTHING;";
                            await cmdMail.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Oracle
                        using var conn = new OracleConnection(cs);
                        await conn.OpenAsync();

                        // 1. Branding
                        if (!string.IsNullOrWhiteSpace(request.Sistema) || !string.IsNullOrWhiteSpace(request.NombreSistema))
                        {
                            using var cmdBranding = conn.CreateCommand();
                            cmdBranding.CommandText = @"
                                MERGE INTO CTR_BRANDING b
                                USING (SELECT 1 AS id FROM DUAL) src ON (b.ID_BRANDING = src.id)
                                WHEN NOT MATCHED THEN
                                  INSERT (ID_BRANDING, SISTEMA, NOMBRE_SISTEMA, UNIDAD_SISTEMA_ID, SIGLA, NOMBRE_UNIDAD, LOGO_FILE_NAME, FAVICON_FILE_NAME, FONDO_1_FILE_NAME, VIGENTE, USUARIO_CREACION, MAQUINA_CREACION)
                                  VALUES (1, :s, :ns, :uId, :sigla, :nu, :logo, :fav, :f1, 1, 'SETUP', 'LOCAL')";
                            cmdBranding.Parameters.Add(":s", OracleDbType.Varchar2).Value = request.Sistema ?? "GUSOF";
                            cmdBranding.Parameters.Add(":ns", OracleDbType.Varchar2).Value = request.NombreSistema ?? "Sistema de Gestión Institucional";
                            cmdBranding.Parameters.Add(":uId", OracleDbType.Int32).Value = request.UnidadSistemaId;
                            cmdBranding.Parameters.Add(":sigla", OracleDbType.Varchar2).Value = request.Sigla ?? "POLICIA";
                            cmdBranding.Parameters.Add(":nu", OracleDbType.Varchar2).Value = request.NombreUnidad ?? "";
                            cmdBranding.Parameters.Add(":logo", OracleDbType.Varchar2).Value = request.LogoFileName ?? "";
                            cmdBranding.Parameters.Add(":fav", OracleDbType.Varchar2).Value = request.FaviconFileName ?? "";
                            cmdBranding.Parameters.Add(":f1", OracleDbType.Varchar2).Value = request.Fondo1FileName ?? "";
                            await cmdBranding.ExecuteNonQueryAsync();
                        }

                        // 2. Dev User
                        long idUsuario = 0;
                        long devIdentNum = long.TryParse(devIdent, out var devIdVal) && devIdVal > 0 ? devIdVal : 0L;
                        using (var cmdSelect = conn.CreateCommand())
                        {
                            cmdSelect.CommandText = "SELECT ID_USUARIO FROM CTR_USUARIOS WHERE UPPER(USERNAME) = UPPER(:pUser) OR (IDENTIFICACION > 0 AND IDENTIFICACION = :pIdent)";
                            cmdSelect.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                            cmdSelect.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                            var scalar = await cmdSelect.ExecuteScalarAsync();
                            if (scalar != null && scalar != DBNull.Value) idUsuario = Convert.ToInt64(scalar);
                        }

                        if (idUsuario == 0)
                        {
                            try
                            {
                                using var cmdSeq = conn.CreateCommand();
                                cmdSeq.CommandText = "SELECT SEQ_CTR_USUARIOS.NEXTVAL FROM DUAL";
                                var valSeq = await cmdSeq.ExecuteScalarAsync();
                                idUsuario = Convert.ToInt64(valSeq ?? 1);
                            }
                            catch
                            {
                                idUsuario = 1;
                            }

                            using var cmdInsUser = conn.CreateCommand();
                            cmdInsUser.CommandText = @"
                                INSERT INTO CTR_USUARIOS 
                                (ID_USUARIO, BLOQUEADO, USERNAME, IDENTIFICACION, GRAD_ALFABETICO, APELLIDOS, NOMBRES, FUNCIONARIO, UNDE_LABORANDO, CODIGOCARGO, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                                VALUES
                                (:pId, 0, :pUser, :pIdent, :pGrad, :pApellidos, :pNombres, :pFuncStr, :pUnde, :pCargo, 1, SYSDATE, 'SETUP')";
                            cmdInsUser.Parameters.Add(":pId", OracleDbType.Int64).Value = idUsuario;
                            cmdInsUser.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                            cmdInsUser.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                            cmdInsUser.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = devGrad;
                            cmdInsUser.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = devApellidos;
                            cmdInsUser.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = devNombres;
                            cmdInsUser.Parameters.Add(":pFuncStr", OracleDbType.Varchar2).Value = devFuncStr;
                            cmdInsUser.Parameters.Add(":pUnde", OracleDbType.Int64).Value = devUndeLaborando;
                            cmdInsUser.Parameters.Add(":pCargo", OracleDbType.Int64).Value = devCodigoCargo;
                            await cmdInsUser.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            using var cmdUpdUser = conn.CreateCommand();
                            cmdUpdUser.CommandText = @"
                                UPDATE CTR_USUARIOS 
                                SET USERNAME = :pUser, IDENTIFICACION = :pIdent, GRAD_ALFABETICO = :pGrad, APELLIDOS = :pApellidos, NOMBRES = :pNombres, FUNCIONARIO = :pFuncStr, UNDE_LABORANDO = :pUnde, CODIGOCARGO = :pCargo, BLOQUEADO = 0
                                WHERE ID_USUARIO = :pIdUser";
                            cmdUpdUser.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = devUser;
                            cmdUpdUser.Parameters.Add(":pIdent", OracleDbType.Int64).Value = devIdentNum;
                            cmdUpdUser.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = devGrad;
                            cmdUpdUser.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = devApellidos;
                            cmdUpdUser.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = devNombres;
                            cmdUpdUser.Parameters.Add(":pFuncStr", OracleDbType.Varchar2).Value = devFuncStr;
                            cmdUpdUser.Parameters.Add(":pUnde", OracleDbType.Int64).Value = devUndeLaborando;
                            cmdUpdUser.Parameters.Add(":pCargo", OracleDbType.Int64).Value = devCodigoCargo;
                            cmdUpdUser.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                            await cmdUpdUser.ExecuteNonQueryAsync();
                        }

                        using (var cmdInsRolAdmin = conn.CreateCommand())
                        {
                            long nextUserRolId = 1;
                            try
                            {
                                using var cmdSeqRol = conn.CreateCommand();
                                cmdSeqRol.CommandText = "SELECT SEQ_CTR_ROLES_USER_ADMIN.NEXTVAL FROM DUAL";
                                var valSeqRol = await cmdSeqRol.ExecuteScalarAsync();
                                nextUserRolId = Convert.ToInt64(valSeqRol ?? 1);
                            }
                            catch { }

                            cmdInsRolAdmin.CommandText = @"
                                MERGE INTO CTR_ROLES_USER_ADMIN r USING (SELECT :pIdUser AS u, 1 AS r FROM DUAL) src ON (r.ID_USUARIO = src.u AND r.ID_ROL = src.r)
                                WHEN NOT MATCHED THEN INSERT (ID_USER_ROL, ID_USUARIO, ID_ROL, VIGENTE, FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION) VALUES (:pNextId, :pIdUser, 1, 1, SYSDATE, 'SETUP', 'LOCAL')";
                            cmdInsRolAdmin.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                            cmdInsRolAdmin.Parameters.Add(":pNextId", OracleDbType.Int64).Value = nextUserRolId;
                            await cmdInsRolAdmin.ExecuteNonQueryAsync();
                        }

                        // 3. Dominio Base Seeds
                        using (var cmdDom = conn.CreateCommand())
                        {
                            cmdDom.CommandText = @"
                                MERGE INTO CTR_DOMINIO d USING (SELECT 1 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                                WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, ABREVIATURA, OBSERVACION, VIGENTE) VALUES (1, 'General', 0, 'GEN', 'Dominio Base General', 1)";
                            await cmdDom.ExecuteNonQueryAsync();
                        }

                        // 4. Cuenta Email Base Seeds
                        using (var cmdMail = conn.CreateCommand())
                        {
                            cmdMail.CommandText = @"
                                MERGE INTO CTR_CUENTA_EMAIL c USING (SELECT 1 AS id FROM DUAL) src ON (c.ID_CUENTA = src.id)
                                WHEN NOT MATCHED THEN INSERT (ID_CUENTA, ID_DOMINIO_GRUPO, NOMBRE_CUENTA, EMAIL, SERVIDOR_SMTP, PUERTO, USUARIO_SMTP, CLAVE_SMTP, USAR_SSL, VIGENTE, USUARIO_CREA)
                                VALUES (1, 1, 'Notificaciones Sistema', 'notificaciones@policia.gov.co', 'smtp.office365.com', 587, 'notificaciones@policia.gov.co', '', 1, 1, 'SETUP')";
                            await cmdMail.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception exSeed)
                {
                    Console.WriteLine($"Aviso al realizar seeding inicial en base de datos: {exSeed.Message}");
                }

                return Ok(new { success = true, message = "Configuración guardada y usuario desarrollador registrado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al guardar configuración: {ex.Message}" });
            }
        }

        [HttpPost("shutdown")]
        public IActionResult ShutdownBackend()
        {
            _logger.LogWarning("DETENCIÓN DE BACKEND SOLICITADA DESDE ASISTENTE DE SETUP. El servidor ASP.NET Core se detendrá en 1 segundo...");
            Console.WriteLine("=========================================================");
            Console.WriteLine(" [SETUP] REINICIO DE SERVIDOR BACKEND REQUERIDO");
            Console.WriteLine(" El backend se ha detenido exitosamente tras guardar la configuración.");
            Console.WriteLine(" Por favor ejecute 'dotnet run' o inicie de nuevo la solución.");
            Console.WriteLine("=========================================================");

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                _appLifetime.StopApplication();
            });

            return Ok(new
            {
                success = true,
                message = "Orden de detención enviada al servidor backend exitosamente."
            });
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
                        -- === NIVEL RAÍZ ===
                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 1);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 2, 0, 'Noticias', '/noticias', 'fa-solid fa-newspaper', 2, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 2);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 3, 0, 'Gestión Documental', '/gestion-documental', 'fa-solid fa-folder-open', 3, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 3);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 4, 0, 'Administración', '/administracion', 'fa-solid fa-user-shield', 4, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 4);

                        -- === HIJOS DE GESTIÓN DOCUMENTAL (id_padre = 3) ===
                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 5, 3, 'Correos Electrónicos', '/gestion-documental/gestion-correos-electronicos', 'fa-solid fa-envelope', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 5);

                        -- === HIJOS DE ADMINISTRACIÓN (id_padre = 4) ===
                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 6, 4, 'Usuarios', '/administracion/usuarios', 'fa-solid fa-users', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 6);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 7, 4, 'Roles', '/administracion/roles', 'fa-solid fa-id-badge', 2, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 7);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 8, 4, 'Menú', '/administracion/menu', 'fa-solid fa-bars', 3, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 8);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 9, 4, 'Formularios', '/administracion/formularios', 'fa-solid fa-file-alt', 4, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 9);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 10, 4, 'Línea de Mando', '/administracion/linea-mando', 'fa-solid fa-sitemap', 5, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 10);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 11, 4, 'Personajes del Mes', '/administracion/personaje-mes', 'fa-solid fa-user-check', 6, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 11);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 12, 4, 'Dominio', '/administracion/dominio', 'fa-solid fa-globe', 7, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 12);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 13, 4, 'Auditoría', '/administracion/auditoria', 'fa-solid fa-clipboard-list', 8, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 13);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 14, 4, 'Cuentas Email', '/administracion/cuentas-email', 'fa-solid fa-at', 9, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 14);

                        INSERT INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado)
                        SELECT 15, 4, 'Configuración Sistema', '/administracion/configuracion-sistema', 'fa-solid fa-gears', 10, 1 WHERE NOT EXISTS (SELECT 1 FROM CTR_MENU WHERE id_menu = 15);
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
        public string? TablespaceData { get; set; }
        public string? TablespaceIndexes { get; set; }
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
        public string? DevIdentificacion { get; set; }
        public string? DevNombres { get; set; }
        public string? DevApellidos { get; set; }
        public string? DevEmail { get; set; }
        public string? DevGrad { get; set; }
    }

    public class SetupSaveRequest : SetupTestRequest
    {
    }
}
