using Comun.Dtos.Videos;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion
{
    public class DbVideoRepository : IDbVideoRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbVideoRepository> _logger;

        public DbVideoRepository(IConfiguration configuration, ILogger<DbVideoRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<DtoVideo>> GetAllAsync(CancellationToken ct)
        {
            var result = new List<DtoVideo>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_video, descripcion, observaciones, ruta_video, fecha_creacion, usuario_creacion, vigente
                FROM ctr_videos
                WHERE vigente = 1
                ORDER BY id_video DESC";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapVideoGeneric(reader));
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapVideoGeneric(reader));
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                try
                {
                    await using var cmd = oracleConn.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PK_ADMINISTRACION.FN_CONSULTAR_VIDEO";
                    cmd.Parameters.Add("in_id_video", OracleDbType.Int64).Value = DBNull.Value;
                    var cursorParam = cmd.Parameters.Add("cur_datos", OracleDbType.RefCursor);
                    cursorParam.Direction = ParameterDirection.ReturnValue;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapVideo(reader));
                }
                catch
                {
                    result.Clear();
                    await using var cmdQuery = oracleConn.CreateCommand();
                    cmdQuery.CommandText = selectSql;
                    await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapVideoGeneric(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar videos en BD. Se retorna lista vacía.");
            }

            return result;
        }

        public async Task<DtoVideo?> GetByIdAsync(long id, CancellationToken ct)
        {
            var list = await GetAllAsync(ct);
            return list.FirstOrDefault(x => x.IdVideo == id);
        }

        public async Task<DtoVideoResult> CreateAsync(DtoVideoRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoVideoResult();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO ctr_videos (descripcion, observaciones, usuario_creacion, ruta_video, vigente, fecha_creacion)
                        VALUES (@d, @o, @u, @r, 1, NOW())";
                    cmd.Parameters.AddWithValue("@d", request.Descripcion ?? string.Empty);
                    cmd.Parameters.AddWithValue("@o", (object?)request.Observaciones ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@u", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@r", (object?)request.RutaVideo ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(ct);

                    result.Success = true;
                    result.Message = "Video creado exitosamente";
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO ctr_videos (descripcion, observaciones, usuario_creacion, ruta_video, vigente, fecha_creacion)
                        VALUES (@d, @o, @u, @r, 1, CURRENT_TIMESTAMP)";
                    cmd.Parameters.AddWithValue("@d", request.Descripcion ?? string.Empty);
                    cmd.Parameters.AddWithValue("@o", (object?)request.Observaciones ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@u", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@r", (object?)request.RutaVideo ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(ct);

                    result.Success = true;
                    result.Message = "Video creado exitosamente";
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var oracleCmd = oracleConn.CreateCommand();
                oracleCmd.CommandText = @"
                    INSERT INTO CTR_VIDEOS (DESCRIPCION, OBSERVACIONES, USUARIO_CREACION, RUTA_VIDEO, VIGENTE, FECHA_CREACION)
                    VALUES (:d, :o, :u, :r, 1, SYSDATE)";
                oracleCmd.Parameters.Add(":d", OracleDbType.Varchar2).Value = request.Descripcion ?? string.Empty;
                oracleCmd.Parameters.Add(":o", OracleDbType.Varchar2).Value = (object?)request.Observaciones ?? DBNull.Value;
                oracleCmd.Parameters.Add(":u", OracleDbType.Varchar2).Value = usuarioAuditoria;
                oracleCmd.Parameters.Add(":r", OracleDbType.Varchar2).Value = (object?)request.RutaVideo ?? DBNull.Value;
                await oracleCmd.ExecuteNonQueryAsync(ct);

                result.Success = true;
                result.Message = "Video creado exitosamente";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error en CreateAsync Video");
            }

            return result;
        }

        public async Task<DtoVideoResult> UpdateAsync(long id, DtoVideoRequest request, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoVideoResult();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE ctr_videos
                        SET descripcion = @d, observaciones = @o, usuario_modifica = @u, maquina_modifica = @m, ruta_video = COALESCE(NULLIF(@r, ''), ruta_video), vigente = @v, fecha_modifica = NOW()
                        WHERE id_video = @id";
                    cmd.Parameters.AddWithValue("@d", request.Descripcion ?? string.Empty);
                    cmd.Parameters.AddWithValue("@o", (object?)request.Observaciones ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@u", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@m", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@r", (object?)request.RutaVideo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@v", request.Vigente);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync(ct);

                    result.Success = true;
                    result.Message = "Video actualizado exitosamente";
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE ctr_videos
                        SET descripcion = @d, observaciones = @o, usuario_modifica = @u, maquina_modifica = @m, ruta_video = COALESCE(NULLIF(@r, ''), ruta_video), vigente = @v, fecha_modifica = CURRENT_TIMESTAMP
                        WHERE id_video = @id";
                    cmd.Parameters.AddWithValue("@d", request.Descripcion ?? string.Empty);
                    cmd.Parameters.AddWithValue("@o", (object?)request.Observaciones ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@u", usuarioAuditoria);
                    cmd.Parameters.AddWithValue("@m", maquinaAuditoria ?? "N/A");
                    cmd.Parameters.AddWithValue("@r", (object?)request.RutaVideo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@v", request.Vigente);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync(ct);

                    result.Success = true;
                    result.Message = "Video actualizado exitosamente";
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var oracleCmd = oracleConn.CreateCommand();
                oracleCmd.CommandText = @"
                    UPDATE CTR_VIDEOS
                    SET DESCRIPCION = :d, OBSERVACIONES = :o, USUARIO_MODIFICA = :u, MAQUINA_MODIFICA = :m, RUTA_VIDEO = COALESCE(:r, RUTA_VIDEO), VIGENTE = :v, FECHA_MODIFICA = SYSDATE
                    WHERE ID_VIDEO = :id";
                oracleCmd.Parameters.Add(":d", OracleDbType.Varchar2).Value = request.Descripcion ?? string.Empty;
                oracleCmd.Parameters.Add(":o", OracleDbType.Varchar2).Value = (object?)request.Observaciones ?? DBNull.Value;
                oracleCmd.Parameters.Add(":u", OracleDbType.Varchar2).Value = usuarioAuditoria;
                oracleCmd.Parameters.Add(":m", OracleDbType.Varchar2).Value = maquinaAuditoria ?? "N/A";
                oracleCmd.Parameters.Add(":r", OracleDbType.Varchar2).Value = (object?)request.RutaVideo ?? DBNull.Value;
                oracleCmd.Parameters.Add(":v", OracleDbType.Int64).Value = request.Vigente;
                oracleCmd.Parameters.Add(":id", OracleDbType.Int64).Value = id;
                await oracleCmd.ExecuteNonQueryAsync(ct);

                result.Success = true;
                result.Message = "Video actualizado exitosamente";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error en UpdateAsync Video");
            }

            return result;
        }

        public async Task<DtoVideoResult> DeleteAsync(long id, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            return await UpdateAsync(id, new DtoVideoRequest { Vigente = 0 }, usuarioAuditoria, maquinaAuditoria, ct);
        }

        private static DtoVideo MapVideoGeneric(System.Data.Common.DbDataReader reader)
        {
            return new DtoVideo
            {
                IdVideo = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                Descripcion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Observaciones = reader.IsDBNull(2) ? null : reader.GetString(2),
                RutaVideo = reader.IsDBNull(3) ? null : reader.GetString(3),
                FechaCreacion = reader.IsDBNull(4) ? DateTime.Now : Convert.ToDateTime(reader.GetValue(4)),
                UsuarioCreacion = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt64(reader.GetValue(6))
            };
        }

        private static DtoVideo MapVideo(OracleDataReader reader)
        {
            return new DtoVideo
            {
                IdVideo = reader.GetInt64(reader.GetOrdinal("ID_VIDEO")),
                Descripcion = reader.GetString(reader.GetOrdinal("DESCRIPCION")),
                Observaciones = reader.IsDBNull(reader.GetOrdinal("OBSERVACIONES")) ? null : reader.GetString(reader.GetOrdinal("OBSERVACIONES")),
                RutaVideo = reader.IsDBNull(reader.GetOrdinal("RUTA_VIDEO")) ? null : reader.GetString(reader.GetOrdinal("RUTA_VIDEO")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FECHA_CREACION")),
                UsuarioCreacion = reader.GetString(reader.GetOrdinal("USUARIO_CREACION")),
                Vigente = reader.GetInt64(reader.GetOrdinal("VIGENTE")),
                FechaModifica = reader.IsDBNull(reader.GetOrdinal("FECHA_MODIFICA")) ? null : reader.GetDateTime(reader.GetOrdinal("FECHA_MODIFICA")),
                UsuarioModifica = reader.IsDBNull(reader.GetOrdinal("USUARIO_MODIFICA")) ? null : reader.GetString(reader.GetOrdinal("USUARIO_MODIFICA")),
                MaquinaModifica = reader.IsDBNull(reader.GetOrdinal("MAQUINA_MODIFICA")) ? null : reader.GetString(reader.GetOrdinal("MAQUINA_MODIFICA"))
            };
        }
    }
}
