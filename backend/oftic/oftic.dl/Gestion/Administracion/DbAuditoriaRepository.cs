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

                    using var cmdCount = conn.CreateCommand();
                    cmdCount.CommandText = "SELECT COUNT(*) FROM CTR_AUDITORIA";
                    result.Total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT fecha, usuario, modulo, accion, registro, resultado, maquina 
                        FROM CTR_AUDITORIA 
                        ORDER BY fecha DESC 
                        LIMIT @pLimit OFFSET @pOffset";
                    cmd.Parameters.AddWithValue("@pLimit", result.PageSize);
                    cmd.Parameters.AddWithValue("@pOffset", offset);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Items.Add(new DtoAuditoriaItem
                        {
                            Fecha = reader.IsDBNull(0) ? DateTime.Now : reader.GetDateTime(0),
                            Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
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

                    using var cmdCount = conn.CreateCommand();
                    cmdCount.CommandText = "SELECT COUNT(*) FROM ctr_auditoria";
                    result.Total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(ct));

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT fecha, usuario, modulo, accion, registro, resultado, maquina 
                        FROM ctr_auditoria 
                        ORDER BY fecha DESC 
                        LIMIT @pLimit OFFSET @pOffset";
                    cmd.Parameters.AddWithValue("@pLimit", result.PageSize);
                    cmd.Parameters.AddWithValue("@pOffset", offset);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Items.Add(new DtoAuditoriaItem
                        {
                            Fecha = reader.IsDBNull(0) ? DateTime.Now : reader.GetDateTime(0),
                            Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
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
                await LoadWithSignatureAsync(oracleConn, filtro, result, ct);
                return result;
            }
            catch (Exception exSp)
            {
                _logger.LogWarning(exSp, "Procedimiento PK_AUDITORIA.P_GET_BITACORA_GENERAL no disponible. Usando consulta SQL directa a CTR_AUDITORIA.");
            }

            // Oracle Direct SQL Fallback
            try
            {
                int offset = (result.Page - 1) * result.PageSize;

                await using var cmdCount = oracleConn.CreateCommand();
                cmdCount.CommandText = "SELECT COUNT(*) FROM CTR_AUDITORIA";
                var totalObj = await cmdCount.ExecuteScalarAsync(ct);
                result.Total = totalObj == null || totalObj == DBNull.Value ? 0 : Convert.ToInt32(totalObj);

                await using var cmdQuery = oracleConn.CreateCommand();
                cmdQuery.BindByName = true;
                cmdQuery.CommandText = @"
                    SELECT FECHA, USUARIO, MODULO, ACCION, REGISTRO, RESULTADO, MAQUINA 
                    FROM CTR_AUDITORIA 
                    ORDER BY FECHA DESC 
                    OFFSET :pOffset ROWS FETCH NEXT :pLimit ROWS ONLY";
                cmdQuery.Parameters.Add(":pOffset", OracleDbType.Int32).Value = offset;
                cmdQuery.Parameters.Add(":pLimit", OracleDbType.Int32).Value = result.PageSize;

                await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Items.Add(new DtoAuditoriaItem
                    {
                        Fecha = reader.IsDBNull(0) ? DateTime.Now : reader.GetDateTime(0),
                        Usuario = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Modulo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Accion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Registro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Resultado = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Maquina = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    });
                }
            }
            catch (Exception exDirect)
            {
                _logger.LogWarning(exDirect, "No fue posible consultar tabla CTR_AUDITORIA.");
            }

            return result;
        }

        private async Task LoadWithSignatureAsync(OracleConnection conn, DtoAuditoriaFiltroRequest filtro, DtoAuditoriaPageResult result, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandTimeout = 60;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_AUDITORIA.P_GET_BITACORA_GENERAL";

            cmd.Parameters.Add("p_fecha_desde", OracleDbType.Date).Value = (object?)filtro.FechaDesde ?? DBNull.Value;
            cmd.Parameters.Add("p_fecha_hasta", OracleDbType.Date).Value = (object?)filtro.FechaHasta ?? DBNull.Value;
            cmd.Parameters.Add("p_usuario", OracleDbType.Varchar2).Value = AsDbValue(filtro.Usuario, 100);
            cmd.Parameters.Add("p_modulo", OracleDbType.Varchar2).Value = AsDbValue(filtro.Modulo, 100);
            cmd.Parameters.Add("p_accion", OracleDbType.Varchar2).Value = AsDbValue(filtro.Accion, 50);
            cmd.Parameters.Add("p_page", OracleDbType.Int32).Value = filtro.Page;
            cmd.Parameters.Add("p_page_size", OracleDbType.Int32).Value = filtro.PageSize;

            var pTotal = new OracleParameter("p_total", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pTotal);
            cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await FillItemsAsync(cmd, result, ct);

            if (pTotal.Value is OracleDecimal dec && !dec.IsNull)
                result.Total = dec.ToInt32();
            else
                result.Total = result.Items.Count;
        }

        private async Task FillItemsAsync(OracleCommand cmd, DtoAuditoriaPageResult result, CancellationToken ct)
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Items.Add(new DtoAuditoriaItem
                {
                    Fecha = GetDateSafe(reader, "FECHA", "FECHA_CREACION", "FECHA_MODIFICA"),
                    Usuario = GetStringSafe(reader, "USUARIO", "USUARIO_CREACION", "USUARIO_MODIFICA"),
                    Modulo = GetStringSafe(reader, "MODULO", "EVENTO", "ID_PROCESO"),
                    Accion = GetStringSafe(reader, "ACCION", "EVENTO", "ID_PROCESO"),
                    Registro = GetStringSafe(reader, "REGISTRO", "DESCRIPCION"),
                    Resultado = GetStringSafe(reader, "RESULTADO"),
                    Maquina = GetStringSafe(reader, "MAQUINA", "MAQUINA_CREACION", "MAQUINA_MODIFICA"),
                });
            }
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
    }
}
