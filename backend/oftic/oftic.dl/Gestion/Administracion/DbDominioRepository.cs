using Comun.Extensions;
using DtoUnidad = Comun.Dtos.DtoUnidad;
using Comun.Dtos.Dominio;
using Comun.Dtos.LineasMando;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datos.Gestion
{
    public class DbDominioRepository: IDbDominioRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbDominioRepository> _logger;

        public DbDominioRepository(IConfiguration configuration, ILogger<DbDominioRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<DtoDominio>> GetAllAsync(CancellationToken ct)
        {
            var result = new List<DtoDominio>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_dominio, descripcion, COALESCE(id_padre, 0), abreviatura, observacion, COALESCE(vigente, 1) FROM CTR_DOMINIOS WHERE COALESCE(vigente, 1) = 1 ORDER BY id_dominio";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoDominio
                        {
                            IdDominio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            Descripcion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            IdPadre = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                            Abreviatura = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Observacion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Vigente = reader.IsDBNull(5) ? 1 : Convert.ToInt32(reader.GetValue(5))
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando dominios en MySQL");
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
                    cmd.CommandText = "SELECT id_dominio, descripcion, COALESCE(id_padre, 0), abreviatura, observacion, COALESCE(vigente, 1) FROM ctr_dominios WHERE COALESCE(vigente, 1) = 1 ORDER BY id_dominio";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoDominio
                        {
                            IdDominio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            Descripcion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            IdPadre = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                            Abreviatura = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Observacion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Vigente = reader.IsDBNull(5) ? 1 : Convert.ToInt32(reader.GetValue(5))
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando dominios en PostgreSQL");
                    return result;
                }
            }

            // Oracle Pure Direct Query (No Stored Procedures)
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var cmdQuery = oracleConn.CreateCommand();
                cmdQuery.BindByName = true;
                cmdQuery.CommandText = "SELECT ID_DOMINIO, DESCRIPCION, NVL(ID_PADRE, 0) AS ID_PADRE, ABREVIATURA, OBSERVACION, NVL(VIGENTE, 1) AS VIGENTE FROM CTR_DOMINIOS WHERE NVL(VIGENTE, 1) = 1 ORDER BY ID_DOMINIO";
                await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(MapDominio(reader));
                }
            }
            catch (Exception exOracle)
            {
                _logger.LogError(exOracle, "Error consultando CTR_DOMINIOS en Oracle.");
            }

            return result;
        }

        public async Task<List<DtoUnidad>> GetUnidadesAsync(CancellationToken ct)
        {
            var unidades = new List<DtoUnidad>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_dominio, descripcion, observacion FROM CTR_DOMINIOS WHERE COALESCE(id_padre, 0) = 0 AND COALESCE(vigente, 1) = 1 ORDER BY descripcion";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        unidades.Add(new DtoUnidad
                        {
                            Value = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Text = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Descripcion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                        });
                    }
                    return unidades;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando unidades en MySQL");
                    return unidades;
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id_dominio, descripcion, observacion FROM ctr_dominios WHERE COALESCE(id_padre, 0) = 0 AND COALESCE(vigente, 1) = 1 ORDER BY descripcion";
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        unidades.Add(new DtoUnidad
                        {
                            Value = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Text = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Descripcion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                        });
                    }
                    return unidades;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando unidades en PostgreSQL");
                    return unidades;
                }
            }

            // Oracle Direct Query
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var cmdQuery = oracleConn.CreateCommand();
                cmdQuery.BindByName = true;
                cmdQuery.CommandText = "SELECT ID_DOMINIO, DESCRIPCION, OBSERVACION FROM CTR_DOMINIOS WHERE NVL(ID_PADRE, 0) = 0 AND NVL(VIGENTE, 1) = 1 ORDER BY DESCRIPCION";
                await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    unidades.Add(new DtoUnidad
                    {
                        Value = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                        Text = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                    });
                }
            }
            catch (Exception exDirect)
            {
                _logger.LogError(exDirect, "Error consultando unidades en CTR_DOMINIOS.");
            }

            return unidades;
        }

        public async Task<DtoDominioResult> CreateAsync(DtoDominioRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            var desc = (request.Descripcion ?? string.Empty).Trim();
            var abrev = (request.Abreviatura ?? string.Empty).Trim();
            var obs = (request.Observacion ?? string.Empty).Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO CTR_DOMINIOS (descripcion, id_padre, abreviatura, observacion, vigente, fecha_creacion, usuario_creacion, maquina_creacion)
                        VALUES (@pDesc, @pIdPadre, @pAbrev, @pObs, 1, NOW(), @pUser, @pMaq);
                        SELECT LAST_INSERT_ID();";
                    cmd.Parameters.AddWithValue("@pDesc", desc);
                    cmd.Parameters.AddWithValue("@pIdPadre", request.IdPadre);
                    cmd.Parameters.AddWithValue("@pAbrev", abrev);
                    cmd.Parameters.AddWithValue("@pObs", obs);
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");

                    var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
                    return new DtoDominioResult { Id = newId, Success = true, Message = "Dominio creado exitosamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando dominio en MySQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en MySQL: {ex.Message}" };
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
                        INSERT INTO ctr_dominios (descripcion, id_padre, abreviatura, observacion, vigente, fecha_creacion, usuario_creacion, maquina_creacion)
                        VALUES (@pDesc, @pIdPadre, @pAbrev, @pObs, 1, CURRENT_TIMESTAMP, @pUser, @pMaq)
                        RETURNING id_dominio;";
                    cmd.Parameters.AddWithValue("@pDesc", desc);
                    cmd.Parameters.AddWithValue("@pIdPadre", request.IdPadre);
                    cmd.Parameters.AddWithValue("@pAbrev", abrev);
                    cmd.Parameters.AddWithValue("@pObs", obs);
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");

                    var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
                    return new DtoDominioResult { Id = newId, Success = true, Message = "Dominio creado exitosamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando dominio en PostgreSQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            // Oracle Pure Direct SQL Insert
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                await using var cmdGetId = oracleConn.CreateCommand();
                cmdGetId.CommandText = "SELECT NVL(MAX(ID_DOMINIO), 0) + 1 FROM CTR_DOMINIOS";
                var nextIdObj = await cmdGetId.ExecuteScalarAsync(ct);
                long newId = Convert.ToInt64(nextIdObj);

                await using var cmdDirect = oracleConn.CreateCommand();
                cmdDirect.BindByName = true;
                cmdDirect.CommandText = @"
                    INSERT INTO CTR_DOMINIOS (ID_DOMINIO, DESCRIPCION, ID_PADRE, ABREVIATURA, OBSERVACION, VIGENTE, FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION)
                    VALUES (:pId, :pDesc, :pIdPadre, :pAbrev, :pObs, 1, SYSDATE, :pUser, :pMaq)";
                cmdDirect.Parameters.Add(":pId", OracleDbType.Int64).Value = newId;
                cmdDirect.Parameters.Add(":pDesc", OracleDbType.Varchar2).Value = desc;
                cmdDirect.Parameters.Add(":pIdPadre", OracleDbType.Int64).Value = request.IdPadre;
                cmdDirect.Parameters.Add(":pAbrev", OracleDbType.Varchar2).Value = abrev;
                cmdDirect.Parameters.Add(":pObs", OracleDbType.Varchar2).Value = obs;
                cmdDirect.Parameters.Add(":pUser", OracleDbType.Int64).Value = usuarioAuditoria;
                cmdDirect.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = maquinaAuditoria ?? "N/A";

                await cmdDirect.ExecuteNonQueryAsync(ct);
                return new DtoDominioResult { Id = newId, Success = true, Message = "Dominio creado exitosamente." };
            }
            catch (Exception exDirect)
            {
                _logger.LogError(exDirect, "Error directo al crear dominio en Oracle CTR_DOMINIOS");
                return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en Oracle: {exDirect.Message}" };
            }
        }

        public async Task<DtoDominioResult> UpdateAsync(long id, DtoDominioRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            var desc = (request.Descripcion ?? string.Empty).Trim();
            var abrev = (request.Abreviatura ?? string.Empty).Trim();
            var obs = (request.Observacion ?? string.Empty).Trim();
            var vigente = request.Vigente;

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE CTR_DOMINIOS 
                           SET descripcion = @pDesc, id_padre = @pIdPadre, abreviatura = @pAbrev, observacion = @pObs, vigente = @pVigente,
                               fecha_modifica = NOW(), usuario_modifica = @pUser, maquina_modifica = @pMaq
                         WHERE id_dominio = @pId;";
                    cmd.Parameters.AddWithValue("@pDesc", desc);
                    cmd.Parameters.AddWithValue("@pIdPadre", request.IdPadre);
                    cmd.Parameters.AddWithValue("@pAbrev", abrev);
                    cmd.Parameters.AddWithValue("@pObs", obs);
                    cmd.Parameters.AddWithValue("@pVigente", vigente);
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@pId", id);

                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoDominioResult { Id = id, Success = true, Message = "Dominio actualizado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando dominio en MySQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en MySQL: {ex.Message}" };
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
                        UPDATE ctr_dominios 
                           SET descripcion = @pDesc, id_padre = @pIdPadre, abreviatura = @pAbrev, observacion = @pObs, vigente = @pVigente,
                               fecha_modifica = CURRENT_TIMESTAMP, usuario_modifica = @pUser, maquina_modifica = @pMaq
                         WHERE id_dominio = @pId;";
                    cmd.Parameters.AddWithValue("@pDesc", desc);
                    cmd.Parameters.AddWithValue("@pIdPadre", request.IdPadre);
                    cmd.Parameters.AddWithValue("@pAbrev", abrev);
                    cmd.Parameters.AddWithValue("@pObs", obs);
                    cmd.Parameters.AddWithValue("@pVigente", vigente);
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@pId", id);

                    await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoDominioResult { Id = id, Success = true, Message = "Dominio actualizado correctamente." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando dominio en PostgreSQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            // Oracle Pure Direct SQL Update
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var cmdDirect = oracleConn.CreateCommand();
                cmdDirect.BindByName = true;
                cmdDirect.CommandText = @"
                    UPDATE CTR_DOMINIOS
                       SET DESCRIPCION = :pDesc, ID_PADRE = :pIdPadre, ABREVIATURA = :pAbrev, OBSERVACION = :pObs, VIGENTE = :pVigente,
                           FECHA_MODIFICA = SYSDATE, USUARIO_MODIFICA = :pUser, MAQUINA_MODIFICA = :pMaq
                     WHERE ID_DOMINIO = :pId";
                cmdDirect.Parameters.Add(":pDesc", OracleDbType.Varchar2).Value = desc;
                cmdDirect.Parameters.Add(":pIdPadre", OracleDbType.Int64).Value = request.IdPadre;
                cmdDirect.Parameters.Add(":pAbrev", OracleDbType.Varchar2).Value = abrev;
                cmdDirect.Parameters.Add(":pObs", OracleDbType.Varchar2).Value = obs;
                cmdDirect.Parameters.Add(":pVigente", OracleDbType.Int32).Value = vigente;
                cmdDirect.Parameters.Add(":pUser", OracleDbType.Int64).Value = usuarioAuditoria;
                cmdDirect.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = maquinaAuditoria ?? "N/A";
                cmdDirect.Parameters.Add(":pId", OracleDbType.Int64).Value = id;

                var affected = await cmdDirect.ExecuteNonQueryAsync(ct);
                return new DtoDominioResult { Id = id, Success = affected > 0, Message = affected > 0 ? "Dominio actualizado correctamente." : "No se encontró el dominio." };
            }
            catch (Exception exDirect)
            {
                _logger.LogError(exDirect, "Error al actualizar dominio en Oracle CTR_DOMINIOS");
                return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en Oracle: {exDirect.Message}" };
            }
        }

        public async Task<DtoDominioResult> DeletelogicalAsync(long id, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE CTR_DOMINIOS SET vigente = 0, fecha_modifica = NOW(), usuario_modifica = @pUser, maquina_modifica = @pMaq WHERE id_dominio = @pId;";
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@pId", id);

                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoDominioResult { Id = id, Success = affected > 0, Message = affected > 0 ? "Dominio retirado exitosamente." : "No se encontró el dominio." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error eliminando dominio en MySQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en MySQL: {ex.Message}" };
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ctr_dominios SET vigente = 0, fecha_modifica = CURRENT_TIMESTAMP, usuario_modifica = @pUser, maquina_modifica = @pMaq WHERE id_dominio = @pId;";
                    cmd.Parameters.AddWithValue("@pUser", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@pMaq", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@pId", id);

                    var affected = await cmd.ExecuteNonQueryAsync(ct);
                    return new DtoDominioResult { Id = id, Success = affected > 0, Message = affected > 0 ? "Dominio retirado exitosamente." : "No se encontró el dominio." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error eliminando dominio en PostgreSQL");
                    return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en PostgreSQL: {ex.Message}" };
                }
            }

            // Oracle Pure Direct SQL Logical Delete
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var cmdDirect = oracleConn.CreateCommand();
                cmdDirect.BindByName = true;
                cmdDirect.CommandText = @"
                    UPDATE CTR_DOMINIOS
                       SET VIGENTE = 0, FECHA_MODIFICA = SYSDATE, USUARIO_MODIFICA = :pUser, MAQUINA_MODIFICA = :pMaq
                     WHERE ID_DOMINIO = :pId";
                cmdDirect.Parameters.Add(":pUser", OracleDbType.Int64).Value = usuarioAuditoria;
                cmdDirect.Parameters.Add(":pMaq", OracleDbType.Varchar2).Value = maquinaAuditoria ?? "N/A";
                cmdDirect.Parameters.Add(":pId", OracleDbType.Int64).Value = id;

                var affected = await cmdDirect.ExecuteNonQueryAsync(ct);
                return new DtoDominioResult { Id = id, Success = affected > 0, Message = affected > 0 ? "Dominio retirado exitosamente." : "No se encontró el dominio." };
            }
            catch (Exception exDirect)
            {
                _logger.LogError(exDirect, "Error directo al retirar dominio en Oracle CTR_DOMINIOS");
                return new DtoDominioResult { Id = 0, Success = false, Message = $"Error en Oracle: {exDirect.Message}" };
            }
        }

        private static DtoDominio MapDominio(OracleDataReader reader)
        {
            var ordId = TryGetOrdinal(reader, "ID_DOMINIO");
            var ordDesc = TryGetOrdinal(reader, "DESCRIPCION");
            var ordPadre = TryGetOrdinal(reader, "ID_PADRE");
            var ordAbrev = TryGetOrdinal(reader, "ABREVIATURA");
            var ordObs = TryGetOrdinal(reader, "OBSERVACION");
            var ordVigente = TryGetOrdinal(reader, "VIGENTE");

            return new DtoDominio
            {
                IdDominio = ordId >= 0 && !reader.IsDBNull(ordId) ? Convert.ToInt64(reader.GetValue(ordId)) : 0,
                Descripcion = ordDesc >= 0 && !reader.IsDBNull(ordDesc) ? reader.GetString(ordDesc) : string.Empty,
                IdPadre = ordPadre >= 0 && !reader.IsDBNull(ordPadre) ? Convert.ToInt64(reader.GetValue(ordPadre)) : 0,
                Abreviatura = ordAbrev >= 0 && !reader.IsDBNull(ordAbrev) ? reader.GetString(ordAbrev) : string.Empty,
                Observacion = ordObs >= 0 && !reader.IsDBNull(ordObs) ? reader.GetString(ordObs) : string.Empty,
                Vigente = ordVigente >= 0 && !reader.IsDBNull(ordVigente) ? Convert.ToInt32(reader.GetValue(ordVigente)) : 1
            };
        }

        private static int TryGetOrdinal(OracleDataReader r, string col)
        {
            for (var i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
