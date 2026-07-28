using Comun.Extensions;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbBrandingRepository> _logger;

        public DbBrandingRepository(IConfiguration configuration, ILogger<DbBrandingRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<DtoBranding>> GetAllAsync(CancellationToken ct)
        {
            var lista = new List<DtoBranding>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnector.MySqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT id_branding, sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, fondo_2_file_name, fondo_3_file_name, vigente FROM CTR_BRANDING ORDER BY id_branding DESC";
                    using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        lista.Add(new DtoBranding
                        {
                            IdBranding = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Sistema = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            NombreSistema = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            UnidadSistemaId = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                            Sigla = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            NombreUnidad = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            LogoFileName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            FaviconFileName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Fondo1FileName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            Fondo2FileName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            Fondo3FileName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                            Vigente = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11))
                        });
                    }
                    return lista;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new Npgsql.NpgsqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT id_branding, sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, fondo_2_file_name, fondo_3_file_name, vigente FROM ctr_branding ORDER BY id_branding DESC";
                    using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        lista.Add(new DtoBranding
                        {
                            IdBranding = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Sistema = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            NombreSistema = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            UnidadSistemaId = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                            Sigla = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            NombreUnidad = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            LogoFileName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            FaviconFileName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Fondo1FileName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            Fondo2FileName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            Fondo3FileName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                            Vigente = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11))
                        });
                    }
                    return lista;
                }

                // Oracle Inline Query Fallback (Sin procedimientos almacenados)
                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    using (var command = new OracleCommand("SELECT ID_BRANDING, SISTEMA, NOMBRE_SISTEMA, UNIDAD_SISTEMA_ID, SIGLA, NOMBRE_UNIDAD, LOGO_FILE_NAME, FAVICON_FILE_NAME, FONDO_1_FILE_NAME, FONDO_2_FILE_NAME, FONDO_3_FILE_NAME, VIGENTE FROM CTR_BRANDING ORDER BY ID_BRANDING DESC", connection))
                    {
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
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();
            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnector.MySqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE CTR_BRANDING SET vigente = 0, usuario_modificacion = @u, maquina_modificacion = @m, fecha_modificacion = NOW() WHERE id_branding = @id";
                    command.Parameters.AddWithValue("@u", usuario);
                    command.Parameters.AddWithValue("@m", maquina);
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync(ct);
                    return new DtoBrandingResult { Success = true, Message = "Branding eliminado lógicamente." };
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new Npgsql.NpgsqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE ctr_branding SET vigente = 0, usuario_modificacion = @u, maquina_modificacion = @m, fecha_modificacion = CURRENT_TIMESTAMP WHERE id_branding = @id";
                    command.Parameters.AddWithValue("@u", usuario);
                    command.Parameters.AddWithValue("@m", maquina);
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync(ct);
                    return new DtoBrandingResult { Success = true, Message = "Branding eliminado lógicamente." };
                }

                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    using (var command = connection.CreateCommand())
                    {
                        command.BindByName = true;
                        command.CommandText = "UPDATE CTR_BRANDING SET VIGENTE = 0, USUARIO_MODIFICA = :u, MAQUINA_MODIFICA = :m, FECHA_MODIFICA = SYSDATE WHERE ID_BRANDING = :id";
                        command.Parameters.Add("u", OracleDbType.Varchar2).Value = usuario;
                        command.Parameters.Add("m", OracleDbType.Varchar2).Value = maquina;
                        command.Parameters.Add("id", OracleDbType.Int32).Value = id;

                        try
                        {
                            await command.ExecuteNonQueryAsync(ct);
                        }
                        catch (OracleException ex) when (ex.Number == 904)
                        {
                            command.CommandText = "UPDATE CTR_BRANDING SET VIGENTE = 0, USUARIO_MODIFICACION = :u, MAQUINA_MODIFICACION = :m, FECHA_MODIFICACION = SYSDATE WHERE ID_BRANDING = :id";
                            await command.ExecuteNonQueryAsync(ct);
                        }
                        return new DtoBrandingResult { Success = true, Message = "Branding eliminado lógicamente." };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar lógicamente branding con ID {Id}", id);
                return new DtoBrandingResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        private async Task<DtoBrandingResult> ExecuteInsUpdAsync(DtoBrandingRequest request, bool isUpdate, CancellationToken ct)
        {
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();
            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnector.MySqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();

                    if (isUpdate)
                    {
                        command.CommandText = @"UPDATE CTR_BRANDING SET sistema = @s, nombre_sistema = @ns, unidad_sistema_id = @uId, sigla = @sigla, nombre_unidad = @nu, logo_file_name = @logo, favicon_file_name = @fav, fondo_1_file_name = @f1, fondo_2_file_name = @f2, fondo_3_file_name = @f3, vigente = @v, usuario_modificacion = @um, maquina_modificacion = @mm, fecha_modificacion = NOW() WHERE id_branding = @id";
                        command.Parameters.AddWithValue("@id", request.IdBranding);
                    }
                    else
                    {
                        command.CommandText = @"INSERT INTO CTR_BRANDING (sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, fondo_2_file_name, fondo_3_file_name, vigente, usuario_creacion, maquina_creacion, fecha_creacion) VALUES (@s, @ns, @uId, @sigla, @nu, @logo, @fav, @f1, @f2, @f3, @v, @uc, @mc, NOW())";
                        command.Parameters.AddWithValue("@uc", request.UsuarioAuditoria ?? "SETUP");
                        command.Parameters.AddWithValue("@mc", request.MaquinaAuditoria ?? "LOCAL");
                    }

                    command.Parameters.AddWithValue("@s", request.Sistema ?? "GUSOF");
                    command.Parameters.AddWithValue("@ns", request.NombreSistema ?? "Sistema");
                    command.Parameters.AddWithValue("@uId", request.UnidadSistemaId);
                    command.Parameters.AddWithValue("@sigla", request.Sigla ?? "POLICIA");
                    command.Parameters.AddWithValue("@nu", request.NombreUnidad ?? "");
                    command.Parameters.AddWithValue("@logo", request.LogoFileName ?? "");
                    command.Parameters.AddWithValue("@fav", request.FaviconFileName ?? "");
                    command.Parameters.AddWithValue("@f1", request.Fondo1FileName ?? "");
                    command.Parameters.AddWithValue("@f2", request.Fondo2FileName ?? "");
                    command.Parameters.AddWithValue("@f3", request.Fondo3FileName ?? "");
                    command.Parameters.AddWithValue("@v", request.Vigente);
                    if (isUpdate)
                    {
                        command.Parameters.AddWithValue("@um", request.UsuarioAuditoria ?? "SETUP");
                        command.Parameters.AddWithValue("@mm", request.MaquinaAuditoria ?? "LOCAL");
                    }

                    await command.ExecuteNonQueryAsync(ct);
                    int id = isUpdate ? request.IdBranding : Convert.ToInt32(command.LastInsertedId);
                    return new DtoBrandingResult { Id = id, Success = true, Message = "Operación exitosa en MySQL." };
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new Npgsql.NpgsqlConnection(_cs);
                    await connection.OpenAsync(ct);
                    using var command = connection.CreateCommand();

                    if (isUpdate)
                    {
                        command.CommandText = @"UPDATE ctr_branding SET sistema = @s, nombre_sistema = @ns, unidad_sistema_id = @uId, sigla = @sigla, nombre_unidad = @nu, logo_file_name = @logo, favicon_file_name = @fav, fondo_1_file_name = @f1, fondo_2_file_name = @f2, fondo_3_file_name = @f3, vigente = @v, usuario_modificacion = @um, maquina_modificacion = @mm, fecha_modificacion = CURRENT_TIMESTAMP WHERE id_branding = @id";
                        command.Parameters.AddWithValue("@id", request.IdBranding);
                    }
                    else
                    {
                        command.CommandText = @"INSERT INTO ctr_branding (sistema, nombre_sistema, unidad_sistema_id, sigla, nombre_unidad, logo_file_name, favicon_file_name, fondo_1_file_name, fondo_2_file_name, fondo_3_file_name, vigente, usuario_creacion, maquina_creacion, fecha_creacion) VALUES (@s, @ns, @uId, @sigla, @nu, @logo, @fav, @f1, @f2, @f3, @v, @uc, @mc, CURRENT_TIMESTAMP) RETURNING id_branding";
                        command.Parameters.AddWithValue("@uc", request.UsuarioAuditoria ?? "SETUP");
                        command.Parameters.AddWithValue("@mc", request.MaquinaAuditoria ?? "LOCAL");
                    }

                    command.Parameters.AddWithValue("@s", request.Sistema ?? "GUSOF");
                    command.Parameters.AddWithValue("@ns", request.NombreSistema ?? "Sistema");
                    command.Parameters.AddWithValue("@uId", request.UnidadSistemaId);
                    command.Parameters.AddWithValue("@sigla", request.Sigla ?? "POLICIA");
                    command.Parameters.AddWithValue("@nu", request.NombreUnidad ?? "");
                    command.Parameters.AddWithValue("@logo", request.LogoFileName ?? "");
                    command.Parameters.AddWithValue("@fav", request.FaviconFileName ?? "");
                    command.Parameters.AddWithValue("@f1", request.Fondo1FileName ?? "");
                    command.Parameters.AddWithValue("@f2", request.Fondo2FileName ?? "");
                    command.Parameters.AddWithValue("@f3", request.Fondo3FileName ?? "");
                    command.Parameters.AddWithValue("@v", request.Vigente);
                    if (isUpdate)
                    {
                        command.Parameters.AddWithValue("@um", request.UsuarioAuditoria ?? "SETUP");
                        command.Parameters.AddWithValue("@mm", request.MaquinaAuditoria ?? "LOCAL");
                    }

                    if (isUpdate)
                    {
                        await command.ExecuteNonQueryAsync(ct);
                        return new DtoBrandingResult { Id = request.IdBranding, Success = true, Message = "Actualización exitosa en PostgreSQL." };
                    }
                    else
                    {
                        var scalarId = await command.ExecuteScalarAsync(ct);
                        int id = scalarId != null ? Convert.ToInt32(scalarId) : 1;
                        return new DtoBrandingResult { Id = id, Success = true, Message = "Inserción exitosa en PostgreSQL." };
                    }
                }

                // Oracle Direct DML (sin depender de Stored Procedures PK_ADMINISTRACION)
                using (var connection = new OracleConnection(_cs))
                {
                    await connection.OpenAsync(ct);
                    using (var command = connection.CreateCommand())
                    {
                        command.BindByName = true;
                        if (isUpdate)
                        {
                            command.CommandText = @"
UPDATE CTR_BRANDING
   SET SISTEMA           = :sistema,
       NOMBRE_SISTEMA    = :nombre_sistema,
       UNIDAD_SISTEMA_ID = :unidad_sistema_id,
       SIGLA             = :sigla,
       NOMBRE_UNIDAD     = :nombre_unidad,
       LOGO_FILE_NAME    = :logo_file_name,
       FAVICON_FILE_NAME = :favicon_file_name,
       FONDO_1_FILE_NAME = :fondo_1_file_name,
       FONDO_2_FILE_NAME = :fondo_2_file_name,
       FONDO_3_FILE_NAME = :fondo_3_file_name,
       VIGENTE           = :vigente,
       USUARIO_MODIFICA  = :usuario_auditoria,
       MAQUINA_MODIFICA  = :maquina_auditoria,
       FECHA_MODIFICA    = SYSDATE
 WHERE ID_BRANDING       = :id";

                            command.Parameters.Add("id", OracleDbType.Int32).Value = request.IdBranding;
                            command.Parameters.Add("sistema", OracleDbType.Varchar2).Value = request.Sistema ?? "GUSOF";
                            command.Parameters.Add("nombre_sistema", OracleDbType.Varchar2).Value = request.NombreSistema ?? "Sistema";
                            command.Parameters.Add("unidad_sistema_id", OracleDbType.Int32).Value = request.UnidadSistemaId;
                            command.Parameters.Add("sigla", OracleDbType.Varchar2).Value = request.Sigla ?? "POLICIA";
                            command.Parameters.Add("nombre_unidad", OracleDbType.Varchar2).Value = request.NombreUnidad ?? "";
                            command.Parameters.Add("logo_file_name", OracleDbType.Varchar2).Value = request.LogoFileName ?? "";
                            command.Parameters.Add("favicon_file_name", OracleDbType.Varchar2).Value = request.FaviconFileName ?? "";
                            command.Parameters.Add("fondo_1_file_name", OracleDbType.Varchar2).Value = request.Fondo1FileName ?? "";
                            command.Parameters.Add("fondo_2_file_name", OracleDbType.Varchar2).Value = request.Fondo2FileName ?? "";
                            command.Parameters.Add("fondo_3_file_name", OracleDbType.Varchar2).Value = request.Fondo3FileName ?? "";
                            command.Parameters.Add("vigente", OracleDbType.Int32).Value = request.Vigente;
                            command.Parameters.Add("usuario_auditoria", OracleDbType.Varchar2).Value = request.UsuarioAuditoria ?? "SETUP";
                            command.Parameters.Add("maquina_auditoria", OracleDbType.Varchar2).Value = request.MaquinaAuditoria ?? "LOCAL";

                            try
                            {
                                await command.ExecuteNonQueryAsync(ct);
                            }
                            catch (OracleException ex) when (ex.Number == 904)
                            {
                                command.CommandText = @"
UPDATE CTR_BRANDING
   SET SISTEMA               = :sistema,
       NOMBRE_SISTEMA        = :nombre_sistema,
       UNIDAD_SISTEMA_ID     = :unidad_sistema_id,
       SIGLA                 = :sigla,
       NOMBRE_UNIDAD         = :nombre_unidad,
       LOGO_FILE_NAME        = :logo_file_name,
       FAVICON_FILE_NAME     = :favicon_file_name,
       FONDO_1_FILE_NAME     = :fondo_1_file_name,
       FONDO_2_FILE_NAME     = :fondo_2_file_name,
       FONDO_3_FILE_NAME     = :fondo_3_file_name,
       VIGENTE               = :vigente,
       USUARIO_MODIFICACION  = :usuario_auditoria,
       MAQUINA_MODIFICACION  = :maquina_auditoria,
       FECHA_MODIFICACION    = SYSDATE
 WHERE ID_BRANDING           = :id";
                                await command.ExecuteNonQueryAsync(ct);
                            }
                            return new DtoBrandingResult { Id = request.IdBranding, Success = true, Message = "Actualización exitosa en Oracle." };
                        }
                        else
                        {
                            long nextId = 1;
                            try
                            {
                                using var cmdNext = connection.CreateCommand();
                                cmdNext.CommandText = "SELECT NVL(MAX(ID_BRANDING), 0) + 1 FROM CTR_BRANDING";
                                var val = await cmdNext.ExecuteScalarAsync(ct);
                                nextId = val != null && val != DBNull.Value ? Convert.ToInt64(val) : 1;
                            }
                            catch { nextId = 1; }

                            command.CommandText = @"
INSERT INTO CTR_BRANDING
  (ID_BRANDING, SISTEMA, NOMBRE_SISTEMA, UNIDAD_SISTEMA_ID, SIGLA, NOMBRE_UNIDAD,
   LOGO_FILE_NAME, FAVICON_FILE_NAME, FONDO_1_FILE_NAME, FONDO_2_FILE_NAME, FONDO_3_FILE_NAME,
   VIGENTE, USUARIO_CREACION, MAQUINA_CREACION, FECHA_CREACION)
VALUES
  (:id, :sistema, :nombre_sistema, :unidad_sistema_id, :sigla, :nombre_unidad,
   :logo_file_name, :favicon_file_name, :fondo_1_file_name, :fondo_2_file_name, :fondo_3_file_name,
   :vigente, :usuario_auditoria, :maquina_auditoria, SYSDATE)";

                            command.Parameters.Add("id", OracleDbType.Int64).Value = nextId;
                            command.Parameters.Add("sistema", OracleDbType.Varchar2).Value = request.Sistema ?? "GUSOF";
                            command.Parameters.Add("nombre_sistema", OracleDbType.Varchar2).Value = request.NombreSistema ?? "Sistema";
                            command.Parameters.Add("unidad_sistema_id", OracleDbType.Int32).Value = request.UnidadSistemaId;
                            command.Parameters.Add("sigla", OracleDbType.Varchar2).Value = request.Sigla ?? "POLICIA";
                            command.Parameters.Add("nombre_unidad", OracleDbType.Varchar2).Value = request.NombreUnidad ?? "";
                            command.Parameters.Add("logo_file_name", OracleDbType.Varchar2).Value = request.LogoFileName ?? "";
                            command.Parameters.Add("favicon_file_name", OracleDbType.Varchar2).Value = request.FaviconFileName ?? "";
                            command.Parameters.Add("fondo_1_file_name", OracleDbType.Varchar2).Value = request.Fondo1FileName ?? "";
                            command.Parameters.Add("fondo_2_file_name", OracleDbType.Varchar2).Value = request.Fondo2FileName ?? "";
                            command.Parameters.Add("fondo_3_file_name", OracleDbType.Varchar2).Value = request.Fondo3FileName ?? "";
                            command.Parameters.Add("vigente", OracleDbType.Int32).Value = request.Vigente;
                            command.Parameters.Add("usuario_auditoria", OracleDbType.Varchar2).Value = request.UsuarioAuditoria ?? "SETUP";
                            command.Parameters.Add("maquina_auditoria", OracleDbType.Varchar2).Value = request.MaquinaAuditoria ?? "LOCAL";

                            await command.ExecuteNonQueryAsync(ct);
                            return new DtoBrandingResult { Id = (int)nextId, Success = true, Message = "Inserción exitosa en Oracle." };
                        }
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
