using Comun.Dtos.Auditoria;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion
{
    public class DbAuditoriaRepository : IDbAuditoriaRepository
    {
        private readonly string _cs;
        private readonly ILogger<DbAuditoriaRepository> _logger;

        public DbAuditoriaRepository(IConfiguration configuration, ILogger<DbAuditoriaRepository> logger)
        {
            _cs = configuration.GetConnectionString("DbConnection")!;
            _logger = logger;
        }

        public async Task<DtoAuditoriaPageResult> GetBitacoraAsync(DtoAuditoriaFiltroRequest filtro, CancellationToken ct)
        {
            var result = new DtoAuditoriaPageResult
            {
                Page = filtro.Page,
                PageSize = filtro.PageSize
            };

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await LoadWithSignatureAsync(conn, filtro, result, ct);

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
