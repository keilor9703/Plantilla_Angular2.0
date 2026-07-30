using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datos.Interfaz;
using Comun.Dtos.Menu;
using Oracle.ManagedDataAccess.Types;

namespace Datos.Gestion
{
    public class DbMenuRepository : IDbMenuRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly IDbAuditoriaRepository _auditoriaRepo;

        public DbMenuRepository(IConfiguration configuration, IDbAuditoriaRepository auditoriaRepo)
        {
            _cs = Comun.Extensions.ConfigurationExtensions.GetDecryptedConnectionString(configuration);
            _configuration = configuration;
            _auditoriaRepo = auditoriaRepo;
        }


        public async Task<List<DtoMenuItem>> GetMyMenuAsync(long idUsuario, CancellationToken ct)
        {
            var result = new List<DtoMenuItem>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                    FROM CTR_MENU
                    WHERE estado = 1
                    ORDER BY id_padre, orden, id_menu";
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoMenuItem
                    {
                        IdMenu = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                        Descripcion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        IdPadre = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                        Posicion = reader.IsDBNull(3) ? 1 : Convert.ToInt32(reader.GetValue(3)),
                        Tipo = reader.IsDBNull(4) ? "Ruta" : reader.GetString(4),
                        Icono = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6)),
                        Detalle = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
                return result;
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                    FROM ctr_menu
                    WHERE estado = 1
                    ORDER BY id_padre, orden, id_menu";
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoMenuItem
                    {
                        IdMenu = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                        Descripcion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        IdPadre = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                        Posicion = reader.IsDBNull(3) ? 1 : Convert.ToInt32(reader.GetValue(3)),
                        Tipo = reader.IsDBNull(4) ? "Ruta" : reader.GetString(4),
                        Icono = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6)),
                        Detalle = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }
                return result;
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            const string sqlMenuPorRoles = @"
                WITH roles_vigentes AS (
                    SELECT DISTINCT rua.id_rol
                      FROM CTR_ROLES_USER_ADMIN rua
                     WHERE rua.id_usuario = :pIdUsuario
                        AND NVL(rua.vigente, 0) = 1
                ),
                menus_asignados AS (
                    SELECT DISTINCT m.id_menu
                      FROM CTR_MENU m
                     WHERE EXISTS (SELECT 1 FROM roles_vigentes rv WHERE rv.id_rol IN (1, 2))
                    UNION
                    SELECT DISTINCT mr.id_menu
                      FROM CTR_MENU_ROLES mr
                      JOIN roles_vigentes rv ON rv.id_rol = mr.id_rol
                )
                SELECT DISTINCT
                       m.id_menu,
                       m.titulo AS descripcion,
                       m.id_padre AS idpadre,
                       m.orden AS posicion,
                       'Ruta' AS tipo,
                       m.icono,
                       m.estado AS vigente,
                       m.ruta AS detalle
                  FROM ctr_menu m
                 WHERE m.id_menu IN (SELECT ma.id_menu FROM menus_asignados ma)
                 ORDER BY 3, 4, 1";

            try
            {
                await using var cmd = oracleConn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sqlMenuPorRoles;
                cmd.Parameters.Add("pIdUsuario", OracleDbType.Int64).Value = idUsuario;

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(MapMenuItem(reader));
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallback directo a consulta limpia de menú
                result.Clear();
                await using var cmdFallback = oracleConn.CreateCommand();
                cmdFallback.CommandType = CommandType.Text;
                cmdFallback.CommandText = "SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta FROM ctr_menu WHERE estado = 1 ORDER BY id_padre, orden, id_menu";
                await using var readerFallback = await cmdFallback.ExecuteReaderAsync(ct);
                while (await readerFallback.ReadAsync(ct))
                {
                    result.Add(new DtoMenuItem
                    {
                        IdMenu = readerFallback.IsDBNull(0) ? 0 : Convert.ToInt64(readerFallback.GetValue(0)),
                        Descripcion = readerFallback.IsDBNull(1) ? "" : readerFallback.GetString(1),
                        IdPadre = readerFallback.IsDBNull(2) ? 0 : Convert.ToInt64(readerFallback.GetValue(2)),
                        Posicion = readerFallback.IsDBNull(3) ? 1 : Convert.ToInt32(readerFallback.GetValue(3)),
                        Tipo = readerFallback.IsDBNull(4) ? "Ruta" : readerFallback.GetString(4),
                        Icono = readerFallback.IsDBNull(5) ? null : readerFallback.GetString(5),
                        Vigente = readerFallback.IsDBNull(6) ? 1 : Convert.ToInt32(readerFallback.GetValue(6)),
                        Detalle = readerFallback.IsDBNull(7) ? null : readerFallback.GetString(7)
                    });
                }
                return result;
            }
        }

        public async Task<List<DtoMenuItem>> GetAdminMenuAsync(CancellationToken ct)
        {
            var result = new List<DtoMenuItem>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string mysqlSql = @"
                SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                FROM CTR_MENU
                WHERE estado = 1
                ORDER BY id_padre, orden, id_menu";

            const string pgSql = @"
                SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                FROM ctr_menu
                WHERE estado = 1
                ORDER BY id_padre, orden, id_menu";

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = mysqlSql;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    result.Add(MapMenuItemGeneric(reader));
                return result;
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                await using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = pgSql;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    result.Add(MapMenuItemGeneric(reader));
                return result;
            }

            // Oracle — consulta directa con columnas estandarizadas de CTR_MENU
            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);
            await using var oracleCmd = oracleConn.CreateCommand();
            oracleCmd.CommandType = CommandType.Text;
            try
            {
                oracleCmd.CommandText = @"
                    SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                      FROM ctr_menu
                     WHERE estado = 1
                     ORDER BY id_padre, orden, id_menu";
                await using var reader = await oracleCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    result.Add(MapMenuItemGeneric(reader));
            }
            catch
            {
                result.Clear();
                oracleCmd.CommandText = @"
                    SELECT id_menu, titulo, id_padre, orden, 'Ruta' AS tipo, icono, estado, ruta
                      FROM ctr_menu
                     ORDER BY id_padre, orden, id_menu";
                await using var reader = await oracleCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    result.Add(MapMenuItemGeneric(reader));
            }

            return result;
        }

        private static DtoMenuItem MapMenuItemGeneric(System.Data.Common.DbDataReader reader)
        {
            return new DtoMenuItem
            {
                IdMenu = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                Descripcion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                IdPadre = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                Posicion = reader.IsDBNull(3) ? 1 : Convert.ToInt32(reader.GetValue(3)),
                Tipo = reader.IsDBNull(4) ? "Ruta" : reader.GetString(4),
                Icono = reader.IsDBNull(5) ? null : reader.GetString(5),
                Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6)),
                Detalle = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }



        public async Task<DtoMenuResult> SaveMenuAsync(DtoMenuSaveRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var tx = conn.BeginTransaction();
            try
            {
                var isUpdate = request.IdMenu.HasValue && request.IdMenu.Value > 0;
                long idMenu;

                if (isUpdate)
                {
                    idMenu = request.IdMenu!.Value;
                    var idPadre = request.IdPadre <= 0 ? idMenu : request.IdPadre;

                    await using var cmdUpdate = conn.CreateCommand();
                    cmdUpdate.Transaction = tx;
                    cmdUpdate.CommandType = CommandType.Text;
                    cmdUpdate.CommandText = @"
                        UPDATE ctr_menu
                           SET titulo           = :pDescripcion,
                               id_padre         = :pIdPadre,
                               orden            = :pPosicion,
                               icono            = :pIcono,
                               estado           = :pVigente,
                               ruta             = :pDetalle,
                               usuario_modifica = :pUsuarioModifica,
                               fecha_modifica   = SYSDATE,
                               maquina_modifica = :pMaquinaModifica
                         WHERE id_menu = :pIdMenu";

                    BindSaveParams(cmdUpdate, request, idPadre, usuarioAuditoria, maquinaAuditoria);
                    cmdUpdate.Parameters.Add(":pIdMenu", OracleDbType.Int64).Value = idMenu;
                    var rows = await cmdUpdate.ExecuteNonQueryAsync(ct);
                    if (rows <= 0)
                    {
                        await tx.RollbackAsync(ct);
                        return new DtoMenuResult { Id = 0, Message = "No se encontró el menú a actualizar." };
                    }

                    await tx.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new Comun.Dtos.Auditoria.DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "MENU", Accion = "ACTUALIZAR", Registro = $"Menú ID {idMenu}: {request.Descripcion}", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { }
                    return new DtoMenuResult { Id = idMenu, Message = "Menú actualizado correctamente." };
                }

                await using (var cmdNext = conn.CreateCommand())
                {
                    cmdNext.Transaction = tx;
                    cmdNext.CommandType = CommandType.Text;
                    cmdNext.CommandText = "SELECT NVL(MAX(id_menu),0) + 1 FROM ctr_menu";
                    idMenu = Convert.ToInt64(await cmdNext.ExecuteScalarAsync(ct));
                }

                await using (var cmdInsert = conn.CreateCommand())
                {
                    var idPadre = request.IdPadre <= 0 ? idMenu : request.IdPadre;
                    cmdInsert.Transaction = tx;
                    cmdInsert.CommandType = CommandType.Text;
                    cmdInsert.CommandText = @"
                        INSERT INTO ctr_menu
                          (id_menu, titulo, id_padre, orden, icono, estado, ruta,
                           usuario_creacion, fecha_creacion, maquina_creacion)
                        VALUES
                          (:pIdMenu, :pDescripcion, :pIdPadre, :pPosicion, :pIcono, :pVigente, :pDetalle,
                           :pUsuarioCreacion, SYSDATE, :pMaquinaCreacion)";

                    cmdInsert.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                    cmdInsert.Parameters.Add("pDescripcion", OracleDbType.Varchar2).Value = request.Descripcion!.Trim();
                    cmdInsert.Parameters.Add("pIdPadre", OracleDbType.Int64).Value = idPadre;
                    cmdInsert.Parameters.Add("pPosicion", OracleDbType.Int32).Value = request.Posicion;
                    cmdInsert.Parameters.Add("pIcono", OracleDbType.Varchar2).Value = AsDbValue(request.Icono);
                    cmdInsert.Parameters.Add("pVigente", OracleDbType.Int32).Value = request.Vigente is 0 ? 0 : 1;
                    cmdInsert.Parameters.Add("pDetalle", OracleDbType.Varchar2).Value = AsDbValue(request.Detalle);
                    cmdInsert.Parameters.Add("pUsuarioCreacion", OracleDbType.Varchar2).Value = AsDbValue(usuarioAuditoria);
                    cmdInsert.Parameters.Add("pMaquinaCreacion", OracleDbType.Varchar2).Value = AsDbValue(maquinaAuditoria);

                    await cmdInsert.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new Comun.Dtos.Auditoria.DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "MENU", Accion = "CREAR", Registro = $"Menú ID {idMenu}: {request.Descripcion}", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { }
                return new DtoMenuResult { Id = idMenu, Message = "Menú creado correctamente." };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<DtoMenuResult> SetEstadoMenuAsync(long idMenu, int vigente, string usuarioAuditoria, string maquinaAuditoria, string? observaciones = null, CancellationToken ct = default)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            int rows = 0;
            var v = vigente == 0 ? 0 : 1;
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    UPDATE ctr_menu
                       SET estado           = :pVigente,
                           vigente          = :pVigente,
                           usuario_modifica = :pUsuarioModifica,
                           fecha_modifica   = SYSDATE,
                           maquina_modifica = :pMaquinaModifica
                     WHERE id_menu = :pIdMenu";

                cmd.Parameters.Add("pVigente", OracleDbType.Int32).Value = v;
                cmd.Parameters.Add("pUsuarioModifica", OracleDbType.Varchar2).Value = AsDbValue(usuarioAuditoria);
                cmd.Parameters.Add("pMaquinaModifica", OracleDbType.Varchar2).Value = AsDbValue(maquinaAuditoria);
                cmd.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;

                rows = await cmd.ExecuteNonQueryAsync(ct);
            }
            catch
            {
                try
                {
                    await using var cmdFallback = conn.CreateCommand();
                    cmdFallback.CommandType = CommandType.Text;
                    cmdFallback.CommandText = @"UPDATE ctr_menu SET estado = :pVigente WHERE id_menu = :pIdMenu";
                    cmdFallback.Parameters.Add("pVigente", OracleDbType.Int32).Value = v;
                    cmdFallback.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                    rows = await cmdFallback.ExecuteNonQueryAsync(ct);
                }
                catch
                {
                    await using var cmdFallback2 = conn.CreateCommand();
                    cmdFallback2.CommandType = CommandType.Text;
                    cmdFallback2.CommandText = @"UPDATE ctr_menu SET vigente = :pVigente WHERE id_menu = :pIdMenu";
                    cmdFallback2.Parameters.Add("pVigente", OracleDbType.Int32).Value = v;
                    cmdFallback2.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                    rows = await cmdFallback2.ExecuteNonQueryAsync(ct);
                }
            }

            if (rows <= 0)
            {
                return new DtoMenuResult { Id = 0, Message = "No se encontró el menú." };
            }

            try
            {
                await _auditoriaRepo.RegistrarAuditoriaAsync(new Comun.Dtos.Auditoria.DtoAuditoriaRegistrarRequest
                {
                    Usuario = usuarioAuditoria,
                    Modulo = "MENU",
                    Accion = v == 1 ? "ACTIVAR" : "DESACTIVAR",
                    Registro = $"Menú ID {idMenu}: vigente={v}",
                    Resultado = "EXITOSO",
                    Maquina = maquinaAuditoria ?? "LOCAL",
                    Observaciones = observaciones
                }, ct);
            }
            catch { }

            return new DtoMenuResult
            {
                Id = idMenu,
                Message = v == 1 ? "Menú activado." : "Menú desactivado."
            };
        }

        public async Task<List<DtoMenuRolCatalogItem>> GetRolesCatalogAsync(CancellationToken ct)
        {
            var result = new List<DtoMenuRolCatalogItem>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
SELECT r.id_rol, NVL(r.nombre_rol, NVL(r.descripcion, r.codigo_rol)) AS descripcion
  FROM ctr_roles r
 WHERE NVL(r.estado, NVL(r.vigente, 1)) = 1
 ORDER BY NVL(r.nombre_rol, NVL(r.descripcion, r.codigo_rol))";

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoMenuRolCatalogItem
                    {
                        IdRol = Convert.ToInt32(ToInt64Safe(reader.GetValue(0))),
                        Descripcion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                    });
                }
            }
            catch (OracleException ex) when (ex.Number == 904)
            {
                cmd.CommandText = @"
SELECT r.id_rol, NVL(r.descripcion, NVL(r.nombre_rol, r.codigo_rol)) AS descripcion
  FROM ctr_roles r
 WHERE NVL(r.vigente, NVL(r.estado, 1)) = 1
 ORDER BY NVL(r.descripcion, NVL(r.nombre_rol, r.codigo_rol))";
                await using var reader2 = await cmd.ExecuteReaderAsync(ct);
                while (await reader2.ReadAsync(ct))
                {
                    result.Add(new DtoMenuRolCatalogItem
                    {
                        IdRol = Convert.ToInt32(ToInt64Safe(reader2.GetValue(0))),
                        Descripcion = reader2.IsDBNull(1) ? string.Empty : reader2.GetString(1)
                    });
                }
            }

            return result;
        }

        public async Task<List<DtoMenuRolItem>> GetRolesByMenuAsync(long idMenu, CancellationToken ct)
        {
            var result = new List<DtoMenuRolItem>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
SELECT mr.id_rol, NVL(r.nombre_rol, NVL(r.descripcion, 'Rol ' || mr.id_rol)) AS descripcion_rol
  FROM ctr_menu_roles mr
  LEFT JOIN ctr_roles r ON r.id_rol = mr.id_rol
 WHERE mr.id_menu = :pIdMenu
 ORDER BY descripcion_rol";
            cmd.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                long idx = 1;
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoMenuRolItem
                    {
                        IdMenuRol = idx++,
                        IdRol = Convert.ToInt32(ToInt64Safe(reader.GetValue(0))),
                        DescripcionRol = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                    });
                }
            }
            catch (OracleException ex) when (ex.Number == 904)
            {
                cmd.CommandText = @"
SELECT mr.id_rol, NVL(r.descripcion, NVL(r.nombre_rol, 'Rol ' || mr.id_rol)) AS descripcion_rol
  FROM ctr_menu_roles mr
  LEFT JOIN ctr_roles r ON r.id_rol = mr.id_rol
 WHERE mr.id_menu = :pIdMenu
 ORDER BY descripcion_rol";
                await using var reader2 = await cmd.ExecuteReaderAsync(ct);
                long idx = 1;
                while (await reader2.ReadAsync(ct))
                {
                    result.Add(new DtoMenuRolItem
                    {
                        IdMenuRol = idx++,
                        IdRol = Convert.ToInt32(ToInt64Safe(reader2.GetValue(0))),
                        DescripcionRol = reader2.IsDBNull(1) ? string.Empty : reader2.GetString(1)
                    });
                }
            }

            return result;
        }

        public async Task<DtoMenuResult> AssignRolToMenuAsync(long idMenu, int idRol, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var tx = conn.BeginTransaction();
            try
            {
                // Evitar duplicados.
                await using (var cmdExists = conn.CreateCommand())
                {
                    cmdExists.Transaction = tx;
                    cmdExists.CommandType = CommandType.Text;
                    cmdExists.CommandText = @"
SELECT COUNT(1)
  FROM ctr_menu_roles
 WHERE id_menu = :pIdMenu
   AND id_rol = :pIdRol";
                    cmdExists.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                    cmdExists.Parameters.Add("pIdRol", OracleDbType.Int32).Value = idRol;

                    var count = ToInt32Safe(await cmdExists.ExecuteScalarAsync(ct));
                    if (count > 0)
                    {
                        await tx.RollbackAsync(ct);
                        return new DtoMenuResult { Id = 0, Message = "El rol ya está asignado al menú." };
                    }
                }

                long nextId = 1;
                try
                {
                    await using var cmdNext = conn.CreateCommand();
                    cmdNext.Transaction = tx;
                    cmdNext.CommandType = CommandType.Text;
                    cmdNext.CommandText = "SELECT NVL(MAX(id_menu_rol), 0) + 1 FROM ctr_menu_roles";
                    nextId = ToInt64Safe(await cmdNext.ExecuteScalarAsync(ct));
                }
                catch (OracleException ex) when (ex.Number == 904)
                {
                    try
                    {
                        await using var cmdNext2 = conn.CreateCommand();
                        cmdNext2.Transaction = tx;
                        cmdNext2.CommandType = CommandType.Text;
                        cmdNext2.CommandText = "SELECT NVL(MAX(id_menurol), 0) + 1 FROM ctr_menu_roles";
                        nextId = ToInt64Safe(await cmdNext2.ExecuteScalarAsync(ct));
                    }
                    catch { nextId = 1; }
                }

                var usrVal = !string.IsNullOrWhiteSpace(usuarioAuditoria) ? usuarioAuditoria : "ADMIN";
                var inserted = false;
                string[] insertSqls = new[]
                {
                    @"INSERT INTO ctr_menu_roles (id_menu_rol, id_rol, id_menu, estado, usuario_creacion, fecha_creacion) VALUES (:pIdMenuRol, :pIdRol, :pIdMenu, 1, :pUsr, SYSDATE)",
                    @"INSERT INTO ctr_menu_roles (id_menurol, id_rol, id_menu, estado, usuario_creacion, fecha_creacion) VALUES (:pIdMenuRol, :pIdRol, :pIdMenu, 1, :pUsr, SYSDATE)",
                    @"INSERT INTO ctr_menu_roles (id_menu_rol, id_rol, id_menu, estado) VALUES (:pIdMenuRol, :pIdRol, :pIdMenu, 1)",
                    @"INSERT INTO ctr_menu_roles (id_menurol, id_rol, id_menu, estado) VALUES (:pIdMenuRol, :pIdRol, :pIdMenu, 1)",
                    @"INSERT INTO ctr_menu_roles (id_rol, id_menu, estado) VALUES (:pIdRol, :pIdMenu, 1)",
                    @"INSERT INTO ctr_menu_roles (id_rol, id_menu) VALUES (:pIdRol, :pIdMenu)"
                };

                foreach (var sql in insertSqls)
                {
                    try
                    {
                        await using var cmdInsert = conn.CreateCommand();
                        cmdInsert.Transaction = tx;
                        cmdInsert.BindByName = true;
                        cmdInsert.CommandType = CommandType.Text;
                        cmdInsert.CommandText = sql;
                        if (sql.Contains(":pIdMenuRol")) cmdInsert.Parameters.Add("pIdMenuRol", OracleDbType.Int64).Value = nextId;
                        if (sql.Contains(":pIdRol")) cmdInsert.Parameters.Add("pIdRol", OracleDbType.Int32).Value = idRol;
                        if (sql.Contains(":pIdMenu")) cmdInsert.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                        if (sql.Contains(":pUsr")) cmdInsert.Parameters.Add("pUsr", OracleDbType.Varchar2).Value = usrVal;

                        await cmdInsert.ExecuteNonQueryAsync(ct);
                        inserted = true;
                        break;
                    }
                    catch (OracleException ex) when (ex.Number == 904 || ex.Number == 957 || ex.Number == 1400)
                    {
                        // Intentar siguiente variante de consulta SQL
                    }
                }

                if (!inserted)
                {
                    await tx.RollbackAsync(ct);
                    return new DtoMenuResult { Id = 0, Message = "No fue posible asignar el menú al rol." };
                }

                await tx.CommitAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new Comun.Dtos.Auditoria.DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "MENU", Accion = "ASIGNAR ROL", Registro = $"Menú ID {idMenu}: Rol {idRol} asignado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { }
                return new DtoMenuResult { Id = nextId, Message = "Rol asignado al menú correctamente." };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new DtoMenuResult { Id = 0, Message = $"Error asignando menú a rol: {ex.Message}" };
            }
        }

        public async Task<DtoMenuResult> RemoveRolFromMenuAsync(long idMenu, int idRol, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var tx = conn.BeginTransaction();
            try
            {
                await using var cmdDelete = conn.CreateCommand();
                cmdDelete.Transaction = tx;
                cmdDelete.CommandType = CommandType.Text;
                cmdDelete.CommandText = @"
DELETE FROM ctr_menu_roles
 WHERE id_menu = :pIdMenu
   AND id_rol = :pIdRol";
                cmdDelete.Parameters.Add("pIdMenu", OracleDbType.Int64).Value = idMenu;
                cmdDelete.Parameters.Add("pIdRol", OracleDbType.Int32).Value = idRol;

                var rows = await cmdDelete.ExecuteNonQueryAsync(ct);
                if (rows <= 0)
                {
                    await tx.RollbackAsync(ct);
                    return new DtoMenuResult { Id = 0, Message = "No se encontró la asignación menú-rol." };
                }

                await tx.CommitAsync(ct);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new Comun.Dtos.Auditoria.DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria, Modulo = "MENU", Accion = "ELIMINAR ROL", Registro = $"Menú ID {idMenu}: Rol {idRol} retirado", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { }
                return new DtoMenuResult { Id = idMenu, Message = "Asignación menú-rol eliminada." };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new DtoMenuResult { Id = 0, Message = $"Error eliminando asignación: {ex.Message}" };
            }
        }

        public async Task<List<DtoRoleMenuItem>> GetMenusByRolAsync(int idRol, CancellationToken ct)
        {
            var result = new List<DtoRoleMenuItem>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
SELECT m.id_menu,
       m.titulo AS descripcion,
       m.id_padre AS idpadre,
       m.orden AS posicion,
       'Ruta' AS tipo,
       m.ruta AS detalle
  FROM ctr_menu_roles mr
  JOIN ctr_menu m ON m.id_menu = mr.id_menu
 WHERE mr.id_rol = :pIdRol
 ORDER BY m.id_padre, m.orden, m.id_menu";
            cmd.Parameters.Add("pIdRol", OracleDbType.Int32).Value = idRol;

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DtoRoleMenuItem
                    {
                        IdMenu = ToInt64Safe(reader.GetValue(0)),
                        DescripcionMenu = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        IdPadre = ToInt64Safe(reader.GetValue(2)),
                        Posicion = ToInt32Safe(reader.GetValue(3)),
                        Tipo = reader.IsDBNull(4) ? "Ruta" : reader.GetString(4),
                        Detalle = reader.IsDBNull(5) ? null : reader.GetString(5)
                    });
                }
            }
            catch (OracleException ex) when (ex.Number == 904)
            {
                cmd.CommandText = @"
SELECT m.id_menu,
       NVL(m.descripcion, 'Menú ' || m.id_menu) AS descripcion,
       NVL(m.idpadre, 0) AS idpadre,
       1 AS posicion,
       NVL(m.tipo, 'Ruta') AS tipo,
       m.detalle
  FROM ctr_menu_roles mr
  JOIN ctr_menu m ON m.id_menu = mr.id_menu
 WHERE mr.id_rol = :pIdRol
 ORDER BY NVL(m.idpadre, 0), m.id_menu";
                await using var readerFallback = await cmd.ExecuteReaderAsync(ct);
                while (await readerFallback.ReadAsync(ct))
                {
                    result.Add(new DtoRoleMenuItem
                    {
                        IdMenu = ToInt64Safe(readerFallback.GetValue(0)),
                        DescripcionMenu = readerFallback.IsDBNull(1) ? string.Empty : readerFallback.GetString(1),
                        IdPadre = ToInt64Safe(readerFallback.GetValue(2)),
                        Posicion = ToInt32Safe(readerFallback.GetValue(3)),
                        Tipo = readerFallback.IsDBNull(4) ? "Ruta" : readerFallback.GetString(4),
                        Detalle = readerFallback.IsDBNull(5) ? null : readerFallback.GetString(5)
                    });
                }
            }

            return result;
        }

        public Task<DtoMenuResult> AssignMenuToRolAsync(int idRol, long idMenu, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        => AssignRolToMenuAsync(idMenu, idRol, usuarioAuditoria, maquinaAuditoria, ct);

        public Task<DtoMenuResult> RemoveMenuFromRolAsync(int idRol, long idMenu, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        => RemoveRolFromMenuAsync(idMenu, idRol, usuarioAuditoria, maquinaAuditoria, ct);

        private static void BindSaveParams(OracleCommand cmd, DtoMenuSaveRequest request, long idPadre, string usuarioAuditoria, string maquinaAuditoria)
        {
            cmd.Parameters.Add("pDescripcion", OracleDbType.Varchar2).Value = request.Descripcion!.Trim();
            cmd.Parameters.Add("pIdPadre", OracleDbType.Int64).Value = idPadre;
            cmd.Parameters.Add("pPosicion", OracleDbType.Int32).Value = request.Posicion;
            cmd.Parameters.Add("pIcono", OracleDbType.Varchar2).Value = AsDbValue(request.Icono);
            cmd.Parameters.Add("pVigente", OracleDbType.Int32).Value = request.Vigente is 0 ? 0 : 1;
            cmd.Parameters.Add("pDetalle", OracleDbType.Varchar2).Value = AsDbValue(request.Detalle);
            cmd.Parameters.Add("pUsuarioModifica", OracleDbType.Varchar2).Value = AsDbValue(usuarioAuditoria);
            cmd.Parameters.Add("pMaquinaModifica", OracleDbType.Varchar2).Value = AsDbValue(maquinaAuditoria);
        }

        private static object AsDbValue(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? DBNull.Value : normalized;
        }

        private static int ToInt32Safe(object? value)
        {
            if (value is null || value == DBNull.Value)
            {
                return 0;
            }

            if (value is OracleDecimal od)
            {
                return od.IsNull ? 0 : od.ToInt32();
            }

            if (value is int i)
            {
                return i;
            }

            if (value is long l)
            {
                return (int)l;
            }

            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }

        private static long ToInt64Safe(object? value)
        {
            if (value is null || value == DBNull.Value)
            {
                return 0L;
            }

            if (value is OracleDecimal od)
            {
                return od.IsNull ? 0L : od.ToInt64();
            }

            if (value is long l)
            {
                return l;
            }

            if (value is int i)
            {
                return i;
            }

            return long.TryParse(value.ToString(), out var parsed) ? parsed : 0L;
        }

        private static DtoMenuItem MapMenuItem(OracleDataReader reader)
        {
            return new DtoMenuItem
            {
                IdMenu = reader.GetInt64(0),
                Descripcion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                IdPadre = reader.GetInt64(2),
                Posicion = reader.GetInt32(3),
                Tipo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Icono = reader.IsDBNull(5) ? null : reader.GetString(5),
                Vigente = reader.GetInt32(6),
                Detalle = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }
    }
}
