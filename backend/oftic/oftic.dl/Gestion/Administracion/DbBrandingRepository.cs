using Comun.Dtos.Administracion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion
{
    public class DbBrandingRepository : IDbBrandingRepository
    {
        private readonly string _cs;
        private readonly ILogger<DbBrandingRepository> _logger;

        public DbBrandingRepository(IConfiguration configuration, ILogger<DbBrandingRepository> logger)
        {
            _cs = configuration.GetConnectionString("DbCoest")!;
            _logger = logger;
        }

        public async Task<List<DtoBranding>> GetAllAsync(CancellationToken ct)
        {
            var lista = new List<DtoBranding>();
            try
            {
                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    using (var command = new OracleCommand("USR_COEST.PK_ADMINISTRACION.P_GET_ALL_BRANDING", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync(ct))
                        {
                            while (await reader.ReadAsync(ct))
                            {
                                lista.Add(new DtoBranding
                                {
                                    IdBranding = GetInt(reader, "ID_BRANDING"),
                                    Sistema = GetString(reader, "SISTEMA"),
                                    NombreSistema = GetString(reader, "NOMBRE_SISTEMA"),
                                    UnidadSistemaId = GetInt(reader, "UNIDAD_SISTEMA_ID"),
                                    Sigla = GetString(reader, "SIGLA"),
                                    NombreUnidad = GetString(reader, "NOMBRE_UNIDAD"),
                                    LogoFileName = GetString(reader, "LOGO_FILE_NAME"),
                                    FaviconFileName = GetString(reader, "FAVICON_FILE_NAME"),
                                    Fondo1FileName = GetString(reader, "FONDO_1_FILE_NAME"),
                                    Fondo2FileName = GetString(reader, "FONDO_2_FILE_NAME"),
                                    Fondo3FileName = GetString(reader, "FONDO_3_FILE_NAME"),
                                    Vigente = GetInt(reader, "VIGENTE")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar branding");
                throw;
            }
            return lista;
        }

        public async Task<DtoBrandingResult> CreateAsync(DtoBrandingRequest request, CancellationToken ct)
        {
            return await ExecuteInsUpdAsync(request, isUpdate: false, ct);
        }

        public async Task<DtoBrandingResult> UpdateAsync(DtoBrandingRequest request, CancellationToken ct)
        {
            return await ExecuteInsUpdAsync(request, isUpdate: true, ct);
        }

        public async Task<DtoBrandingResult> DeleteLogicalAsync(int id, string usuario, string maquina, CancellationToken ct)
        {
            try
            {
                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    using (var command = new OracleCommand("USR_COEST.PK_ADMINISTRACION.P_DELETE_LOGICAL_BRANDING", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("p_id_branding", OracleDbType.Int32).Value = id;
                        command.Parameters.Add("p_usuario_modificacion", OracleDbType.Varchar2).Value = usuario;
                        command.Parameters.Add("p_maquina_modificacion", OracleDbType.Varchar2).Value = maquina;
                        command.Parameters.Add("p_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("p_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync(ct);

                        return new DtoBrandingResult
                        {
                            Success = GetOutputInt(command.Parameters["p_success"]) == 1,
                            Message = command.Parameters["p_message"].Value?.ToString() ?? string.Empty
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar branding id {Id}", id);
                return new DtoBrandingResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<DtoBrandingResult> ExecuteInsUpdAsync(DtoBrandingRequest request, bool isUpdate, CancellationToken ct)
        {
            try
            {
                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    string proc = isUpdate ? "USR_COEST.PK_ADMINISTRACION.P_UPDATE_BRANDING" : "USR_COEST.PK_ADMINISTRACION.P_CREATE_BRANDING";
                    using (var command = new OracleCommand(proc, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.BindByName = true;

                        if (isUpdate)
                        {
                            command.Parameters.Add("p_id_branding", OracleDbType.Int32).Value = request.IdBranding;
                        }

                        command.Parameters.Add("p_sistema", OracleDbType.Varchar2).Value = request.Sistema;
                        command.Parameters.Add("p_nombre_sistema", OracleDbType.Varchar2).Value = request.NombreSistema;
                        command.Parameters.Add("p_unidad_sistema_id", OracleDbType.Int32).Value = request.UnidadSistemaId;
                        command.Parameters.Add("p_sigla", OracleDbType.Varchar2).Value = request.Sigla;
                        command.Parameters.Add("p_nombre_unidad", OracleDbType.Varchar2).Value = request.NombreUnidad;
                        command.Parameters.Add("p_logo_file_name", OracleDbType.Varchar2).Value = request.LogoFileName;
                        command.Parameters.Add("p_favicon_file_name", OracleDbType.Varchar2).Value = request.FaviconFileName;
                        command.Parameters.Add("p_fondo_1_file_name", OracleDbType.Varchar2).Value = request.Fondo1FileName;
                        command.Parameters.Add("p_fondo_2_file_name", OracleDbType.Varchar2).Value = request.Fondo2FileName;
                        command.Parameters.Add("p_fondo_3_file_name", OracleDbType.Varchar2).Value = request.Fondo3FileName;
                        command.Parameters.Add("p_vigente", OracleDbType.Int32).Value = request.Vigente;

                        if (isUpdate)
                        {
                            command.Parameters.Add("p_usuario_modificacion", OracleDbType.Varchar2).Value = request.UsuarioAuditoria;
                            command.Parameters.Add("p_maquina_modificacion", OracleDbType.Varchar2).Value = request.MaquinaAuditoria;
                        }
                        else
                        {
                            command.Parameters.Add("p_usuario_creacion", OracleDbType.Varchar2).Value = request.UsuarioAuditoria;
                            command.Parameters.Add("p_maquina_creacion", OracleDbType.Varchar2).Value = request.MaquinaAuditoria;
                            command.Parameters.Add("p_id_branding", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        }

                        command.Parameters.Add("p_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("p_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync(ct);

                        int id = isUpdate ? request.IdBranding : GetOutputInt(command.Parameters["p_id_branding"]);

                        return new DtoBrandingResult
                        {
                            Id = id,
                            Success = GetOutputInt(command.Parameters["p_success"]) == 1,
                            Message = command.Parameters["p_message"].Value?.ToString() ?? string.Empty
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al gestionar branding");
                return new DtoBrandingResult { Success = false, Message = ex.Message };
            }
        }

        // Helpers
        private static string GetString(OracleDataReader reader, string column)
        {
            var index = reader.GetOrdinal(column);
            return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
        }

        private static int GetInt(OracleDataReader reader, string column)
        {
            var index = reader.GetOrdinal(column);
            if (reader.IsDBNull(index)) return 0;
            var val = reader.GetValue(index);
            
            string? sVal = val.ToString();
            if (int.TryParse(sVal, out int result)) return result;

            try { return Convert.ToInt32(val); }
            catch { return 0; }
        }

        private static int GetOutputInt(OracleParameter param)
        {
            if (param.Value == null || param.Value == DBNull.Value) return 0;
            
            // Usar ToString() es lo más seguro con tipos de Oracle para evitar InvalidCastException
            string? sVal = param.Value.ToString();
            if (int.TryParse(sVal, out int result))
            {
                return result;
            }

            try 
            {
                return Convert.ToInt32(param.Value);
            }
            catch 
            {
                return 0;
            }
        }
    }
}
