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

            cmd.Parameters.Add("P_NOMBRE_EVENTO", OracleDbType.Varchar2, 300).Value = request.NombreEvento;
            cmd.Parameters.Add("P_UNIDAD", OracleDbType.Varchar2, 150).Value = request.Unidad;
            cmd.Parameters.Add("P_FECHA_INICIO", OracleDbType.Date).Value = request.FechaInicio;
            cmd.Parameters.Add("P_FECHA_FIN", OracleDbType.Date).Value = request.FechaFin;
            cmd.Parameters.Add("P_DEPARTAMENTO", OracleDbType.Varchar2, 100).Value = request.Departamento;
            cmd.Parameters.Add("P_CIUDAD", OracleDbType.Varchar2, 100).Value = request.Ciudad;
            cmd.Parameters.Add("P_DIRECCION_EVENTO", OracleDbType.Varchar2, 220).Value = request.DireccionEvento;
            cmd.Parameters.Add("P_CEDULA_RESPONSABLE", OracleDbType.Varchar2, 20).Value = request.CedulaResponsable;
            cmd.Parameters.Add("P_NOMBRE_RESPONSABLE", OracleDbType.Varchar2, 200).Value = request.NombreResponsable;
            cmd.Parameters.Add("P_CORREO_RESPONSABLE", OracleDbType.Varchar2, 150).Value = (object?)request.CorreoResponsable ?? DBNull.Value;
            cmd.Parameters.Add("P_TELEFONO_RESPONSABLE", OracleDbType.Varchar2, 30).Value = (object?)request.TelefonoResponsable ?? DBNull.Value;
            cmd.Parameters.Add("P_IMAGEN_EVENTO", OracleDbType.Varchar2, 500).Value = (object?)request.ImagenEvento ?? DBNull.Value;
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
            cmd.Parameters.Add("P_NOMBRE_EVENTO", OracleDbType.Varchar2, 300).Value = request.NombreEvento;
            cmd.Parameters.Add("P_UNIDAD", OracleDbType.Varchar2, 150).Value = request.Unidad;
            cmd.Parameters.Add("P_FECHA_INICIO", OracleDbType.Date).Value = request.FechaInicio;
            cmd.Parameters.Add("P_FECHA_FIN", OracleDbType.Date).Value = request.FechaFin;
            cmd.Parameters.Add("P_DEPARTAMENTO", OracleDbType.Varchar2, 100).Value = request.Departamento;
            cmd.Parameters.Add("P_CIUDAD", OracleDbType.Varchar2, 100).Value = request.Ciudad;
            cmd.Parameters.Add("P_DIRECCION_EVENTO", OracleDbType.Varchar2, 220).Value = request.DireccionEvento;
            cmd.Parameters.Add("P_CEDULA_RESPONSABLE", OracleDbType.Varchar2, 20).Value = request.CedulaResponsable;
            cmd.Parameters.Add("P_NOMBRE_RESPONSABLE", OracleDbType.Varchar2, 200).Value = request.NombreResponsable;
            cmd.Parameters.Add("P_CORREO_RESPONSABLE", OracleDbType.Varchar2, 150).Value = (object?)request.CorreoResponsable ?? DBNull.Value;
            cmd.Parameters.Add("P_TELEFONO_RESPONSABLE", OracleDbType.Varchar2, 30).Value = (object?)request.TelefonoResponsable ?? DBNull.Value;
            cmd.Parameters.Add("P_IMAGEN_EVENTO", OracleDbType.Varchar2, 500).Value = (object?)request.ImagenEvento ?? DBNull.Value;
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
            NombreEvento = GetString(r, "NOMBRE_EVENTO"),
            Unidad = GetString(r, "UNIDAD"),
            FechaInicio = GetDate(r, "FECHA_INICIO"),
            FechaFin = GetDate(r, "FECHA_FIN"),
            Departamento = GetString(r, "DEPARTAMENTO"),
            Ciudad = GetString(r, "CIUDAD"),
            DireccionEvento = GetString(r, "DIRECCION_EVENTO"),
            CedulaResponsable = GetString(r, "CEDULA_RESPONSABLE"),
            NombreResponsable = GetString(r, "NOMBRE_RESPONSABLE"),
            CorreoResponsable = GetNullableString(r, "CORREO_RESPONSABLE"),
            TelefonoResponsable = GetNullableString(r, "TELEFONO_RESPONSABLE"),
            ImagenEvento = GetNullableString(r, "IMAGEN_EVENTO"),
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
