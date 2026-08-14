using Comun.Extensions;
using Comun.Dtos.Auditoria;
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
    public class DbAuditoriaRepository : IDbAuditoriaRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbAuditoriaRepository> _logger;

        public DbAuditoriaRepository(IConfiguration configuration, ILogger<DbAuditoriaRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct)
        {
            var result = new DtoAuditoriaPageResult
            {
                Page = filtro.Page <= 0 ? 1 : filtro.Page,
                PageSize = filtro.PageSize <= 0 ? 10 : filtro.PageSize
            };

            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    int offset = (result.Page - 1) * result.PageSize;

                    var whereList = new List<string>();
                    using var cmdCount = conn.CreateCommand();
                    using var cmdQuery = conn.CreateCommand();

                    if (filtro.FechaDesde.HasValue)
                    {
                        whereList.Add("fecha >= @pFechaDesde");
                        cmdCount.Parameters.AddWithValue("@pFechaDesde", filtro.FechaDesde.Value);
                        cmdQuery.Parameters.AddWithValue("@pFechaDesde", filtro.FechaDesde.Value);
                    }
                    if (filtro.FechaHasta.HasValue)
                    {
                        whereList.Add("fecha <= @pFechaHasta");
                        cmdCount.Parameters.AddWithValue("@pFechaHasta", filtro.FechaHasta.Value);
                        cmdQuery.Parameters.AddWithValue("@pFechaHasta", filtro.FechaHasta.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Usuario))
                    {
                        whereList.Add("LOWER(usuario) LIKE @pUsuario");
                        var uParam = $"%{filtro.Usuario.Trim().ToLower()}%";
                        cmdCount.Parameters.AddWithValue("@pUsuario", uParam);
                        cmdQuery.Parameters.AddWithValue("@pUsuario", uParam);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Modulo))
                    {
                        whereList.Add("UPPER(modulo) LIKE @pModulo");
                        var mParam = $"%{filtro.Modulo.Trim().ToUpper()}%";
                        cmdCount.Parameters.AddWithValue("@pModulo", mParam);
                        cmdQuery.Parameters.AddWithValue("@pModulo", mParam);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Accion))
                    {
                        whereList.Add("UPPER(accion) LIKE @pAccion");
                        var aParam = $"%{filtro.Accion.Trim().ToUpper()}%";
                        cmdCount.Parameters.AddWithValue("@pAccion", aParam);
                        cmdQuery.Parameters.AddWithValue("@pAccion", aParam);
                    }

                    var whereSql = whereList.Count > 0 ? " WHERE " + string.Join(" AND ", whereList) : "";

                    cmdCount.CommandText = $"SELECT COUNT(*) FROM CTR_AUDITORIA{whereSql}";
                    result.Total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

                    cmdQuery.CommandText = $@"
                        SELECT fecha, usuario, modulo, accion, registro, resultado, maquina, observaciones 
                        FROM CTR_AUDITORIA 
                        {whereSql}
                        ORDER BY fecha DESC 
                        LIMIT @pLimit OFFSET @pOffset";
                    cmdQuery.Parameters.AddWithValue("@pLimit", result.PageSize);
                    cmdQuery.Parameters.AddWithValue("@pOffset", offset);

                    using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Items.Add(new DtoAuditoriaItem
                        {
                            Fecha = reader.IsDBNull(0) ? FechaUtility.HoraBogota : reader.GetDateTime(0),
                            Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            Observaciones = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null,
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Aviso consultando auditoria MySQL.");
                    return result;
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    int offset = (result.Page - 1) * result.PageSize;

                    var whereList = new List<string>();
                    using var cmdCount = conn.CreateCommand();
                    using var cmdQuery = conn.CreateCommand();

                    if (filtro.FechaDesde.HasValue)
                    {
                        whereList.Add("fecha >= @pFechaDesde");
                        cmdCount.Parameters.AddWithValue("@pFechaDesde", filtro.FechaDesde.Value);
                        cmdQuery.Parameters.AddWithValue("@pFechaDesde", filtro.FechaDesde.Value);
                    }
                    if (filtro.FechaHasta.HasValue)
                    {
                        whereList.Add("fecha <= @pFechaHasta");
                        cmdCount.Parameters.AddWithValue("@pFechaHasta", filtro.FechaHasta.Value);
                        cmdQuery.Parameters.AddWithValue("@pFechaHasta", filtro.FechaHasta.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Usuario))
                    {
                        whereList.Add("LOWER(usuario) LIKE @pUsuario");
                        var uParam = $"%{filtro.Usuario.Trim().ToLower()}%";
                        cmdCount.Parameters.AddWithValue("@pUsuario", uParam);
                        cmdQuery.Parameters.AddWithValue("@pUsuario", uParam);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Modulo))
                    {
                        whereList.Add("UPPER(modulo) LIKE @pModulo");
                        var mParam = $"%{filtro.Modulo.Trim().ToUpper()}%";
                        cmdCount.Parameters.AddWithValue("@pModulo", mParam);
                        cmdQuery.Parameters.AddWithValue("@pModulo", mParam);
                    }
                    if (!string.IsNullOrWhiteSpace(filtro.Accion))
                    {
                        whereList.Add("UPPER(accion) LIKE @pAccion");
                        var aParam = $"%{filtro.Accion.Trim().ToUpper()}%";
                        cmdCount.Parameters.AddWithValue("@pAccion", aParam);
                        cmdQuery.Parameters.AddWithValue("@pAccion", aParam);
                    }

                    var whereSql = whereList.Count > 0 ? " WHERE " + string.Join(" AND ", whereList) : "";

                    cmdCount.CommandText = $"SELECT COUNT(*) FROM ctr_auditoria{whereSql}";
                    result.Total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

                    cmdQuery.CommandText = $@"
                        SELECT fecha, usuario, modulo, accion, registro, resultado, maquina, observaciones 
                        FROM ctr_auditoria 
                        {whereSql}
                        ORDER BY fecha DESC 
                        LIMIT @pLimit OFFSET @pOffset";
                    cmdQuery.Parameters.AddWithValue("@pLimit", result.PageSize);
                    cmdQuery.Parameters.AddWithValue("@pOffset", offset);

                    using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Items.Add(new DtoAuditoriaItem
                        {
                            Fecha = reader.IsDBNull(0) ? FechaUtility.HoraBogota : reader.GetDateTime(0),
                            Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            Observaciones = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null,
                        });
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Aviso consultando auditoria PostgreSQL.");
                    return result;
                }
            }

            // Oracle Implementation
            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            try
            {
                int offset = (result.Page - 1) * result.PageSize;

                var whereList = new List<string>();
                await using var cmdCount = oracleConn.CreateCommand();
                await using var cmdQuery = oracleConn.CreateCommand();
                cmdCount.BindByName = true;
                cmdQuery.BindByName = true;

                if (filtro.FechaDesde.HasValue)
                {
                    whereList.Add("FECHA >= :pFechaDesde");
                    cmdCount.Parameters.Add(":pFechaDesde", OracleDbType.Date).Value = filtro.FechaDesde.Value;
                    cmdQuery.Parameters.Add(":pFechaDesde", OracleDbType.Date).Value = filtro.FechaDesde.Value;
                }
                if (filtro.FechaHasta.HasValue)
                {
                    whereList.Add("FECHA <= :pFechaHasta");
                    cmdCount.Parameters.Add(":pFechaHasta", OracleDbType.Date).Value = filtro.FechaHasta.Value;
                    cmdQuery.Parameters.Add(":pFechaHasta", OracleDbType.Date).Value = filtro.FechaHasta.Value;
                }
                if (!string.IsNullOrWhiteSpace(filtro.Usuario))
                {
                    whereList.Add("LOWER(USUARIO) LIKE :pUsuario");
                    var uParam = $"%{filtro.Usuario.Trim().ToLower()}%";
                    cmdCount.Parameters.Add(":pUsuario", OracleDbType.Varchar2).Value = uParam;
                    cmdQuery.Parameters.Add(":pUsuario", OracleDbType.Varchar2).Value = uParam;
                }
                if (!string.IsNullOrWhiteSpace(filtro.Modulo))
                {
                    whereList.Add("UPPER(MODULO) LIKE :pModulo");
                    var mParam = $"%{filtro.Modulo.Trim().ToUpper()}%";
                    cmdCount.Parameters.Add(":pModulo", OracleDbType.Varchar2).Value = mParam;
                    cmdQuery.Parameters.Add(":pModulo", OracleDbType.Varchar2).Value = mParam;
                }
                if (!string.IsNullOrWhiteSpace(filtro.Accion))
                {
                    whereList.Add("UPPER(ACCION) LIKE :pAccion");
                    var aParam = $"%{filtro.Accion.Trim().ToUpper()}%";
                    cmdCount.Parameters.Add(":pAccion", OracleDbType.Varchar2).Value = aParam;
                    cmdQuery.Parameters.Add(":pAccion", OracleDbType.Varchar2).Value = aParam;
                }

                var whereSql = whereList.Count > 0 ? " WHERE " + string.Join(" AND ", whereList) : "";

                cmdCount.CommandText = $"SELECT COUNT(*) FROM CTR_AUDITORIA{whereSql}";
                var totalObj = await cmdCount.ExecuteScalarAsync(ct);
                result.Total = totalObj == null || totalObj == DBNull.Value ? 0 : Convert.ToInt32(totalObj);

                cmdQuery.CommandText = $@"
                    SELECT FECHA, USUARIO, MODULO, ACCION, REGISTRO, RESULTADO, MAQUINA, OBSERVACIONES 
                    FROM CTR_AUDITORIA 
                    {whereSql}
                    ORDER BY FECHA DESC 
                    OFFSET :pOffset ROWS FETCH NEXT :pLimit ROWS ONLY";
                cmdQuery.Parameters.Add(":pOffset", OracleDbType.Int32).Value = offset;
                cmdQuery.Parameters.Add(":pLimit", OracleDbType.Int32).Value = result.PageSize;

                await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Items.Add(new DtoAuditoriaItem
                    {
                        Fecha = reader.IsDBNull(0) ? FechaUtility.HoraBogota : reader.GetDateTime(0),
                        Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        Observaciones = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetString(7) : null,
                    });
                }
            }
            catch (Exception exDirect)
            {
                _logger.LogWarning(exDirect, "No fue posible consultar tabla CTR_AUDITORIA.");
            }

            return result;
        }

        private static object AsDbValue(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            value = value.Trim();
            return value.Length > max ? value[..max] : value;
        }

        private static string GetStringSafe(OracleDataReader r, params string[] candidates)
        {
            foreach (var col in candidates)
            {
                var ord = TryGetOrdinal(r, col);
                if (ord >= 0)
                    return r.IsDBNull(ord) ? string.Empty : r.GetValue(ord)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static DateTime GetDateSafe(OracleDataReader r, params string[] candidates)
        {
            foreach (var col in candidates)
            {
                var ord = TryGetOrdinal(r, col);
                if (ord < 0 || r.IsDBNull(ord)) continue;
                var value = r.GetValue(ord);
                if (value is DateTime dt) return dt;
                if (DateTime.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return DateTime.MinValue;
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

        private async Task<string> ResolverUsernameAsync(string usuarioRaw, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(usuarioRaw)) return "SISTEMA";
            var trimmed = usuarioRaw.Trim();
            if (long.TryParse(trimmed, out var idUsuario) && idUsuario > 0)
            {
                try
                {
                    var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();
                    if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new MySqlConnector.MySqlConnection(_cs);
                        await conn.OpenAsync(ct);
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT username FROM CTR_USUARIOS WHERE id_usuario = @id OR identificacion = @rawStr LIMIT 1";
                        cmd.Parameters.AddWithValue("@id", idUsuario);
                        cmd.Parameters.AddWithValue("@rawStr", trimmed);
                        var obj = await cmd.ExecuteScalarAsync(ct);
                        if (obj != null && obj != DBNull.Value && !string.IsNullOrWhiteSpace(obj.ToString()))
                            return obj.ToString()!.Trim();
                    }
                    else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                    {
                        using var conn = new Npgsql.NpgsqlConnection(_cs);
                        await conn.OpenAsync(ct);
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT username FROM ctr_usuarios WHERE id_usuario = @id OR identificacion = @rawStr LIMIT 1";
                        cmd.Parameters.AddWithValue("@id", idUsuario);
                        cmd.Parameters.AddWithValue("@rawStr", trimmed);
                        var obj = await cmd.ExecuteScalarAsync(ct);
                        if (obj != null && obj != DBNull.Value && !string.IsNullOrWhiteSpace(obj.ToString()))
                            return obj.ToString()!.Trim();
                    }
                    else
                    {
                        await using var conn = new OracleConnection(_cs);
                        await conn.OpenAsync(ct);
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT username FROM ctr_usuarios WHERE (id_usuario = :id OR identificacion = :rawStr) AND ROWNUM = 1";
                        cmd.Parameters.Add(":id", OracleDbType.Int64).Value = idUsuario;
                        cmd.Parameters.Add(":rawStr", OracleDbType.Varchar2).Value = trimmed;
                        var obj = await cmd.ExecuteScalarAsync(ct);
                        if (obj != null && obj != DBNull.Value && !string.IsNullOrWhiteSpace(obj.ToString()))
                            return obj.ToString()!.Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo resolver username para id_usuario/identificacion={Id}", idUsuario);
                }
            }
            return trimmed;
        }

        public async Task<bool> RegistrarAuditoriaAsync(DtoAuditoriaRegistrarRequest request, CancellationToken ct = default)
        {
            if (request == null) return false;

            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();
            var usuarioRaw = string.IsNullOrWhiteSpace(request.Usuario) ? "SISTEMA" : request.Usuario.Trim();
            var usuario = await ResolverUsernameAsync(usuarioRaw, ct);
            var modulo = string.IsNullOrWhiteSpace(request.Modulo) ? "GENERAL" : request.Modulo.Trim();
            var accion = string.IsNullOrWhiteSpace(request.Accion) ? "ACCION" : request.Accion.Trim();
            var registro = string.IsNullOrWhiteSpace(request.Registro) ? "-" : request.Registro.Trim();
            var resultado = string.IsNullOrWhiteSpace(request.Resultado) ? "EXITOSO" : request.Resultado.Trim();
            var maquina = string.IsNullOrWhiteSpace(request.Maquina) ? "LOCAL" : request.Maquina.Trim();
            var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
            var fechaBogota = FechaUtility.HoraBogota;

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO CTR_AUDITORIA (fecha, usuario, modulo, accion, registro, resultado, maquina, observaciones)
                        VALUES (@f, @u, @m, @a, @r, @res, @ip, @obs)";
                    cmd.Parameters.AddWithValue("@f", fechaBogota);
                    cmd.Parameters.AddWithValue("@u", usuario);
                    cmd.Parameters.AddWithValue("@m", modulo);
                    cmd.Parameters.AddWithValue("@a", accion);
                    cmd.Parameters.AddWithValue("@r", registro);
                    cmd.Parameters.AddWithValue("@res", resultado);
                    cmd.Parameters.AddWithValue("@ip", maquina);
                    cmd.Parameters.AddWithValue("@obs", (object?)observaciones ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return true;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO ctr_auditoria (fecha, usuario, modulo, accion, registro, resultado, maquina, observaciones)
                        VALUES (@f, @u, @m, @a, @r, @res, @ip, @obs)";
                    cmd.Parameters.AddWithValue("@f", fechaBogota);
                    cmd.Parameters.AddWithValue("@u", usuario);
                    cmd.Parameters.AddWithValue("@m", modulo);
                    cmd.Parameters.AddWithValue("@a", accion);
                    cmd.Parameters.AddWithValue("@r", registro);
                    cmd.Parameters.AddWithValue("@res", resultado);
                    cmd.Parameters.AddWithValue("@ip", maquina);
                    cmd.Parameters.AddWithValue("@obs", (object?)observaciones ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(ct);
                    return true;
                }

                // Oracle implementation using sequence SEQ_CTR_AUDITORIA.NEXTVAL
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);
                await using var cmdOracle = oracleConn.CreateCommand();
                cmdOracle.BindByName = true;
                cmdOracle.CommandText = @"
                    INSERT INTO CTR_AUDITORIA (ID_AUDITORIA, FECHA, USUARIO, MODULO, ACCION, REGISTRO, RESULTADO, MAQUINA, OBSERVACIONES)
                    VALUES (SEQ_CTR_AUDITORIA.NEXTVAL, :f, :u, :m, :a, :r, :res, :ip, :obs)";
                cmdOracle.Parameters.Add(":f", OracleDbType.Date).Value = fechaBogota;
                cmdOracle.Parameters.Add(":u", OracleDbType.Varchar2).Value = usuario;
                cmdOracle.Parameters.Add(":m", OracleDbType.Varchar2).Value = modulo;
                cmdOracle.Parameters.Add(":a", OracleDbType.Varchar2).Value = accion;
                cmdOracle.Parameters.Add(":r", OracleDbType.Varchar2).Value = registro;
                cmdOracle.Parameters.Add(":res", OracleDbType.Varchar2).Value = resultado;
                cmdOracle.Parameters.Add(":ip", OracleDbType.Varchar2).Value = maquina;
                cmdOracle.Parameters.Add(":obs", OracleDbType.Varchar2).Value = (object?)observaciones ?? DBNull.Value;
                await cmdOracle.ExecuteNonQueryAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error registrando auditoría en base de datos para módulo {Modulo}, acción {Accion}", modulo, accion);
                return false;
            }
        }

        public bool RegistrarAuditoria(DtoAuditoriaRegistrarRequest request)
        {
            return RegistrarAuditoriaAsync(request).GetAwaiter().GetResult();
        }
    }
}
