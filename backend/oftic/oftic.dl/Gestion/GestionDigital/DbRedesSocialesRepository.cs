using Comun.Dtos;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Oracle.ManagedDataAccess.Types;

namespace Datos.Gestion;

public class DbRedesSocialesRepository : IDbRedesSocialesRepository
{
    private readonly string _cs;
    private readonly ILogger<DbRedesSocialesRepository> _logger;

    public DbRedesSocialesRepository(IConfiguration configuration, ILogger<DbRedesSocialesRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }


    public async Task<IEnumerable<DtoRedesSociales>> GetAllAsync()
    {
        var redesSociales = new List<DtoRedesSociales>();

        try
        {
            using (var connection = new OracleConnection(_cs))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand("PK_APIS.P_GET_REDES_SOCIALES", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value = DBNull.Value;
                    command.Parameters.Add("P_Result", OracleDbType.RefCursor, ParameterDirection.Output);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            redesSociales.Add(MapRedSocial(reader));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las redes sociales");
            throw;
        }

        return redesSociales;
    }

    public async Task<DtoRedesSociales?> GetByIdAsync(int id)
    {
        DtoRedesSociales? redesSociales = null;

        try
        {
            using (var connection = new OracleConnection(_cs))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand("PK_APIS.P_GET_REDES_SOCIALES", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value = id;
                    command.Parameters.Add("P_Result", OracleDbType.RefCursor, ParameterDirection.Output);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            redesSociales = MapRedSocial(reader);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener red social por ID: {Id}", id);
            throw;
        }

        return redesSociales;
    }

    public async Task<DtoRedesSocialesResult> CreateAsync(DtoRedesSociales redesSociales, string usuario)
    {
        return await ExecuteInsUpdAsync(redesSociales, usuario, isUpdate: false);
    }

    public async Task<DtoRedesSocialesResult> UpdateAsync(DtoRedesSociales redesSociales, string usuario)
    {
        return await ExecuteInsUpdAsync(redesSociales, usuario, isUpdate: true);
    }

    public async Task<DtoRedesSocialesResult> DeleteAsync(int id, string usuario)
    {
        using (var connection = new OracleConnection(_cs))
        {
            await connection.OpenAsync();
            using (var command = new OracleCommand("PK_MEDIOS_DIGITALES.P_DELETE_RED_SOCIAL", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value = id;
                command.Parameters.Add("P_Usuario", OracleDbType.Varchar2).Value = usuario;
                command.Parameters.Add("P_Maquina", OracleDbType.Varchar2).Value = Environment.MachineName;
                command.Parameters.Add("SRV_Message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                command.Parameters.Add("P_Resultado", OracleDbType.Int32).Direction = ParameterDirection.Output;

                await command.ExecuteNonQueryAsync();

                int resultado = Convert.ToInt32(command.Parameters["P_Resultado"].Value);
                string mensaje = command.Parameters["SRV_Message"].Value?.ToString() ?? string.Empty;
                bool success = resultado > 0;

                return new DtoRedesSocialesResult { Id = id, Success = success, Message = mensaje };
            }
        }
    }

    public async Task<IEnumerable<DtoDominio>> GetDominiosAsync()
    {
        var dominios = new List<DtoDominio>();

        using (var connection = new OracleConnection(_cs))
        {
            await connection.OpenAsync();
            using (var command = new OracleCommand("PK_ADMINISTRACION.P_GET_ALL_DOMINIO", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dominio = new DtoDominio
                        {
                            Id = reader.GetInt32(0), // Assuming ID
                            Descripcion = reader.GetString(1) // Assuming DESCRIPCION
                        };
                        // Assuming there's idPadre column
                        if (reader.FieldCount > 2)
                        {
                            var idPadre = reader.GetInt32(2);
                            if (idPadre == 164) // Filter for social networks
                            {
                                dominios.Add(dominio);
                            }
                        }
                        else
                        {
                            // If no idPadre, add all or assume
                            dominios.Add(dominio);
                        }
                    }
                }
            }
        }

        return dominios;
    }

    public async Task<IEnumerable<DtoUnidad>> GetUnidadesAsync()
    {
        var unidades = new List<DtoUnidad>();

        using (var connection = new OracleConnection(_cs))
        {
            await connection.OpenAsync();
            using (var command = new OracleCommand("SP_GET_UNIDADES_FULL", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("P_CURSOR", OracleDbType.RefCursor, ParameterDirection.Output);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        unidades.Add(new DtoUnidad
                        {
                            Value = reader.GetInt32(0),
                            Text = reader.GetString(1),
                            Descripcion = reader.GetString(2)
                        });
                    }
                }
            }
        }

        return unidades;
    }

    private async Task<DtoRedesSocialesResult> ExecuteInsUpdAsync(DtoRedesSociales redesSociales, string usuario, bool isUpdate)
    {
        try
        {
            using (var connection = new OracleConnection(_cs))
            {
                await connection.OpenAsync();
                using (var command = new OracleCommand("PK_MEDIOS_DIGITALES.P_INS_UPD_RED_SOCIAL", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.BindByName = true;

                    command.Parameters.Add("P_IdRedSocial", OracleDbType.Int32).Value =
                        isUpdate ? redesSociales.Id : DBNull.Value;
                    command.Parameters.Add("P_RedSocial", OracleDbType.Varchar2).Value = DbValue(redesSociales.RedSocial);
                    command.Parameters.Add("P_UnidadResponsable", OracleDbType.Varchar2).Value = DbValue(redesSociales.UnidadResponsable);
                    command.Parameters.Add("P_NombreCuenta", OracleDbType.Varchar2).Value = DbValue(redesSociales.NombreCuenta);
                    command.Parameters.Add("P_UrlCuenta", OracleDbType.Varchar2).Value = DbValue(redesSociales.UrlCuenta);
                    command.Parameters.Add("P_ActaAsignacion", OracleDbType.Varchar2).Value = DbValue(redesSociales.ActaAsignacion);
                    command.Parameters.Add("P_NombreResponsable", OracleDbType.Varchar2).Value = DbValue(redesSociales.NombreResponsable);
                    command.Parameters.Add("P_TelResponsable", OracleDbType.Varchar2).Value = DbValue(redesSociales.TelResponsable);
                    command.Parameters.Add("P_EmailResponsable", OracleDbType.Varchar2).Value = DbValue(redesSociales.EmailResponsable);
                    command.Parameters.Add("P_EmailCuenta", OracleDbType.Varchar2).Value = DbValue(redesSociales.EmailCuenta);
                    command.Parameters.Add("P_TelAsociado", OracleDbType.Varchar2).Value = DbValue(redesSociales.TelAsociado);
                    command.Parameters.Add("P_Vigente", OracleDbType.Int32).Value = redesSociales.Vigente;
                    command.Parameters.Add("P_Usuario", OracleDbType.Varchar2).Value = usuario;
                    command.Parameters.Add("P_Maquina", OracleDbType.Varchar2).Value = Environment.MachineName;
                    command.Parameters.Add("SRV_Message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    command.Parameters.Add("P_Resultado", OracleDbType.Decimal).Direction = ParameterDirection.Output;

                    await command.ExecuteNonQueryAsync();

                    var resultado = ReadOutInt(command.Parameters["P_Resultado"].Value);
                    var mensaje = command.Parameters["SRV_Message"].Value?.ToString() ?? string.Empty;

                    return new DtoRedesSocialesResult
                    {
                        Id = resultado,
                        Success = resultado > 0,
                        Message = mensaje
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al {Action} red social", isUpdate ? "actualizar" : "crear");
            return new DtoRedesSocialesResult
            {
                Id = 0,
                Success = false,
                Message = $"Error al {(isUpdate ? "actualizar" : "crear")} la red social: {ex.Message}. " +
                          $"Datos: id={redesSociales.Id}, redSocial={redesSociales.RedSocial}, unidad={redesSociales.UnidadResponsable}, vigente={redesSociales.Vigente}, usuario={usuario}"
            };
        }
    }

    private static DtoRedesSociales MapRedSocial(OracleDataReader reader)
    {
        return new DtoRedesSociales
        {
            Id = GetInt(reader, "ID_REDES_SOCIALES"),
            RedSocial = GetString(reader, "RED_SOCIAL"),
            UnidadResponsable = GetString(reader, "UNIDAD_RESPONSABLE"),
            NombreCuenta = GetString(reader, "NOMBRE_CUENTA"),
            UrlCuenta = GetString(reader, "URL_CUENTA"),
            ActaAsignacion = GetString(reader, "ACTA_ASIGNACION"),
            NombreResponsable = GetString(reader, "NOMBRE_RESPONSABLE"),
            TelResponsable = GetString(reader, "TELEFONO_RESPONSABLE"),
            EmailResponsable = GetString(reader, "EMAIL_RESPONSABLE"),
            EmailCuenta = GetString(reader, "EMAIL_CUENTA"),
            TelAsociado = GetString(reader, "TELEFONO_ASOCIADO"),
            Vigente = GetInt(reader, "VIGENTE", 1),
            UsuarioCreacion = GetStringOrNull(reader, "USUARIO_CREACION"),
            FechaCreacion = GetDateOrNull(reader, "FECHA_CREACION")
        };
    }

    private static object DbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }

    private static int ReadOutInt(object? value)
    {
        if (value is OracleDecimal oracleDecimal)
        {
            return oracleDecimal.IsNull ? 0 : oracleDecimal.ToInt32();
        }

        if (value is decimal dec)
        {
            return Convert.ToInt32(dec);
        }

        if (value == null || value == DBNull.Value)
        {
            return 0;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    }

    private static int GetOrdinalSafe(OracleDataReader reader, string column)
    {
        try
        {
            return reader.GetOrdinal(column);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    private static string GetString(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index))
        {
            return string.Empty;
        }

        return reader.GetString(index);
    }

    private static string? GetStringOrNull(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetString(index);
    }

    private static int GetInt(OracleDataReader reader, string column, int defaultValue = 0)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index))
        {
            return defaultValue;
        }

        var value = reader.GetValue(index);
        return value switch
        {
            int i => i,
            long l => Convert.ToInt32(l),
            decimal d => Convert.ToInt32(d),
            OracleDecimal od when !od.IsNull => od.ToInt32(),
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : defaultValue
        };
    }

    private static DateTime? GetDateOrNull(OracleDataReader reader, string column)
    {
        var index = GetOrdinalSafe(reader, column);
        if (index < 0 || reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        return value switch
        {
            DateTime dt => dt,
            OracleDate od when !od.IsNull => od.Value,
            _ => DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null
        };
    }
}
