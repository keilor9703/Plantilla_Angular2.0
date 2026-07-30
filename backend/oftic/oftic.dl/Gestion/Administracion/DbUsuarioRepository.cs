using Comun.Dtos;
using Comun.Dtos.Auditoria;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Globalization;

namespace Datos.Gestion
{
    public class DbUsuarioRepository : IDbUsuarioRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _cfg;
        private readonly ILogger<DbUsuarioRepository> _logger;
        private readonly IDbRolRepository _rolRepo;
        private readonly IDbAuditoriaRepository _auditoriaRepo;

        public DbUsuarioRepository(IConfiguration cfg, ILogger<DbUsuarioRepository> logger, IDbRolRepository rolRepo, IDbAuditoriaRepository auditoriaRepo)
        {
            _cs = Comun.Extensions.ConfigurationExtensions.GetDecryptedConnectionString(cfg);
            _cfg = cfg;
            _logger = logger;
            _rolRepo = rolRepo;
            _auditoriaRepo = auditoriaRepo;
        }

        public async Task<long?> GetUsuarioIdByUsernameAsync(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser)";
                cmd.Parameters.AddWithValue("@pUser", username.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt64(obj);
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser)";
                cmd.Parameters.AddWithValue("@pUser", username.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt64(obj);
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);
            await using var oracleCmd = oracleConn.CreateCommand();
            oracleCmd.CommandType = CommandType.Text;
            oracleCmd.CommandText = @"SELECT id_usuario FROM CTR_USUARIOS WHERE UPPER(username) = UPPER(:pUsuario)";
            oracleCmd.Parameters.Add(":pUsuario", OracleDbType.Varchar2).Value = username.Trim();
            var res = await oracleCmd.ExecuteScalarAsync(ct);
            return (res is null || res == DBNull.Value) ? null : Convert.ToInt64(res);
        }

        public async Task<long?> GetUsuarioIdByIdentificacionAsync(string identificacion, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(identificacion)) return null;

            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE identificacion = @pIdent";
                cmd.Parameters.AddWithValue("@pIdent", identificacion.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt64(obj);
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id_usuario FROM ctr_usuarios WHERE identificacion = @pIdent";
                cmd.Parameters.AddWithValue("@pIdent", identificacion.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt64(obj);
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);
            await using var oracleCmd = oracleConn.CreateCommand();
            oracleCmd.CommandType = CommandType.Text;
            long pIdentNum = long.TryParse(identificacion, out var idVal) && idVal > 0 ? idVal : 0L;
            oracleCmd.CommandText = @"SELECT id_usuario FROM ctr_usuarios WHERE (identificacion > 0 AND identificacion = :pIdentificacion)";
            oracleCmd.Parameters.Add(":pIdentificacion", OracleDbType.Int64).Value = pIdentNum;
            var res = await oracleCmd.ExecuteScalarAsync(ct);
            return (res is null || res == DBNull.Value) ? null : Convert.ToInt64(res);
        }

        public async Task<string?> GetIdentificacionByUsernameAsync(string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT identificacion FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser)";
                cmd.Parameters.AddWithValue("@pUser", username.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : obj.ToString()?.Trim();
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT identificacion FROM ctr_usuarios WHERE LOWER(username) = LOWER(@pUser)";
                cmd.Parameters.AddWithValue("@pUser", username.Trim());
                var obj = await cmd.ExecuteScalarAsync(ct);
                return (obj is null || obj == DBNull.Value) ? null : obj.ToString()?.Trim();
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);
            await using var oracleCmd = oracleConn.CreateCommand();
            oracleCmd.CommandType = CommandType.Text;
            oracleCmd.CommandText = @"SELECT identificacion FROM ctr_usuarios WHERE UPPER(username) = UPPER(:pUsuario)";
            oracleCmd.Parameters.Add(":pUsuario", OracleDbType.Varchar2).Value = username.Trim();
            var res = await oracleCmd.ExecuteScalarAsync(ct);
            return (res is null || res == DBNull.Value) ? null : res.ToString()?.Trim();
        }

        public async Task<bool?> GetActivoByIdentificacionOrUsernameAsync(string? identificacion, string? username, CancellationToken ct)
        {
            var doc = (identificacion ?? string.Empty).Trim();
            var user = (username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(doc) && string.IsNullOrWhiteSpace(user)) return null;

            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COALESCE(bloqueado, 0) FROM CTR_USUARIOS WHERE (@pDoc <> '' AND identificacion = @pDoc) OR (@pUser <> '' AND LOWER(username) = LOWER(@pUser)) LIMIT 1";
                    cmd.Parameters.AddWithValue("@pDoc", doc);
                    cmd.Parameters.AddWithValue("@pUser", user);
                    var val = await cmd.ExecuteScalarAsync(ct);
                    if (val is null || val == DBNull.Value) return null;
                    return Convert.ToInt32(val) == 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando activo local en MySQL");
                }
            }
            else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COALESCE(bloqueado, 0) FROM ctr_usuarios WHERE (@pDoc <> '' AND identificacion::text = @pDoc) OR (@pUser <> '' AND LOWER(username) = LOWER(@pUser)) LIMIT 1";
                    cmd.Parameters.AddWithValue("@pDoc", doc);
                    cmd.Parameters.AddWithValue("@pUser", user);
                    var val = await cmd.ExecuteScalarAsync(ct);
                    if (val is null || val == DBNull.Value) return null;
                    return Convert.ToInt32(val) == 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando activo local en PostgreSQL");
                }
            }
            else
            {
                try
                {
                    await using var conn = new OracleConnection(_cs);
                    await conn.OpenAsync(ct);
                    await using var cmd = conn.CreateCommand();
                    cmd.BindByName = true;
                    long pIdentNum = long.TryParse(doc, out var idVal) && idVal > 0 ? idVal : 0L;
                    cmd.CommandText = "SELECT NVL(bloqueado, 0) FROM ctr_usuarios WHERE (:pIdentNum > 0 AND identificacion = :pIdentNum) OR (:pUser IS NOT NULL AND UPPER(username) = UPPER(:pUser)) FETCH FIRST 1 ROWS ONLY";
                    cmd.Parameters.Add(":pIdentNum", OracleDbType.Int64).Value = pIdentNum;
                    cmd.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(user) ? DBNull.Value : user;
                    var val = await cmd.ExecuteScalarAsync(ct);
                    if (val is null || val == DBNull.Value) return null;
                    return Convert.ToInt32(val) == 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando activo local en Oracle");
                }
            }

            return null;
        }

        public async Task<bool> SetUsuarioEstadoAsync(string identificacion, bool activo, CancellationToken ct)
        {
            var doc = (identificacion ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(doc)) return false;

            int bloqueado = activo ? 0 : 1;
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE CTR_USUARIOS SET bloqueado = @pBloq WHERE identificacion = @pDoc OR LOWER(username) = LOWER(@pDoc)";
                    cmd.Parameters.AddWithValue("@pBloq", bloqueado);
                    cmd.Parameters.AddWithValue("@pDoc", doc);
                    var rows = await cmd.ExecuteNonQueryAsync(ct);
                    return rows > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando estado de usuario en MySQL");
                    return false;
                }
            }
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ctr_usuarios SET bloqueado = @pBloq WHERE identificacion::text = @pDoc OR LOWER(username) = LOWER(@pDoc)";
                    cmd.Parameters.AddWithValue("@pBloq", bloqueado);
                    cmd.Parameters.AddWithValue("@pDoc", doc);
                    var rows = await cmd.ExecuteNonQueryAsync(ct);
                    return rows > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando estado de usuario en PostgreSQL");
                    return false;
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                long pIdentNum = long.TryParse(doc, out var idVal) && idVal > 0 ? idVal : 0L;
                cmd.CommandText = @"UPDATE ctr_usuarios 
                                       SET bloqueado = :pBloq 
                                     WHERE (:pIdentNum > 0 AND identificacion = :pIdentNum) 
                                        OR UPPER(username) = UPPER(:pDoc)";
                cmd.Parameters.Add(":pBloq", OracleDbType.Int32).Value = bloqueado;
                cmd.Parameters.Add(":pIdentNum", OracleDbType.Int64).Value = pIdentNum;
                cmd.Parameters.Add(":pDoc", OracleDbType.Varchar2).Value = doc;
                var rows = await cmd.ExecuteNonQueryAsync(ct);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado de usuario en Oracle");
                return false;
            }
        }

        public async Task<long> EnsureUsuarioExistsAsync(string username, string? identificacion = null, CancellationToken ct = default)
        {
            return await EnsureUsuarioExistsAsync(new DtoFuncionario
            {
                usuario = username,
                identificacion = string.IsNullOrWhiteSpace(identificacion) ? username : identificacion
            }, ct);
        }

        public async Task<long> EnsureUsuarioExistsAsync(DtoFuncionario funcionario, CancellationToken ct)
        {
            var username = (funcionario.usuario ?? funcionario.identificacion ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("No fue posible resolver username para crear usuario local.");
                return 0;
            }

            var identificacion = (funcionario.identificacion ?? username).Trim();
            var pGrad = !string.IsNullOrWhiteSpace(funcionario.gradAlfabetico) 
                ? funcionario.gradAlfabetico 
                : (!string.IsNullOrWhiteSpace(funcionario.nombreGrado) ? funcionario.nombreGrado : "");
            var pApellidos = funcionario.apellidos ?? "";
            var pNombres = funcionario.nombres ?? username;
            var pFuncStr = !string.IsNullOrWhiteSpace(funcionario.funcionario) 
                ? funcionario.funcionario 
                : $"{pGrad} {pNombres} {pApellidos}".Trim();
            long pUnde = long.TryParse(funcionario.undeLaborandoCodigo, out var uCode) ? uCode : 0L;
            long pCargo = long.TryParse(funcionario.codigoCargo, out var cCode) ? cCode : 0L;

            var existingId = await GetUsuarioIdByUsernameAsync(username, ct);
            if (existingId.HasValue)
            {
                return existingId.Value;
            }

            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);

                using (var cmdInsert = conn.CreateCommand())
                {
                    cmdInsert.CommandText = @"
                        INSERT IGNORE INTO CTR_USUARIOS 
                        (username, identificacion, bloqueado, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, maquina_creacion)
                        VALUES 
                        (@pUser, @pIdent, 0, @pGrad, @pApellidos, @pNombres, @pFuncStr, @pUnde, @pCargo, 1, 'SETUP');";
                    cmdInsert.Parameters.AddWithValue("@pUser", username);
                    cmdInsert.Parameters.AddWithValue("@pIdent", identificacion);
                    cmdInsert.Parameters.AddWithValue("@pGrad", pGrad);
                    cmdInsert.Parameters.AddWithValue("@pApellidos", pApellidos);
                    cmdInsert.Parameters.AddWithValue("@pNombres", pNombres);
                    cmdInsert.Parameters.AddWithValue("@pFuncStr", pFuncStr);
                    cmdInsert.Parameters.AddWithValue("@pUnde", pUnde);
                    cmdInsert.Parameters.AddWithValue("@pCargo", pCargo);
                    await cmdInsert.ExecuteNonQueryAsync(ct);
                }

                using var cmdSelect = conn.CreateCommand();
                cmdSelect.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser);";
                cmdSelect.Parameters.AddWithValue("@pUser", username);
                var res = await cmdSelect.ExecuteScalarAsync(ct);
                _logger.LogInformation("Usuario auto-registrado en MySQL. username={Username}, id_usuario={IdUsuario}", username, res);
                return (res is null || res == DBNull.Value) ? 0L : Convert.ToInt64(res);
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);

                using (var cmdInsert = conn.CreateCommand())
                {
                    cmdInsert.CommandText = @"
                        INSERT INTO ctr_usuarios 
                        (username, identificacion, bloqueado, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, maquina_creacion)
                        VALUES 
                        (@pUser, @pIdent, 0, @pGrad, @pApellidos, @pNombres, @pFuncStr, @pUnde, @pCargo, 1, 'SETUP')
                        ON CONFLICT (username) DO NOTHING;";
                    cmdInsert.Parameters.AddWithValue("@pUser", username);
                    cmdInsert.Parameters.AddWithValue("@pIdent", identificacion);
                    cmdInsert.Parameters.AddWithValue("@pGrad", pGrad);
                    cmdInsert.Parameters.AddWithValue("@pApellidos", pApellidos);
                    cmdInsert.Parameters.AddWithValue("@pNombres", pNombres);
                    cmdInsert.Parameters.AddWithValue("@pFuncStr", pFuncStr);
                    cmdInsert.Parameters.AddWithValue("@pUnde", pUnde);
                    cmdInsert.Parameters.AddWithValue("@pCargo", pCargo);
                    await cmdInsert.ExecuteNonQueryAsync(ct);
                }

                using var cmdSelect = conn.CreateCommand();
                cmdSelect.CommandText = "SELECT id_usuario FROM ctr_usuarios WHERE LOWER(username) = LOWER(@pUser);";
                cmdSelect.Parameters.AddWithValue("@pUser", username);
                var res = await cmdSelect.ExecuteScalarAsync(ct);
                _logger.LogInformation("Usuario auto-registrado en PostgreSQL. username={Username}, id_usuario={IdUsuario}", username, res);
                return (res is null || res == DBNull.Value) ? 0L : Convert.ToInt64(res);
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            await using var tx = oracleConn.BeginTransaction();
            try
            {
                long nextId;
                await using (var cmdNext = oracleConn.CreateCommand())
                {
                    cmdNext.Transaction = tx;
                    cmdNext.CommandType = CommandType.Text;
                    cmdNext.CommandText = "SELECT NVL(MAX(id_usuario), 0) + 1 FROM ctr_usuarios";
                    nextId = Convert.ToInt64(await cmdNext.ExecuteScalarAsync(ct));
                }

                await using (var cmdInsert = oracleConn.CreateCommand())
                {
                    cmdInsert.Transaction = tx;
                    cmdInsert.CommandType = CommandType.Text;
                    cmdInsert.CommandText = @"
INSERT INTO ctr_usuarios (id_usuario, username, identificacion, bloqueado, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, usuario_creacion, maquina_creacion)
VALUES (:pIdUsuario, :pUsername, :pIdentificacion, 0, :pGrad, :pApellidos, :pNombres, :pFuncStr, :pUnde, :pCargo, 1, 'SETUP')";
                    long pIdentNum = long.TryParse(identificacion, out var idVal) && idVal > 0 ? idVal : 0L;
                    cmdInsert.Parameters.Add(":pIdUsuario", OracleDbType.Int64).Value = nextId;
                    cmdInsert.Parameters.Add(":pUsername", OracleDbType.Varchar2).Value = username;
                    cmdInsert.Parameters.Add(":pIdentificacion", OracleDbType.Int64).Value = pIdentNum;
                    cmdInsert.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = pGrad;
                    cmdInsert.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = pApellidos;
                    cmdInsert.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = pNombres;
                    cmdInsert.Parameters.Add(":pFuncStr", OracleDbType.Varchar2).Value = pFuncStr;
                    cmdInsert.Parameters.Add(":pUnde", OracleDbType.Int64).Value = pUnde;
                    cmdInsert.Parameters.Add(":pCargo", OracleDbType.Int64).Value = pCargo;
                    await cmdInsert.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
                _logger.LogInformation("Usuario creado en CTR_USUARIOS (Oracle). username={Username}, id_usuario={IdUsuario}", username, nextId);
                return nextId;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Error creando usuario en CTR_USUARIOS para username={Username}", username);
                throw;
            }
        }

        public async Task<List<DtoUsuarioListadoItem>> GetUsuariosListadoAsync(string? nombre, CancellationToken ct)
        {
            var result = new List<DtoUsuarioListadoItem>();
            var nombreFiltro = (nombre ?? string.Empty).Trim();
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    string mysqlWhere = "WHERE COALESCE(u.bloqueado, 0) = 0";
                    if (!string.IsNullOrWhiteSpace(nombreFiltro))
                    {
                        mysqlWhere += @" AND (LOWER(u.username) LIKE CONCAT('%', LOWER(@pNombre), '%')
                                           OR CAST(u.identificacion AS CHAR) LIKE CONCAT('%', LOWER(@pNombre), '%')
                                           OR LOWER(CONCAT(COALESCE(u.nombres, ''), ' ', COALESCE(u.apellidos, ''))) LIKE CONCAT('%', LOWER(@pNombre), '%'))";
                        cmd.Parameters.AddWithValue("@pNombre", nombreFiltro);
                    }

                    cmd.CommandText = $@"
                        SELECT 
                            u.id_usuario,
                            COALESCE(CAST(u.identificacion AS CHAR), '') AS identificacion,
                            COALESCE(u.username, '') AS username,
                            TRIM(CONCAT(COALESCE(u.grad_alfabetico, ''), ' ', COALESCE(u.nombres, ''), ' ', COALESCE(u.apellidos, ''))) AS nombre_completo,
                            COALESCE(GROUP_CONCAT(DISTINCT COALESCE(r.nombre_rol, r.descripcion, 'Sin rol') SEPARATOR ', '), 'Sin rol') AS roles,
                            MAX(rua.fecha_fin) AS fecha_fin_rol
                        FROM CTR_USUARIOS u
                        LEFT JOIN CTR_ROLES_USER_ADMIN rua ON rua.id_usuario = u.id_usuario AND COALESCE(rua.vigente, 1) = 1
                        LEFT JOIN CTR_ROLES r ON r.id_rol = rua.id_rol
                        {mysqlWhere}
                        GROUP BY u.id_usuario, u.identificacion, u.username, u.grad_alfabetico, u.nombres, u.apellidos
                        ORDER BY u.username;";

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idUs = Convert.ToInt64(reader[0]);
                        var ident = reader.IsDBNull(1) ? string.Empty : Convert.ToString(reader[1]) ?? string.Empty;
                        var userNm = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        var nomComp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        if (string.IsNullOrWhiteSpace(nomComp)) nomComp = userNm;
                        var rolesStr = reader.IsDBNull(4) ? "Sin rol" : reader.GetString(4);
                        var fechaFin = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-dd");

                        result.Add(new DtoUsuarioListadoItem
                        {
                            idUsuario = idUs,
                            identificacion = ident,
                            username = userNm,
                            nombreCompleto = nomComp,
                            rol = rolesStr,
                            fechaFinRol = fechaFin
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando listado de usuarios en MySQL");
                    return result;
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    string pgWhere = "WHERE COALESCE(u.bloqueado, 0) = 0";
                    if (!string.IsNullOrWhiteSpace(nombreFiltro))
                    {
                        pgWhere += @" AND (LOWER(u.username) LIKE LOWER('%' || @pNombre || '%')
                                         OR LOWER(u.identificacion::text) LIKE LOWER('%' || @pNombre || '%')
                                         OR LOWER(CONCAT(COALESCE(u.nombres, ''), ' ', COALESCE(u.apellidos, ''))) LIKE LOWER('%' || @pNombre || '%'))";
                        cmd.Parameters.AddWithValue("@pNombre", nombreFiltro);
                    }

                    cmd.CommandText = $@"
                        SELECT 
                            u.id_usuario,
                            COALESCE(u.identificacion::text, '') AS identificacion,
                            COALESCE(u.username, '') AS username,
                            TRIM(CONCAT(COALESCE(u.grad_alfabetico, ''), ' ', COALESCE(u.nombres, ''), ' ', COALESCE(u.apellidos, ''))) AS nombre_completo,
                            COALESCE(STRING_AGG(DISTINCT COALESCE(r.nombre_rol, r.descripcion, 'Sin rol'), ', '), 'Sin rol') AS roles,
                            MAX(rua.fecha_fin) AS fecha_fin_rol
                        FROM ctr_usuarios u
                        LEFT JOIN CTR_ROLES_USER_ADMIN rua ON rua.id_usuario = u.id_usuario AND COALESCE(rua.vigente, 1) = 1
                        LEFT JOIN CTR_ROLES r ON r.id_rol = rua.id_rol
                        {pgWhere}
                        GROUP BY u.id_usuario, u.identificacion, u.username, u.grad_alfabetico, u.nombres, u.apellidos
                        ORDER BY u.username;";

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idUs = Convert.ToInt64(reader[0]);
                        var ident = reader.IsDBNull(1) ? string.Empty : Convert.ToString(reader[1]) ?? string.Empty;
                        var userNm = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        var nomComp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        if (string.IsNullOrWhiteSpace(nomComp)) nomComp = userNm;
                        var rolesStr = reader.IsDBNull(4) ? "Sin rol" : reader.GetString(4);
                        var fechaFin = reader.IsDBNull(5) ? null : reader.GetDateTime(5).ToString("yyyy-MM-dd");

                        result.Add(new DtoUsuarioListadoItem
                        {
                            idUsuario = idUs,
                            identificacion = ident,
                            username = userNm,
                            nombreCompleto = nomComp,
                            rol = rolesStr,
                            fechaFinRol = fechaFin
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando listado de usuarios en PostgreSQL");
                    return result;
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandType = CommandType.Text;

                string oraWhere = "WHERE NVL(u.bloqueado, 0) = 0";
                if (!string.IsNullOrWhiteSpace(nombreFiltro))
                {
                    oraWhere += @" AND (UPPER(u.username) LIKE '%' || UPPER(:pNombre) || '%'
                                     OR TO_CHAR(u.identificacion) LIKE '%' || :pNombre || '%'
                                     OR UPPER(REGEXP_REPLACE(TRIM(NVL(u.grad_alfabetico, '') || ' ' || NVL(u.nombres, '') || ' ' || NVL(u.apellidos, '')), '[[:space:]]+', ' ')) LIKE '%' || UPPER(:pNombre) || '%')";
                    cmd.Parameters.Add(":pNombre", OracleDbType.Varchar2).Value = nombreFiltro;
                }

                cmd.CommandText = $@"
WITH roles_agg AS (
    SELECT
        x.id_usuario,
        LISTAGG(x.rol_desc, ', ') WITHIN GROUP (ORDER BY x.rol_desc) AS roles,
        LISTAGG(x.fecha_fin_txt, ', ') WITHIN GROUP (ORDER BY x.fecha_fin_txt) AS fecha_fin_rol
    FROM (
        SELECT DISTINCT
            rua.id_usuario,
            NVL(r.nombre_rol, NVL(r.descripcion, 'Sin rol')) AS rol_desc,
            CASE WHEN rua.fecha_fin IS NOT NULL THEN TO_CHAR(rua.fecha_fin, 'YYYY-MM-DD') END AS fecha_fin_txt
        FROM CTR_ROLES_USER_ADMIN rua
        LEFT JOIN ctr_roles r ON r.id_rol = rua.id_rol
        WHERE NVL(rua.vigente, 0) = 1
    ) x
    GROUP BY x.id_usuario
)
SELECT
    u.id_usuario,
    u.identificacion,
    u.username,
    REGEXP_REPLACE(
        TRIM(NVL(u.grad_alfabetico, '') || ' ' || NVL(u.nombres, '') || ' ' || NVL(u.apellidos, '')),
        '[[:space:]]+',
        ' '
    ) AS nombre_completo,
    NVL(ra.roles, 'Sin rol') AS rol,
    ra.fecha_fin_rol
FROM ctr_usuarios u
LEFT JOIN roles_agg ra ON ra.id_usuario = u.id_usuario
{oraWhere}
ORDER BY UPPER(u.username)";

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoUsuarioListadoItem
                    {
                        idUsuario = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                        identificacion = reader.IsDBNull(1) ? string.Empty : Convert.ToString(reader.GetValue(1)) ?? string.Empty,
                        username = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        nombreCompleto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        rol = reader.IsDBNull(4) ? "Sin rol" : reader.GetString(4),
                        fechaFinRol = reader.IsDBNull(5) ? null : reader.GetString(5)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando listado de usuarios en Oracle");
            }

            return result;
        }

        public async Task<List<DtoRolAsignado>> GetRolesAsignadosAsync(string username, CancellationToken ct)
        {
            var idUsuario = await GetUsuarioIdByUsernameAsync(username, ct);
            if (!idUsuario.HasValue)
            {
                return new List<DtoRolAsignado>();
            }

            var result = new List<DtoRolAsignado>();
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT DISTINCT
                            rua.id_rol,
                            COALESCE(r.descripcion, '') AS descripcion_rol,
                            rua.justificacion,
                            rua.fecha_fin,
                            COALESCE(rua.vigente, 1) AS vigente
                        FROM CTR_ROLES_USER_ADMIN rua
                        LEFT JOIN CTR_ROLES r ON r.id_rol = rua.id_rol
                        WHERE rua.id_usuario = @pIdUsuario
                        ORDER BY descripcion_rol, rua.id_rol;";
                    cmd.Parameters.AddWithValue("@pIdUsuario", idUsuario.Value);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idRol = Convert.ToInt32(reader[0]);
                        var descRol = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var just = reader.IsDBNull(2) ? null : reader.GetString(2);
                        var fechaFin = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                        var vigente = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader[4]);
                        var estado = (vigente != 1 || (fechaFin.HasValue && fechaFin.Value.Date < DateTime.Today)) ? "Vencido" : "Vigente";

                        result.Add(new DtoRolAsignado
                        {
                            id = idRol,
                            rol = !string.IsNullOrWhiteSpace(descRol) ? descRol : $"Rol {idRol}",
                            estado = estado,
                            fechaInicio = null,
                            fechaFin = fechaFin?.ToString("yyyy-MM-dd"),
                            justificacion = just
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando roles asignados en MySQL");
                    return result;
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT DISTINCT
                            rua.id_rol,
                            COALESCE(r.descripcion, '') AS descripcion_rol,
                            rua.justificacion,
                            rua.fecha_fin,
                            COALESCE(rua.vigente, 1) AS vigente
                        FROM CTR_ROLES_USER_ADMIN rua
                        LEFT JOIN ctr_roles r ON r.id_rol = rua.id_rol
                        WHERE rua.id_usuario = @pIdUsuario
                        ORDER BY descripcion_rol, rua.id_rol;";
                    cmd.Parameters.AddWithValue("@pIdUsuario", idUsuario.Value);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idRol = Convert.ToInt32(reader[0]);
                        var descRol = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var just = reader.IsDBNull(2) ? null : reader.GetString(2);
                        var fechaFin = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                        var vigente = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader[4]);
                        var estado = (vigente != 1 || (fechaFin.HasValue && fechaFin.Value.Date < DateTime.Today)) ? "Vencido" : "Vigente";

                        result.Add(new DtoRolAsignado
                        {
                            id = idRol,
                            rol = !string.IsNullOrWhiteSpace(descRol) ? descRol : $"Rol {idRol}",
                            estado = estado,
                            fechaInicio = null,
                            fechaFin = fechaFin?.ToString("yyyy-MM-dd"),
                            justificacion = just
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando roles asignados en PostgreSQL");
                    return result;
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    SELECT DISTINCT
                           rua.id_rol,
                           NVL(r.descripcion, 'Sin rol') AS descripcion_rol,
                           rua.justificacion,
                           rua.fecha_fin,
                           NVL(rua.vigente, 0) AS vigente
                    FROM CTR_ROLES_USER_ADMIN rua
                    LEFT JOIN ctr_roles r ON r.id_rol = rua.id_rol
                    WHERE rua.id_usuario = :pIdUsuario
                    ORDER BY descripcion_rol, rua.id_rol";
                cmd.Parameters.Add(":pIdUsuario", OracleDbType.Int64).Value = idUsuario.Value;

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var idRol = Convert.ToInt32(reader.GetInt64(0));
                    var descripcionRol = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var justificacion = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var fechaFin = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                    var vigente = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetDecimal(4));
                    var estado = (vigente != 1 || (fechaFin.HasValue && fechaFin.Value.Date < DateTime.Today)) ? "Vencido" : "Vigente";

                    result.Add(new DtoRolAsignado
                    {
                        id = idRol,
                        rol = !string.IsNullOrWhiteSpace(descripcionRol) ? descripcionRol : $"Rol {idRol}",
                        estado = estado,
                        fechaInicio = null,
                        fechaFin = fechaFin?.ToString("yyyy-MM-dd"),
                        justificacion = justificacion
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando roles asignados en Oracle");
            }

            return result;
        }

        public async Task<DtoGuardarUsuarioResult> EliminarRolAsync(
            long idUsuario,
            int rolId,
            string usuarioAuditoria,
            string maquinaAuditoria,
            string? observaciones = null,
            CancellationToken ct = default)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET vigente = 0, fecha_fin = NOW() WHERE id_usuario = @pIdUser AND id_rol = @pIdRol;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);

                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    if (affected > 0)
                    {
                        try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} retirado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                    }
                    return new DtoGuardarUsuarioResult
                    {
                        idUsuario = affected > 0 ? idUsuario : 0,
                        message = affected > 0 ? "Rol retirado correctamente." : "No se encontró un rol activo para retirar."
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error eliminando rol en MySQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en MySQL: {ex.Message}" };
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET vigente = 0, fecha_fin = CURRENT_TIMESTAMP WHERE id_usuario = @pIdUser AND id_rol = @pIdRol;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);

                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    if (affected > 0)
                    {
                        try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} retirado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                    }
                    return new DtoGuardarUsuarioResult
                    {
                        idUsuario = affected > 0 ? idUsuario : 0,
                        message = affected > 0 ? "Rol retirado correctamente." : "No se encontró un rol activo para retirar."
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error eliminando rol en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
UPDATE CTR_ROLES_USER_ADMIN
   SET vigente = 0,
       fecha_fin = NVL(fecha_fin, TRUNC(SYSDATE))
 WHERE id_usuario = :pIdUsuario
   AND id_rol = :pIdRol
   AND NVL(vigente, 0) = 1";

                cmd.Parameters.Add(":pIdUsuario", OracleDbType.Int64).Value = idUsuario;
                cmd.Parameters.Add(":pIdRol", OracleDbType.Int32).Value = rolId;

                var affected = await cmd.ExecuteNonQueryAsync(ct);
                if (affected > 0)
                {
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} retirado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                }
                return new DtoGuardarUsuarioResult
                {
                    idUsuario = affected > 0 ? idUsuario : 0,
                    message = affected > 0 ? "Rol retirado correctamente." : "No se encontró un rol vigente para retirar."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public async Task<DtoGuardarUsuarioResult> EditarRolAsync(
            long idUsuario,
            int rolId,
            string justificacion,
            DateTime fechaFin,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE CTR_ROLES_USER_ADMIN
                           SET justificacion = @pJust, fecha_fin = @pFechaFin
                         WHERE id_usuario = @pIdUser AND id_rol = @pIdRol;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    cmd.Parameters.AddWithValue("@pJust", (object?)justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", fechaFin.Date);

                    await cmd.ExecuteNonQueryAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "EDITAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} actualizado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol actualizado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando rol en MySQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en MySQL: {ex.Message}" };
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE CTR_ROLES_USER_ADMIN
                           SET justificacion = @pJust, fecha_fin = @pFechaFin
                         WHERE id_usuario = @pIdUser AND id_rol = @pIdRol;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    cmd.Parameters.AddWithValue("@pJust", (object?)justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", fechaFin.Date);

                    await cmd.ExecuteNonQueryAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "EDITAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} actualizado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol actualizado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando rol en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                await using var cmd = oracleConn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    UPDATE CTR_ROLES_USER_ADMIN
                       SET justificacion = :pJust, fecha_fin = :pFechaFin
                     WHERE id_usuario = :pIdUser AND id_rol = :pIdRol";

                cmd.Parameters.Add(":pJust", OracleDbType.Varchar2).Value = (object?)justificacion ?? DBNull.Value;
                cmd.Parameters.Add(":pFechaFin", OracleDbType.Date).Value = fechaFin.Date;
                cmd.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                cmd.Parameters.Add(":pIdRol", OracleDbType.Int32).Value = rolId;

                await cmd.ExecuteNonQueryAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "EDITAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {rolId} actualizado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol actualizado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public async Task<List<DtoRol>> GetRolesAsync(CancellationToken ct)
        {
            var result = new List<DtoRol>();
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, '') FROM CTR_ROLES WHERE COALESCE(estado, 1) = 1 ORDER BY id_rol;";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = Convert.ToInt32(reader.GetInt64(0)),
                            nombre = reader.IsDBNull(1) ? null : reader.GetString(1)
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando roles en MySQL");
                    return result;
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, '') FROM ctr_roles WHERE COALESCE(estado, 1) = 1 ORDER BY id_rol;";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = Convert.ToInt32(reader.GetInt64(0)),
                            nombre = reader.IsDBNull(1) ? null : reader.GetString(1)
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando roles en PostgreSQL");
                    return result;
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
SELECT id_rol, NVL(nombre_rol, descripcion)
FROM ctr_roles
WHERE NVL(estado, 1) = 1
ORDER BY id_rol";

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoRol
                    {
                        id = Convert.ToInt32(reader.GetInt64(0)),
                        nombre = reader.IsDBNull(1) ? null : reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando roles en Oracle");
            }

            return result;
        }

        public async Task<DtoGuardarUsuarioResult> SaveUsuarioAsync(
            DtoUsuarioRequest request,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            var username = (request.username ?? request.identificacion ?? string.Empty).Trim();
            var identificacion = (request.identificacion ?? string.Empty).Trim();
            var nombres = (request.nombres ?? string.Empty).Trim();
            var apellidos = (request.apellidos ?? string.Empty).Trim();
            var gradAlfabetico = !string.IsNullOrWhiteSpace(request.gradAlfabetico) ? request.gradAlfabetico.Trim() : "ING";
            var funcStr = $"{gradAlfabetico} {nombres} {apellidos}".Trim();
            var undeLaborando = AsIntOrZero(request.undeLaborando);
            var codigoCargo = AsIntOrZero(request.codigoCargo);
            var bloqueado = request.activo ? 0 : 1;

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO CTR_USUARIOS (
                            username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, bloqueado, usuario_creacion, maquina_creacion
                        ) VALUES (
                            @pUser, @pIdent, @pGrad, @pApellidos, @pNombres, @pFunc, @pUnde, @pCargo, @pBloqueado, 1, 'LOCAL'
                        )
                        ON DUPLICATE KEY UPDATE 
                            identificacion = VALUES(identificacion),
                            grad_alfabetico = VALUES(grad_alfabetico),
                            apellidos = VALUES(apellidos),
                            nombres = VALUES(nombres),
                            funcionario = VALUES(funcionario),
                            unde_laborando = VALUES(unde_laborando),
                            codigocargo = VALUES(codigocargo),
                            bloqueado = VALUES(bloqueado);

                        SELECT id_usuario FROM CTR_USUARIOS WHERE LOWER(username) = LOWER(@pUser);";

                    cmd.Parameters.AddWithValue("@pUser", username);
                    cmd.Parameters.AddWithValue("@pIdent", identificacion);
                    cmd.Parameters.AddWithValue("@pGrad", gradAlfabetico);
                    cmd.Parameters.AddWithValue("@pApellidos", apellidos);
                    cmd.Parameters.AddWithValue("@pNombres", nombres);
                    cmd.Parameters.AddWithValue("@pFunc", funcStr);
                    cmd.Parameters.AddWithValue("@pUnde", undeLaborando);
                    cmd.Parameters.AddWithValue("@pCargo", codigoCargo);
                    cmd.Parameters.AddWithValue("@pBloqueado", bloqueado);

                    var res = await cmd.ExecuteScalarAsync(ct);
                    var idUsuario = (res is null || res == DBNull.Value) ? 0L : Convert.ToInt64(res);

                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "GUARDAR", Registro = $"Usuario: {username} ({identificacion})", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult
                    {
                        idUsuario = idUsuario > 0 ? idUsuario : 1L,
                        message = "Usuario guardado correctamente."
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando usuario en MySQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en MySQL: {ex.Message}" };
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO ctr_usuarios (
                            username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, bloqueado, usuario_creacion, maquina_creacion
                        ) VALUES (
                            @pUser, @pIdent, @pGrad, @pApellidos, @pNombres, @pFunc, @pUnde, @pCargo, @pBloqueado, 1, 'LOCAL'
                        )
                        ON CONFLICT (username) DO UPDATE SET
                            identificacion = EXCLUDED.identificacion,
                            grad_alfabetico = EXCLUDED.grad_alfabetico,
                            apellidos = EXCLUDED.apellidos,
                            nombres = EXCLUDED.nombres,
                            funcionario = EXCLUDED.funcionario,
                            unde_laborando = EXCLUDED.unde_laborando,
                            codigocargo = EXCLUDED.codigocargo,
                            bloqueado = EXCLUDED.bloqueado;

                        SELECT id_usuario FROM ctr_usuarios WHERE LOWER(username) = LOWER(@pUser);";

                    cmd.Parameters.AddWithValue("@pUser", username);
                    cmd.Parameters.AddWithValue("@pIdent", identificacion);
                    cmd.Parameters.AddWithValue("@pGrad", gradAlfabetico);
                    cmd.Parameters.AddWithValue("@pApellidos", apellidos);
                    cmd.Parameters.AddWithValue("@pNombres", nombres);
                    cmd.Parameters.AddWithValue("@pFunc", funcStr);
                    cmd.Parameters.AddWithValue("@pUnde", undeLaborando);
                    cmd.Parameters.AddWithValue("@pCargo", codigoCargo);
                    cmd.Parameters.AddWithValue("@pBloqueado", bloqueado);

                    var res = await cmd.ExecuteScalarAsync(ct);
                    var idUsuario = (res is null || res == DBNull.Value) ? 0L : Convert.ToInt64(res);

                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "GUARDAR", Registro = $"Usuario: {username} ({identificacion})", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult
                    {
                        idUsuario = idUsuario > 0 ? idUsuario : 1L,
                        message = "Usuario guardado correctamente."
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando usuario en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    MERGE INTO CTR_USUARIOS target
                    USING (SELECT :pUser AS username FROM dual) src
                    ON (LOWER(target.username) = LOWER(src.username))
                    WHEN MATCHED THEN
                      UPDATE SET 
                        target.identificacion = :pIdent,
                        target.grad_alfabetico = :pGrad,
                        target.apellidos = :pApellidos,
                        target.nombres = :pNombres,
                        target.funcionario = :pFunc,
                        target.unde_laborando = :pUnde,
                        target.codigocargo = :pCargo,
                        target.bloqueado = :pBloqueado
                    WHEN NOT MATCHED THEN
                      INSERT (
                        id_usuario, username, identificacion, grad_alfabetico, apellidos, nombres, funcionario, unde_laborando, codigocargo, bloqueado, usuario_creacion, fecha_creacion, maquina_creacion
                      ) VALUES (
                        (SELECT NVL(MAX(id_usuario), 0) + 1 FROM CTR_USUARIOS),
                        :pUser, :pIdent, :pGrad, :pApellidos, :pNombres, :pFunc, :pUnde, :pCargo, :pBloqueado, 1, SYSDATE, 'LOCAL'
                      )";

                cmd.Parameters.Add(":pUser", OracleDbType.Varchar2).Value = username;
                cmd.Parameters.Add(":pIdent", OracleDbType.Varchar2).Value = identificacion;
                cmd.Parameters.Add(":pGrad", OracleDbType.Varchar2).Value = gradAlfabetico;
                cmd.Parameters.Add(":pApellidos", OracleDbType.Varchar2).Value = apellidos;
                cmd.Parameters.Add(":pNombres", OracleDbType.Varchar2).Value = nombres;
                cmd.Parameters.Add(":pFunc", OracleDbType.Varchar2).Value = funcStr;
                cmd.Parameters.Add(":pUnde", OracleDbType.Int64).Value = undeLaborando;
                cmd.Parameters.Add(":pCargo", OracleDbType.Int64).Value = codigoCargo;
                cmd.Parameters.Add(":pBloqueado", OracleDbType.Int32).Value = bloqueado;

                await cmd.ExecuteNonQueryAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "GUARDAR", Registro = $"Usuario: {username} ({identificacion})", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                return new DtoGuardarUsuarioResult { idUsuario = 1, message = "Usuario guardado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando usuario en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public async Task<DtoGuardarUsuarioResult> EliminarUsuarioAsync(
            long idUsuario,
            string usuarioAuditoria,
            string maquinaAuditoria,
            string? observaciones = null,
            CancellationToken ct = default)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            // ── MySQL ────────────────────────────────────────────────────────
            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var tx = await conn.BeginTransactionAsync(ct);

                    using (var cmd1 = conn.CreateCommand())
                    {
                        cmd1.Transaction = tx;
                        cmd1.CommandText = "UPDATE CTR_USUARIOS SET bloqueado = 1 WHERE id_usuario = @pId";
                        cmd1.Parameters.AddWithValue("@pId", idUsuario);
                        var affected = await cmd1.ExecuteNonQueryAsync(ct);
                        if (affected <= 0)
                        {
                            await tx.RollbackAsync(ct);
                            return new DtoGuardarUsuarioResult { idUsuario = 0, message = "No se encontró el usuario para eliminar." };
                        }
                    }

                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.Transaction = tx;
                        cmd2.CommandText = @"UPDATE CTR_ROLES_USER_ADMIN
   SET vigente = 0, fecha_fin = COALESCE(fecha_fin, CURDATE())
 WHERE id_usuario = @pId AND COALESCE(vigente, 0) = 1";
                        cmd2.Parameters.AddWithValue("@pId", idUsuario);
                        await cmd2.ExecuteNonQueryAsync(ct);
                    }

                    await tx.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR", Registro = $"Usuario ID {idUsuario} bloqueado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Usuario eliminado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error MySQL eliminando usuario id={IdUsuario}", idUsuario);
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error MySQL: {ex.Message}" };
                }
            }

            // ── PostgreSQL ───────────────────────────────────────────────────
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var tx = await conn.BeginTransactionAsync(ct);

                    using (var cmd1 = conn.CreateCommand())
                    {
                        cmd1.Transaction = tx;
                        cmd1.CommandText = "UPDATE CTR_USUARIOS SET bloqueado = 1 WHERE id_usuario = @pId";
                        cmd1.Parameters.AddWithValue("@pId", idUsuario);
                        var affected = await cmd1.ExecuteNonQueryAsync(ct);
                        if (affected <= 0)
                        {
                            await tx.RollbackAsync(ct);
                            return new DtoGuardarUsuarioResult { idUsuario = 0, message = "No se encontró el usuario para eliminar." };
                        }
                    }

                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.Transaction = tx;
                        cmd2.CommandText = @"UPDATE CTR_ROLES_USER_ADMIN
   SET vigente = 0, fecha_fin = COALESCE(fecha_fin, CURRENT_DATE)
 WHERE id_usuario = @pId AND COALESCE(vigente, 0) = 1";
                        cmd2.Parameters.AddWithValue("@pId", idUsuario);
                        await cmd2.ExecuteNonQueryAsync(ct);
                    }

                    await tx.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR", Registro = $"Usuario ID {idUsuario} bloqueado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Usuario eliminado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error PostgreSQL eliminando usuario id={IdUsuario}", idUsuario);
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error PostgreSQL: {ex.Message}" };
                }
            }

            // ── Oracle (default) ─────────────────────────────────────────────
            try
            {
                await using var oraConn = new OracleConnection(_cs);
                await oraConn.OpenAsync(ct);
                await using var oraTx = oraConn.BeginTransaction();

                await using (var cmd1 = oraConn.CreateCommand())
                {
                    cmd1.Transaction = oraTx;
                    cmd1.BindByName = true;
                    cmd1.CommandText = "UPDATE CTR_USUARIOS SET bloqueado = 1 WHERE id_usuario = :pId";
                    cmd1.Parameters.Add(":pId", OracleDbType.Int64).Value = idUsuario;
                    var affected = await cmd1.ExecuteNonQueryAsync(ct);
                    if (affected <= 0)
                    {
                        await oraTx.RollbackAsync(ct);
                        return new DtoGuardarUsuarioResult { idUsuario = 0, message = "No se encontró el usuario para eliminar." };
                    }
                }

                await using (var cmd2 = oraConn.CreateCommand())
                {
                    cmd2.Transaction = oraTx;
                    cmd2.BindByName = true;
                    cmd2.CommandText = @"UPDATE CTR_ROLES_USER_ADMIN
   SET vigente = 0, fecha_fin = NVL(fecha_fin, TRUNC(SYSDATE))
 WHERE id_usuario = :pId AND NVL(vigente, 0) = 1";
                    cmd2.Parameters.Add(":pId", OracleDbType.Int64).Value = idUsuario;
                    await cmd2.ExecuteNonQueryAsync(ct);
                }

                await oraTx.CommitAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ELIMINAR", Registro = $"Usuario ID {idUsuario} bloqueado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Usuario eliminado correctamente." };
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Error Oracle eliminando usuario id={IdUsuario}", idUsuario);
                throw new InvalidOperationException($"Oracle: {ex.Message}", ex);
            }
        }

        public async Task<DtoGuardarUsuarioResult> AsignarRolAsync(
            long idUsuario,
            DtoAsignarRolRequest request,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO CTR_ROLES_USER_ADMIN (id_usuario, id_rol, justificacion, fecha_fin, vigente)
                        VALUES (@pIdUser, @pIdRol, @pJust, @pFechaFin, 1)
                        ON DUPLICATE KEY UPDATE 
                            justificacion = VALUES(justificacion),
                            fecha_fin = VALUES(fecha_fin),
                            vigente = 1;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", request.rolId);
                    cmd.Parameters.AddWithValue("@pJust", (object?)request.justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", DateTime.TryParse(request.fechaFin, out var ff) ? ff.Date : (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ASIGNAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {request.rolId} asignado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol asignado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error asignando rol en MySQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en MySQL: {ex.Message}" };
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using (var cmdTable = conn.CreateCommand())
                    {
                        cmdTable.CommandText = @"
                            CREATE TABLE IF NOT EXISTS CTR_ROLES_USER_ADMIN (
                                id_usuario BIGINT NOT NULL,
                                id_rol INT NOT NULL,
                                justificacion VARCHAR(1000),
                                fecha_fin DATE,
                                vigente INT DEFAULT 1,
                                PRIMARY KEY (id_usuario, id_rol)
                            );";
                        await cmdTable.ExecuteNonQueryAsync(ct);
                    }

                    // Reparar esquema incompleto de tablas anteriores (PostgreSQL IF NOT EXISTS)
                    using (var cmdAlter = conn.CreateCommand())
                    {
                        cmdAlter.CommandText = @"
                            ALTER TABLE CTR_ROLES_USER_ADMIN
                                ADD COLUMN IF NOT EXISTS justificacion VARCHAR(1000),
                                ADD COLUMN IF NOT EXISTS fecha_fin DATE,
                                ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1;";
                        await cmdAlter.ExecuteNonQueryAsync(ct);
                    }

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO CTR_ROLES_USER_ADMIN (id_usuario, id_rol, justificacion, fecha_fin, vigente)
                        VALUES (@pIdUser, @pIdRol, @pJust, @pFechaFin, 1)
                        ON CONFLICT (id_usuario, id_rol) DO UPDATE SET 
                            justificacion = EXCLUDED.justificacion,
                            fecha_fin = EXCLUDED.fecha_fin,
                            vigente = 1;";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", request.rolId);
                    cmd.Parameters.AddWithValue("@pJust", (object?)request.justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", DateTime.TryParse(request.fechaFin, out var ff2) ? ff2.Date : (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ASIGNAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {request.rolId} asignado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol asignado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error asignando rol en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                await using var cmd = oracleConn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    MERGE INTO CTR_ROLES_USER_ADMIN target
                    USING (
                        SELECT :pIdUser AS id_usuario, 
                               :pIdRol AS id_rol,
                               (SELECT NVL(MAX(id_user_rol), 0) + 1 FROM CTR_ROLES_USER_ADMIN) AS next_id 
                        FROM dual
                    ) src
                    ON (target.id_usuario = src.id_usuario AND target.id_rol = src.id_rol)
                    WHEN MATCHED THEN
                      UPDATE SET target.justificacion = :pJust, target.fecha_fin = :pFechaFin, target.vigente = 1
                    WHEN NOT MATCHED THEN
                      INSERT (id_user_rol, id_usuario, id_rol, justificacion, fecha_fin, vigente, fecha_creacion, usuario_creacion, maquina_creacion)
                      VALUES (src.next_id, src.id_usuario, src.id_rol, :pJust, :pFechaFin, 1, SYSDATE, 'ADMIN', 'LOCAL')";

                cmd.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                cmd.Parameters.Add(":pIdRol", OracleDbType.Int32).Value = request.rolId;
                cmd.Parameters.Add(":pJust", OracleDbType.Varchar2).Value = (object?)request.justificacion ?? DBNull.Value;
                cmd.Parameters.Add(":pFechaFin", OracleDbType.Date).Value = DateTime.TryParse(request.fechaFin, out var ff3) ? ff3.Date : (object)DBNull.Value;

                await cmd.ExecuteNonQueryAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "USUARIOS", Accion = "ASIGNAR ROL", Registro = $"Usuario ID {idUsuario}: Rol {request.rolId} asignado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol asignado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public Task<List<DtoRolAdmin>> GetRolesAdminAsync(CancellationToken ct)
            => _rolRepo.GetRolesAdminAsync(ct);

        public Task<DtoSaveRolResult> SaveRolAdminAsync(
            DtoSaveRolRequest request,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
            => _rolRepo.SaveRolAdminAsync(request, usuarioAuditoria, maquinaAuditoria, ct);

        public Task<DtoSaveRolResult> SetRolEstadoAsync(
            long idRol,
            int vigente,
            string usuarioAuditoria,
            string maquinaAuditoria,
            string? observaciones = null,
            CancellationToken ct = default)
            => _rolRepo.SetRolEstadoAsync(idRol, vigente, usuarioAuditoria, maquinaAuditoria, observaciones, ct);

        private static object AsDbValue(string? value, int? maxLen = null)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (maxLen.HasValue && normalized.Length > maxLen.Value)
            {
                normalized = normalized[..maxLen.Value];
            }
            return string.IsNullOrWhiteSpace(normalized) ? DBNull.Value : normalized;
        }

        private static object AsRequiredDbValue(string? value, string fallback, int? maxLen = null)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = fallback;
            }

            if (maxLen.HasValue && normalized.Length > maxLen.Value)
            {
                normalized = normalized[..maxLen.Value];
            }

            return normalized;
        }

        private static object AsNullableInt(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return DBNull.Value;
            }

            return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : DBNull.Value;
        }

        private static int AsIntOrZero(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static long ToInt64Safe(object value)
        {
            if (value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.IsNull ? 0L : oracleDecimal.ToInt64();
            }

            if (value is decimal dec)
            {
                return (long)dec;
            }

            if (value is long l)
            {
                return l;
            }

            if (value is int i)
            {
                return i;
            }

            if (long.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }

            return 0L;
        }

        private static long AsLongOrZero(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0L;
        }

        // These helpers are only called from the Oracle branch of GetFuncionarioLocalAsync.
        // If no Oracle connection is available they simply return null.
        private static async Task<int?> GetBloqueadoByIdentificacionAsync(
            OracleConnection conn,
            string identificacion,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(identificacion)) return null;

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT bloqueado FROM ctr_usuarios
 WHERE identificacion = :pIdentificacion AND rownum = 1";
            cmd.Parameters.Add(":pIdentificacion", OracleDbType.Varchar2).Value = identificacion;

            var obj = await cmd.ExecuteScalarAsync(ct);
            return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt32(obj);
        }

        private static async Task<int?> GetBloqueadoByUsernameAsync(
            OracleConnection conn,
            string username,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT bloqueado FROM ctr_usuarios
 WHERE UPPER(username) = UPPER(:pUsername) AND rownum = 1";
            cmd.Parameters.Add(":pUsername", OracleDbType.Varchar2).Value = username;

            var obj = await cmd.ExecuteScalarAsync(ct);
            return (obj is null || obj == DBNull.Value) ? null : Convert.ToInt32(obj);
        }
    }
}
