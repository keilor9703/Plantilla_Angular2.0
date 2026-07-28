using Comun.Dtos;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Datos.Gestion
{
    public class DbRolRepository : IDbRolRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _cfg;
        private readonly ILogger<DbRolRepository> _logger;

        public DbRolRepository(IConfiguration cfg, ILogger<DbRolRepository> logger)
        {
            _cs = cfg.GetDecryptedConnectionString();
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<List<DtoRolAdmin>> GetRolesAdminAsync(CancellationToken ct)
        {
            var result = new List<DtoRolAdmin>();
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            // ── MySQL ────────────────────────────────────────────────────────
            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, codigo_rol, 'Sin nombre') AS nombre, COALESCE(estado, vigente, 1) AS vigente FROM ctr_roles ORDER BY id_rol";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRolAdmin
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.IsDBNull(1) ? null : reader.GetString(1),
                            vigente = reader.IsDBNull(2) ? 1 : reader.GetInt32(2)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error MySQL leyendo CTR_ROLES");
                }
                return result;
            }

            // ── PostgreSQL ───────────────────────────────────────────────────
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, codigo_rol, 'Sin nombre') AS nombre, COALESCE(estado, vigente, 1) AS vigente FROM ctr_roles ORDER BY id_rol";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRolAdmin
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.IsDBNull(1) ? null : reader.GetString(1),
                            vigente = reader.IsDBNull(2) ? 1 : reader.GetInt32(2)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error PostgreSQL leyendo CTR_ROLES");
                }
                return result;
            }

            // ── Oracle (default) ─────────────────────────────────────────────
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;

                try
                {
                    cmd.CommandText = "SELECT id_rol, NVL(nombre_rol, NVL(descripcion, NVL(codigo_rol, 'Sin nombre'))) AS nombre, NVL(estado, 1) AS vigente FROM ctr_roles ORDER BY id_rol";
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idVal = reader.GetValue(0);
                        var nomVal = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var vigVal = reader.IsDBNull(2) ? 1 : Convert.ToInt32(reader.GetValue(2));

                        result.Add(new DtoRolAdmin
                        {
                            id = Convert.ToInt32(ToInt64Safe(idVal)),
                            nombre = nomVal,
                            vigente = vigVal
                        });
                    }
                }
                catch (OracleException ex) when (ex.Number == 904)
                {
                    cmd.CommandText = "SELECT id_rol, NVL(nombre_rol, NVL(descripcion, NVL(codigo_rol, 'Sin nombre'))) AS nombre, NVL(vigente, 1) AS vigente FROM ctr_roles ORDER BY id_rol";
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var idVal = reader.GetValue(0);
                        var nomVal = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var vigVal = reader.IsDBNull(2) ? 1 : Convert.ToInt32(reader.GetValue(2));

                        result.Add(new DtoRolAdmin
                        {
                            id = Convert.ToInt32(ToInt64Safe(idVal)),
                            nombre = nomVal,
                            vigente = vigVal
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Oracle leyendo CTR_ROLES: {Message}", ex.Message);
            }

            return result;
        }

        public async Task<DtoSaveRolResult> SaveRolAdminAsync(
            DtoSaveRolRequest request,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var nombre = (request.nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return new DtoSaveRolResult { idRol = 0, message = "La descripción del rol es obligatoria." };
            }

            var vigente = request.vigente == 0 ? 0 : 1;
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            // ── MySQL ────────────────────────────────────────────────────────
            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    var idRol = request.id.GetValueOrDefault();
                    if (idRol > 0)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"UPDATE ctr_roles
   SET nombre_rol = @pNombre, descripcion = @pNombre, vigente = @pVigente
 WHERE id_rol = @pId";
                        cmd.Parameters.AddWithValue("@pNombre", nombre);
                        cmd.Parameters.AddWithValue("@pVigente", vigente);
                        cmd.Parameters.AddWithValue("@pId", idRol);
                        var affected = await cmd.ExecuteNonQueryAsync(ct);
                        if (affected <= 0) return new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol para actualizar." };
                        return new DtoSaveRolResult { idRol = idRol, message = "Rol actualizado correctamente." };
                    }
                    else
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO ctr_roles (codigo_rol, nombre_rol, descripcion, vigente) VALUES (@pCodigo, @pNombre, @pNombre, @pVigente); SELECT LAST_INSERT_ID();";
                        cmd.Parameters.AddWithValue("@pCodigo", nombre.ToUpper().Replace(" ", "_"));
                        cmd.Parameters.AddWithValue("@pNombre", nombre);
                        cmd.Parameters.AddWithValue("@pVigente", vigente);
                        var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
                        return new DtoSaveRolResult { idRol = newId, message = "Rol creado correctamente." };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error MySQL guardando rol");
                    return new DtoSaveRolResult { idRol = 0, message = $"Error MySQL: {ex.Message}" };
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

                    var idRol = request.id.GetValueOrDefault();
                    if (idRol > 0)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"UPDATE ctr_roles
   SET nombre_rol = @pNombre, descripcion = @pNombre, vigente = @pVigente
 WHERE id_rol = @pId";
                        cmd.Parameters.AddWithValue("@pNombre", nombre);
                        cmd.Parameters.AddWithValue("@pVigente", vigente);
                        cmd.Parameters.AddWithValue("@pId", idRol);
                        var affected = await cmd.ExecuteNonQueryAsync(ct);
                        if (affected <= 0) return new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol para actualizar." };
                        return new DtoSaveRolResult { idRol = idRol, message = "Rol actualizado correctamente." };
                    }
                    else
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO ctr_roles (codigo_rol, nombre_rol, descripcion, vigente) VALUES (@pCodigo, @pNombre, @pNombre, @pVigente) RETURNING id_rol";
                        cmd.Parameters.AddWithValue("@pCodigo", nombre.ToUpper().Replace(" ", "_"));
                        cmd.Parameters.AddWithValue("@pNombre", nombre);
                        cmd.Parameters.AddWithValue("@pVigente", vigente);
                        var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
                        return new DtoSaveRolResult { idRol = newId, message = "Rol creado correctamente." };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error PostgreSQL guardando rol");
                    return new DtoSaveRolResult { idRol = 0, message = $"Error PostgreSQL: {ex.Message}" };
                }
            }

            // ── Oracle (default) ─────────────────────────────────────────────
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var tx = conn.BeginTransaction();

                var idRol2 = request.id.GetValueOrDefault();
                if (idRol2 > 0)
                {
                    await using var cmdU = conn.CreateCommand();
                    cmdU.Transaction = tx;
                    cmdU.BindByName = true;
                    cmdU.CommandText = @"UPDATE ctr_roles
   SET nombre_rol = :pNombre, descripcion = :pNombre, estado = :pVigente,
       usuario_modifica = :pUsr, fecha_modifica = SYSDATE, maquina_modifica = :pMaq
 WHERE id_rol = :pId";
                    cmdU.Parameters.Add(":pNombre", OracleDbType.Varchar2).Value = nombre;
                    cmdU.Parameters.Add(":pVigente", OracleDbType.Int32).Value = vigente;
                    cmdU.Parameters.Add(":pUsr", OracleDbType.Varchar2).Value = AsRequiredDbValue(usuarioAuditoria, "ADMIN", 100);
                    cmdU.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = AsRequiredDbValue(maquinaAuditoria, "LOCAL", 100);
                    cmdU.Parameters.Add(":pId", OracleDbType.Int64).Value = idRol2;
                    
                    int affected;
                    try
                    {
                        affected = await cmdU.ExecuteNonQueryAsync(ct);
                    }
                    catch (OracleException ex) when (ex.Number == 904)
                    {
                        cmdU.CommandText = @"UPDATE ctr_roles
   SET nombre_rol = :pNombre, descripcion = :pNombre, vigente = :pVigente,
       usuario_modifica = :pUsr, fecha_modifica = SYSDATE, maquina_modifica = :pMaq
 WHERE id_rol = :pId";
                        affected = await cmdU.ExecuteNonQueryAsync(ct);
                    }

                    if (affected <= 0) { await tx.RollbackAsync(ct); return new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol para actualizar." }; }
                    await tx.CommitAsync(ct);
                    return new DtoSaveRolResult { idRol = idRol2, message = "Rol actualizado correctamente." };
                }

                long nextId;
                await using (var cmdN = conn.CreateCommand())
                {
                    cmdN.Transaction = tx;
                    cmdN.CommandText = "SELECT NVL(MAX(id_rol), 0) + 1 FROM ctr_roles";
                    nextId = Convert.ToInt64(await cmdN.ExecuteScalarAsync(ct));
                }

                await using (var cmdI = conn.CreateCommand())
                {
                    cmdI.Transaction = tx;
                    cmdI.BindByName = true;
                    cmdI.CommandText = @"INSERT INTO ctr_roles
    (id_rol, codigo_rol, nombre_rol, descripcion, estado, fecha_creacion, usuario_creacion, maquina_creacion)
VALUES (:pId, :pCodigo, :pNombre, :pNombre, :pVigente, SYSDATE, :pUsr, :pMaq)";
                    cmdI.Parameters.Add(":pId", OracleDbType.Int64).Value = nextId;
                    cmdI.Parameters.Add(":pCodigo", OracleDbType.Varchar2).Value = AsRequiredDbValue(nombre.ToUpper().Replace(" ", "_"), "ROL_" + nextId, 50);
                    cmdI.Parameters.Add(":pNombre", OracleDbType.Varchar2).Value = nombre;
                    cmdI.Parameters.Add(":pVigente", OracleDbType.Int32).Value = vigente;
                    cmdI.Parameters.Add(":pUsr", OracleDbType.Varchar2).Value = AsRequiredDbValue(usuarioAuditoria, "ADMIN", 100);
                    cmdI.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = AsRequiredDbValue(maquinaAuditoria, "LOCAL", 100);
                    
                    int inserted;
                    try
                    {
                        inserted = await cmdI.ExecuteNonQueryAsync(ct);
                    }
                    catch (OracleException ex) when (ex.Number == 904)
                    {
                        cmdI.CommandText = @"INSERT INTO ctr_roles
    (id_rol, codigo_rol, nombre_rol, descripcion, vigente, fecha_creacion, usuario_creacion, maquina_creacion)
VALUES (:pId, :pCodigo, :pNombre, :pNombre, :pVigente, SYSDATE, :pUsr, :pMaq)";
                        inserted = await cmdI.ExecuteNonQueryAsync(ct);
                    }

                    if (inserted <= 0) { await tx.RollbackAsync(ct); return new DtoSaveRolResult { idRol = 0, message = "No fue posible crear el rol." }; }
                }

                await tx.CommitAsync(ct);
                return new DtoSaveRolResult { idRol = nextId, message = "Rol creado correctamente." };
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Error Oracle guardando rol en CTR_ROLES: {Message}", ex.Message);
                return new DtoSaveRolResult { idRol = 0, message = $"Error en Oracle: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando rol en CTR_ROLES: {Message}", ex.Message);
                return new DtoSaveRolResult { idRol = 0, message = $"Error: {ex.Message}" };
            }
        }

        public async Task<DtoSaveRolResult> SetRolEstadoAsync(
            long idRol,
            int vigente,
            string usuarioAuditoria,
            string maquinaAuditoria,
            CancellationToken ct)
        {
            var v = vigente == 0 ? 0 : 1;
            var msg = v == 0 ? "Rol inactivado correctamente." : "Rol activado correctamente.";
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            // ── MySQL ────────────────────────────────────────────────────────
            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ctr_roles SET vigente = @pV WHERE id_rol = @pId";
                    cmd.Parameters.AddWithValue("@pV", v);
                    cmd.Parameters.AddWithValue("@pId", idRol);
                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    return affected <= 0
                        ? new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol." }
                        : new DtoSaveRolResult { idRol = idRol, message = msg };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error MySQL SetRolEstado");
                    return new DtoSaveRolResult { idRol = 0, message = $"Error MySQL: {ex.Message}" };
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
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ctr_roles SET vigente = @pV WHERE id_rol = @pId";
                    cmd.Parameters.AddWithValue("@pV", v);
                    cmd.Parameters.AddWithValue("@pId", idRol);
                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    return affected <= 0
                        ? new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol." }
                        : new DtoSaveRolResult { idRol = idRol, message = msg };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error PostgreSQL SetRolEstado");
                    return new DtoSaveRolResult { idRol = 0, message = $"Error PostgreSQL: {ex.Message}" };
                }
            }

            // ── Oracle (default) ─────────────────────────────────────────────
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"UPDATE ctr_roles
   SET estado = :pVigente, usuario_modifica = :pUsr,
       fecha_modifica = SYSDATE, maquina_modifica = :pMaq
 WHERE id_rol = :pId";
                cmd.Parameters.Add(":pVigente", OracleDbType.Int32).Value = v;
                cmd.Parameters.Add(":pUsr", OracleDbType.Varchar2).Value = AsRequiredDbValue(usuarioAuditoria, "ADMIN", 100);
                cmd.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = AsRequiredDbValue(maquinaAuditoria, "LOCAL", 100);
                cmd.Parameters.Add(":pId", OracleDbType.Int64).Value = idRol;

                int affected;
                try
                {
                    affected = await cmd.ExecuteNonQueryAsync(ct);
                }
                catch (OracleException ex) when (ex.Number == 904)
                {
                    cmd.CommandText = @"UPDATE ctr_roles
   SET vigente = :pVigente, usuario_modifica = :pUsr,
       fecha_modifica = SYSDATE, maquina_modifica = :pMaq
 WHERE id_rol = :pId";
                    affected = await cmd.ExecuteNonQueryAsync(ct);
                }

                return affected <= 0
                    ? new DtoSaveRolResult { idRol = 0, message = "No se encontró el rol." }
                    : new DtoSaveRolResult { idRol = idRol, message = msg };
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "Error Oracle SetRolEstado: {Message}", ex.Message);
                return new DtoSaveRolResult { idRol = 0, message = $"Error en Oracle: {ex.Message}" };
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
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, codigo_rol) FROM ctr_roles WHERE COALESCE(estado, vigente, 1) = 1 ORDER BY id_rol";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
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
                    cmd.CommandText = "SELECT id_rol, COALESCE(nombre_rol, descripcion, codigo_rol) FROM ctr_roles WHERE COALESCE(estado, vigente, 1) = 1 ORDER BY id_rol";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
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

            // Oracle
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;

                try
                {
                    cmd.CommandText = "SELECT id_rol, NVL(nombre_rol, NVL(descripcion, codigo_rol)) FROM ctr_roles WHERE NVL(estado, 1) = 1 ORDER BY id_rol";
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = Convert.ToInt32(ToInt64Safe(reader.GetValue(0))),
                            nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                        });
                    }
                }
                catch (OracleException ex) when (ex.Number == 904)
                {
                    cmd.CommandText = "SELECT id_rol, NVL(nombre_rol, NVL(descripcion, codigo_rol)) FROM ctr_roles WHERE NVL(vigente, 1) = 1 ORDER BY id_rol";
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRol
                        {
                            id = Convert.ToInt32(ToInt64Safe(reader.GetValue(0))),
                            nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando roles en Oracle");
            }
            return result;
        }

        public async Task<List<DtoRolAsignado>> GetRolesAsignadosAsync(string usuario, CancellationToken ct)
        {
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
                        SELECT rua.id_rol, COALESCE(r.nombre_rol, r.descripcion, r.codigo_rol, 'Sin nombre'), rua.justificacion, DATE_FORMAT(rua.fecha_fin, '%Y-%m-%d')
                        FROM CTR_ROLES_USER_ADMIN rua
                        JOIN ctr_usuarios u ON u.id_usuario = rua.id_usuario
                        JOIN ctr_roles r ON r.id_rol = rua.id_rol
                        WHERE (u.username = @pUsr OR CAST(u.identificacion AS CHAR) = @pUsr)
                          AND COALESCE(rua.vigente, 1) = 1";
                    cmd.Parameters.AddWithValue("@pUsr", usuario);
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRolAsignado
                        {
                            id = reader.GetInt32(0),
                            rol = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            justificacion = reader.IsDBNull(2) ? null : reader.GetString(2),
                            fechaFin = reader.IsDBNull(3) ? null : reader.GetString(3)
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
                        SELECT rua.id_rol, COALESCE(r.nombre_rol, r.descripcion, r.codigo_rol, 'Sin nombre'), rua.justificacion, TO_CHAR(rua.fecha_fin, 'YYYY-MM-DD')
                        FROM CTR_ROLES_USER_ADMIN rua
                        JOIN ctr_usuarios u ON u.id_usuario = rua.id_usuario
                        JOIN ctr_roles r ON r.id_rol = rua.id_rol
                        WHERE (u.username = @pUsr OR u.identificacion::text = @pUsr)
                          AND COALESCE(rua.vigente, 1) = 1";
                    cmd.Parameters.AddWithValue("@pUsr", usuario);
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoRolAsignado
                        {
                            id = reader.GetInt32(0),
                            rol = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            justificacion = reader.IsDBNull(2) ? null : reader.GetString(2),
                            fechaFin = reader.IsDBNull(3) ? null : reader.GetString(3)
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

            // Oracle
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"
                    SELECT rua.id_rol, NVL(r.nombre_rol, NVL(r.descripcion, r.codigo_rol)), rua.justificacion, TO_CHAR(rua.fecha_fin, 'YYYY-MM-DD')
                    FROM CTR_ROLES_USER_ADMIN rua
                    JOIN ctr_usuarios u ON u.id_usuario = rua.id_usuario
                    JOIN ctr_roles r ON r.id_rol = rua.id_rol
                    WHERE (UPPER(u.username) = UPPER(:pUsr) OR TO_CHAR(u.identificacion) = :pUsr)
                      AND NVL(rua.vigente, 1) = 1";
                cmd.Parameters.Add(":pUsr", OracleDbType.Varchar2).Value = usuario;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoRolAsignado
                    {
                        id = Convert.ToInt32(ToInt64Safe(reader.GetValue(0))),
                        rol = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        justificacion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        fechaFin = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando roles asignados en Oracle");
            }
            return result;
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
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol asignado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public async Task<DtoGuardarUsuarioResult> EditarRolAsync(
            long rolId,
            long idUsuario,
            DtoEditarRolRequest request,
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
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET justificacion = @pJust, fecha_fin = @pFechaFin WHERE id_usuario = @pIdUser AND id_rol = @pIdRol";
                    cmd.Parameters.AddWithValue("@pJust", (object?)request.justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", DateTime.TryParse(request.fechaFin, out var ff) ? ff.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol editado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error editando rol en MySQL");
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
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET justificacion = @pJust, fecha_fin = @pFechaFin WHERE id_usuario = @pIdUser AND id_rol = @pIdRol";
                    cmd.Parameters.AddWithValue("@pJust", (object?)request.justificacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pFechaFin", DateTime.TryParse(request.fechaFin, out var ff) ? ff.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol editado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error editando rol en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            // Oracle
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET justificacion = :pJust, fecha_fin = :pFechaFin WHERE id_usuario = :pIdUser AND id_rol = :pIdRol";
                cmd.Parameters.Add(":pJust", OracleDbType.Varchar2).Value = (object?)request.justificacion ?? DBNull.Value;
                cmd.Parameters.Add(":pFechaFin", OracleDbType.Date).Value = DateTime.TryParse(request.fechaFin, out var ff) ? ff.Date : (object)DBNull.Value;
                cmd.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                cmd.Parameters.Add(":pIdRol", OracleDbType.Int64).Value = rolId;
                await cmd.ExecuteNonQueryAsync(ct);
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol editado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
        }

        public async Task<DtoGuardarUsuarioResult> EliminarRolAsync(
            long rolId,
            long idUsuario,
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
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET vigente = 0 WHERE id_usuario = @pIdUser AND id_rol = @pIdRol";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol eliminado correctamente." };
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
                    cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET vigente = 0 WHERE id_usuario = @pIdUser AND id_rol = @pIdRol";
                    cmd.Parameters.AddWithValue("@pIdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@pIdRol", rolId);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol eliminado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error eliminando rol en PostgreSQL");
                    return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            // Oracle
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = "UPDATE CTR_ROLES_USER_ADMIN SET vigente = 0 WHERE id_usuario = :pIdUser AND id_rol = :pIdRol";
                cmd.Parameters.Add(":pIdUser", OracleDbType.Int64).Value = idUsuario;
                cmd.Parameters.Add(":pIdRol", OracleDbType.Int64).Value = rolId;
                await cmd.ExecuteNonQueryAsync(ct);
                return new DtoGuardarUsuarioResult { idUsuario = idUsuario, message = "Rol eliminado correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando rol en Oracle");
                return new DtoGuardarUsuarioResult { idUsuario = 0, message = $"Error en Oracle: {ex.Message}" };
            }
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

            if (long.TryParse(value?.ToString(), out var parsed))
            {
                return parsed;
            }

            return 0L;
        }
    }
}
