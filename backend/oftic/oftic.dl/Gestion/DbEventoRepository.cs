using Comun.Dtos.Eventos;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion;

public class DbEventoRepository : IDbEventoRepository
{
    private readonly string _cs;
    private readonly ILogger<DbEventoRepository> _logger;

    public DbEventoRepository(IConfiguration configuration, ILogger<DbEventoRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    public async Task<List<DtoEvento>> GetAllAsync(CancellationToken ct)
    {
        var result = new List<DtoEvento>();

        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_GET_EVENTOS";

            cmd.Parameters.Add("P_ID_EVENTO", OracleDbType.Int64).Value = DBNull.Value;
            cmd.Parameters.Add("P_RESULT", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(MapEvento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando todos los eventos");
        }

        return result;
    }

    public async Task<DtoEvento?> GetByIdAsync(long id, CancellationToken ct)
    {
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_GET_EVENTOS";

            cmd.Parameters.Add("P_ID_EVENTO", OracleDbType.Int64).Value = id;
            cmd.Parameters.Add("P_RESULT", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return MapEvento(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando evento {Id}", id);
        }

        return null;
    }

    public async Task<List<DtoEvento>> GetActivosAsync(DateTime? fechaReferencia, CancellationToken ct)
    {
        var result = new List<DtoEvento>();

        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_GET_EVENTOS_ACTIVOS";

            cmd.Parameters.Add("P_FECHA_REFERENCIA", OracleDbType.Date).Value = (object?)fechaReferencia ?? DBNull.Value;
            cmd.Parameters.Add("P_RESULT", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(MapEvento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando eventos activos");
        }

        return result;
    }

    public async Task<DtoEventoResult> CreateAsync(DtoEventoRequest request, string usuario, string maquina, CancellationToken ct)
    {
        var result = new DtoEventoResult();

        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_INS_EVENTO";

            cmd.Parameters.Add("P_UNIDAD", OracleDbType.Varchar2, 100).Value = request.Unidad;
            cmd.Parameters.Add("P_TITULO", OracleDbType.Varchar2, 300).Value = request.Titulo;
            cmd.Parameters.Add("P_SUBTITULO", OracleDbType.Varchar2, 500).Value = (object?)request.Subtitulo ?? DBNull.Value;
            cmd.Parameters.Add("P_DESCRIPCION", OracleDbType.Clob).Value = (object?)request.Descripcion ?? DBNull.Value;
            cmd.Parameters.Add("P_LUGAR", OracleDbType.Varchar2, 200).Value = (object?)request.Lugar ?? DBNull.Value;
            cmd.Parameters.Add("P_CIUDAD", OracleDbType.Varchar2, 100).Value = (object?)request.Ciudad ?? DBNull.Value;
            cmd.Parameters.Add("P_FECHA_INICIO", OracleDbType.Date).Value = request.FechaInicio;
            cmd.Parameters.Add("P_FECHA_FIN", OracleDbType.Date).Value = request.FechaFin;
            cmd.Parameters.Add("P_HORA_INICIO", OracleDbType.Varchar2, 5).Value = (object?)request.HoraInicio ?? DBNull.Value;
            cmd.Parameters.Add("P_HORA_FIN", OracleDbType.Varchar2, 5).Value = (object?)request.HoraFin ?? DBNull.Value;
            cmd.Parameters.Add("P_IMAGEN_PORTADA", OracleDbType.Varchar2, 500).Value = (object?)request.ImagenPortada ?? DBNull.Value;
            cmd.Parameters.Add("P_ENLACE_EVENTO", OracleDbType.Varchar2, 500).Value = (object?)request.EnlaceEvento ?? DBNull.Value;
            cmd.Parameters.Add("P_TIPO_EVENTO", OracleDbType.Varchar2, 50).Value = (object?)request.TipoEvento ?? DBNull.Value;
            cmd.Parameters.Add("P_ORDEN", OracleDbType.Int32).Value = request.Orden;
            cmd.Parameters.Add("P_USUARIO_CREACION", OracleDbType.Varchar2, 100).Value = usuario;
            cmd.Parameters.Add("P_MAQUINA_CREACION", OracleDbType.Varchar2, 100).Value = maquina;

            var pId = cmd.Parameters.Add("P_ID_EVENTO", OracleDbType.Int64);
            pId.Direction = ParameterDirection.Output;

            var pResultado = cmd.Parameters.Add("P_RESULTADO", OracleDbType.Int32);
            pResultado.Direction = ParameterDirection.Output;

            var pMensaje = cmd.Parameters.Add("P_MENSAJE", OracleDbType.Varchar2, 4000);
            pMensaje.Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync(ct);

            result.Id = ToLong(pId.Value);
            result.Success = ToInt(pResultado.Value) == 1;
            result.Message = pMensaje.Value?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error creando evento: {ex.Message}";
            _logger.LogError(ex, "Error creando evento");
        }

        return result;
    }

    public async Task<DtoEventoResult> UpdateAsync(long id, DtoEventoRequest request, string usuario, string maquina, CancellationToken ct)
    {
        var result = new DtoEventoResult();

        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_UPD_EVENTO";

            cmd.Parameters.Add("P_ID_EVENTO", OracleDbType.Int64).Value = id;
            cmd.Parameters.Add("P_UNIDAD", OracleDbType.Varchar2, 100).Value = request.Unidad;
            cmd.Parameters.Add("P_TITULO", OracleDbType.Varchar2, 300).Value = request.Titulo;
            cmd.Parameters.Add("P_SUBTITULO", OracleDbType.Varchar2, 500).Value = (object?)request.Subtitulo ?? DBNull.Value;
            cmd.Parameters.Add("P_DESCRIPCION", OracleDbType.Clob).Value = (object?)request.Descripcion ?? DBNull.Value;
            cmd.Parameters.Add("P_LUGAR", OracleDbType.Varchar2, 200).Value = (object?)request.Lugar ?? DBNull.Value;
            cmd.Parameters.Add("P_CIUDAD", OracleDbType.Varchar2, 100).Value = (object?)request.Ciudad ?? DBNull.Value;
            cmd.Parameters.Add("P_FECHA_INICIO", OracleDbType.Date).Value = request.FechaInicio;
            cmd.Parameters.Add("P_FECHA_FIN", OracleDbType.Date).Value = request.FechaFin;
            cmd.Parameters.Add("P_HORA_INICIO", OracleDbType.Varchar2, 5).Value = (object?)request.HoraInicio ?? DBNull.Value;
            cmd.Parameters.Add("P_HORA_FIN", OracleDbType.Varchar2, 5).Value = (object?)request.HoraFin ?? DBNull.Value;
            cmd.Parameters.Add("P_IMAGEN_PORTADA", OracleDbType.Varchar2, 500).Value = (object?)request.ImagenPortada ?? DBNull.Value;
            cmd.Parameters.Add("P_ENLACE_EVENTO", OracleDbType.Varchar2, 500).Value = (object?)request.EnlaceEvento ?? DBNull.Value;
            cmd.Parameters.Add("P_TIPO_EVENTO", OracleDbType.Varchar2, 50).Value = (object?)request.TipoEvento ?? DBNull.Value;
            cmd.Parameters.Add("P_ORDEN", OracleDbType.Int32).Value = request.Orden;
            cmd.Parameters.Add("P_VIGENTE", OracleDbType.Int32).Value = request.Vigente;
            cmd.Parameters.Add("P_USUARIO_MODIFICA", OracleDbType.Varchar2, 100).Value = usuario;
            cmd.Parameters.Add("P_MAQUINA_MODIFICA", OracleDbType.Varchar2, 100).Value = maquina;

            var pResultado = cmd.Parameters.Add("P_RESULTADO", OracleDbType.Int32);
            pResultado.Direction = ParameterDirection.Output;

            var pMensaje = cmd.Parameters.Add("P_MENSAJE", OracleDbType.Varchar2, 4000);
            pMensaje.Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync(ct);

            result.Id = id;
            result.Success = ToInt(pResultado.Value) == 1;
            result.Message = pMensaje.Value?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error actualizando evento: {ex.Message}";
            _logger.LogError(ex, "Error actualizando evento {Id}", id);
        }

        return result;
    }

    public async Task<DtoEventoResult> DeleteAsync(long id, string usuario, string maquina, CancellationToken ct)
    {
        var result = new DtoEventoResult();

        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_CREATIVOS.P_DEL_EVENTO";

            cmd.Parameters.Add("P_ID_EVENTO", OracleDbType.Int64).Value = id;
            cmd.Parameters.Add("P_USUARIO_MODIFICA", OracleDbType.Varchar2, 100).Value = usuario;
            cmd.Parameters.Add("P_MAQUINA_MODIFICA", OracleDbType.Varchar2, 100).Value = maquina;

            var pResultado = cmd.Parameters.Add("P_RESULTADO", OracleDbType.Int32);
            pResultado.Direction = ParameterDirection.Output;

            var pMensaje = cmd.Parameters.Add("P_MENSAJE", OracleDbType.Varchar2, 4000);
            pMensaje.Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync(ct);

            result.Id = id;
            result.Success = ToInt(pResultado.Value) == 1;
            result.Message = pMensaje.Value?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error eliminando evento: {ex.Message}";
            _logger.LogError(ex, "Error eliminando evento {Id}", id);
        }

        return result;
    }

    private static DtoEvento MapEvento(OracleDataReader r)
    {
        return new DtoEvento
        {
            IdEvento = GetLong(r, "ID_EVENTO"),
            Unidad = GetString(r, "UNIDAD"),
            Titulo = GetString(r, "TITULO"),
            Subtitulo = GetNullableString(r, "SUBTITULO"),
            Descripcion = GetNullableString(r, "DESCRIPCION"),
            Lugar = GetNullableString(r, "LUGAR"),
            Ciudad = GetNullableString(r, "CIUDAD"),
            FechaInicio = GetDate(r, "FECHA_INICIO"),
            FechaFin = GetDate(r, "FECHA_FIN"),
            HoraInicio = GetNullableString(r, "HORA_INICIO"),
            HoraFin = GetNullableString(r, "HORA_FIN"),
            ImagenPortada = GetNullableString(r, "IMAGEN_PORTADA"),
            EnlaceEvento = GetNullableString(r, "ENLACE_EVENTO"),
            TipoEvento = GetNullableString(r, "TIPO_EVENTO"),
            Orden = GetInt(r, "ORDEN"),
            Vigente = GetInt(r, "VIGENTE", 1),
            FechaCreacion = GetDate(r, "FECHA_CREACION", DateTime.MinValue),
            UsuarioCreacion = GetString(r, "USUARIO_CREACION"),
            MaquinaCreacion = GetNullableString(r, "MAQUINA_CREACION"),
            FechaModifica = GetNullableDate(r, "FECHA_MODIFICA"),
            UsuarioModifica = GetNullableString(r, "USUARIO_MODIFICA"),
            MaquinaModifica = GetNullableString(r, "MAQUINA_MODIFICA")
        };
    }

    private static string GetString(OracleDataReader r, string col)
    {
        var idx = r.GetOrdinal(col);
        return r.IsDBNull(idx) ? string.Empty : r.GetString(idx);
    }

    private static string? GetNullableString(OracleDataReader r, string col)
    {
        var idx = r.GetOrdinal(col);
        return r.IsDBNull(idx) ? null : r.GetValue(idx).ToString();
    }

    private static DateTime GetDate(OracleDataReader r, string col, DateTime defaultValue = default)
    {
        var idx = r.GetOrdinal(col);
        return r.IsDBNull(idx) ? defaultValue : r.GetDateTime(idx);
    }

    private static DateTime? GetNullableDate(OracleDataReader r, string col)
    {
        var idx = r.GetOrdinal(col);
        return r.IsDBNull(idx) ? null : r.GetDateTime(idx);
    }

    private static int GetInt(OracleDataReader r, string col, int defaultValue = 0)
    {
        var idx = r.GetOrdinal(col);
        if (r.IsDBNull(idx)) return defaultValue;

        var value = r.GetValue(idx);
        return value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            OracleDecimal od when !od.IsNull => od.ToInt32(),
            _ => defaultValue
        };
    }

    private static long GetLong(OracleDataReader r, string col)
    {
        var idx = r.GetOrdinal(col);
        if (r.IsDBNull(idx)) return 0;

        var value = r.GetValue(idx);
        return value switch
        {
            long l => l,
            int i => i,
            decimal d => (long)d,
            OracleDecimal od when !od.IsNull => od.ToInt64(),
            _ => 0
        };
    }

    private static int ToInt(object? value)
    {
        return value switch
        {
            OracleDecimal od when !od.IsNull => od.ToInt32(),
            decimal d => (int)d,
            int i => i,
            long l => (int)l,
            _ => 0
        };
    }

    private static long ToLong(object? value)
    {
        return value switch
        {
            OracleDecimal od when !od.IsNull => od.ToInt64(),
            decimal d => (long)d,
            int i => i,
            long l => l,
            _ => 0
        };
    }
}
