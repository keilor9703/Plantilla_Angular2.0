using Comun.Dtos.GestionDocumental;
using Comun.Extensions;
using Datos.Interfaz.GestionDocumental;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion.GestionDocumental
{
    public class DbGestionCorreosRepository : IDbGestionCorreosRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _config;
        private readonly ILogger<DbGestionCorreosRepository> _logger;

        public DbGestionCorreosRepository(IConfiguration config, ILogger<DbGestionCorreosRepository> logger)
        {
            _cs = config.GetDecryptedConnectionString();
            _config = config;
            _logger = logger;
        }

        public bool RegistrarEnvio(DtoEnvioCorreoRequest request, string radicado, string usuario, string maquina)
        {
            var provider = (_config["DatabaseProvider"] ?? "Oracle").Trim();

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Para)) partes.Add(request.Para.Trim());
            if (!string.IsNullOrWhiteSpace(request.Cc)) partes.Add(request.Cc.Trim());
            var destinatario = partes.Count > 0 ? string.Join("; ", partes) : "(sin destinatario)";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnector.MySqlConnection(_cs);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO ctr_envio_correo (id_cuenta_email, radicado, destinatario, asunto, cuerpo, id_clasificacion, usuario_crea, maquina_crea, fecha_crea)
                        VALUES (@c, @r, @d, @a, @b, @cat, @u, @m, NOW())";
                    command.Parameters.AddWithValue("@c", request.IdCuentaEmail);
                    command.Parameters.AddWithValue("@r", radicado ?? "");
                    command.Parameters.AddWithValue("@d", destinatario);
                    command.Parameters.AddWithValue("@a", request.Asunto ?? "");
                    command.Parameters.AddWithValue("@b", request.Cuerpo ?? "");
                    command.Parameters.AddWithValue("@cat", request.IdClasificacion);
                    command.Parameters.AddWithValue("@u", usuario ?? "SISTEMA");
                    command.Parameters.AddWithValue("@m", maquina ?? "LOCAL");
                    command.ExecuteNonQuery();
                    return true;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new Npgsql.NpgsqlConnection(_cs);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO ctr_envio_correo (id_cuenta_email, radicado, destinatario, asunto, cuerpo, id_clasificacion, usuario_crea, maquina_crea, fecha_crea)
                        VALUES (@c, @r, @d, @a, @b, @cat, @u, @m, CURRENT_TIMESTAMP)";
                    command.Parameters.AddWithValue("@c", request.IdCuentaEmail);
                    command.Parameters.AddWithValue("@r", radicado ?? "");
                    command.Parameters.AddWithValue("@d", destinatario);
                    command.Parameters.AddWithValue("@a", request.Asunto ?? "");
                    command.Parameters.AddWithValue("@b", request.Cuerpo ?? "");
                    command.Parameters.AddWithValue("@cat", request.IdClasificacion);
                    command.Parameters.AddWithValue("@u", usuario ?? "SISTEMA");
                    command.Parameters.AddWithValue("@m", maquina ?? "LOCAL");
                    command.ExecuteNonQuery();
                    return true;
                }

                using (var connection = new OracleConnection(_cs))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO CTR_ENVIO_CORREO (ID_ENVIO, ID_CUENTA_EMAIL, RADICADO, DESTINATARIO, ASUNTO, CUERPO, ID_CLASIFICACION, USUARIO_CREA, MAQUINA_CREA, FECHA_CREA)
                            VALUES (SEQ_CTR_ENVIO_CORREO.NEXTVAL, :c, :r, :d, :a, :b, :cat, :u, :m, SYSDATE)";
                        command.Parameters.Add(":c", OracleDbType.Int32).Value = request.IdCuentaEmail;
                        command.Parameters.Add(":r", OracleDbType.Varchar2).Value = radicado ?? "";
                        command.Parameters.Add(":d", OracleDbType.Varchar2).Value = destinatario;
                        command.Parameters.Add(":a", OracleDbType.Varchar2).Value = request.Asunto ?? "";
                        command.Parameters.Add(":b", OracleDbType.Clob).Value = request.Cuerpo ?? "";
                        command.Parameters.Add(":cat", OracleDbType.Int32).Value = (object?)request.IdClasificacion ?? DBNull.Value;
                        command.Parameters.Add(":u", OracleDbType.Varchar2).Value = usuario ?? "SISTEMA";
                        command.Parameters.Add(":m", OracleDbType.Varchar2).Value = maquina ?? "LOCAL";
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar envío de correo en DB");
                return false;
            }
        }

        public List<DtoHistoricoCorreo> ConsultarHistorico()
        {
            var lista = new List<DtoHistoricoCorreo>();
            var provider = (_config["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT e.id_envio AS ID_ENVIO, e.radicado AS RADICADO, e.destinatario AS DESTINATARIO, e.asunto AS ASUNTO,
                       e.cuerpo AS CUERPO, e.fecha_crea AS FECHA, c.email AS DE_EMAIL, c.nombre_cuenta AS NOMBRE_CUENTA,
                       e.usuario_crea AS USERNAME, d.descripcion AS CLASIFICACION
                FROM CTR_ENVIO_CORREO e
                LEFT JOIN CTR_CUENTA_EMAIL c ON e.id_cuenta_email = c.id_cuenta
                LEFT JOIN CTR_DOMINIOS d ON e.id_clasificacion = d.id_dominio
                ORDER BY e.id_envio DESC";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnector.MySqlConnection(_cs);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = selectSql;
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        lista.Add(new DtoHistoricoCorreo
                        {
                            IdEnvio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            Radicado = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Destinatario = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Asunto = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Cuerpo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Fecha = reader.IsDBNull(5) ? DateTime.Now : Convert.ToDateTime(reader.GetValue(5)),
                            DeEmail = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            NombreCuenta = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Username = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            Clasificacion = reader.IsDBNull(9) ? "" : reader.GetString(9)
                        });
                    }
                    return lista;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new Npgsql.NpgsqlConnection(_cs);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = selectSql;
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        lista.Add(new DtoHistoricoCorreo
                        {
                            IdEnvio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            Radicado = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Destinatario = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Asunto = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Cuerpo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Fecha = reader.IsDBNull(5) ? DateTime.Now : Convert.ToDateTime(reader.GetValue(5)),
                            DeEmail = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            NombreCuenta = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Username = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            Clasificacion = reader.IsDBNull(9) ? "" : reader.GetString(9)
                        });
                    }
                    return lista;
                }

                using (var connection = new OracleConnection(_cs))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = selectSql;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new DtoHistoricoCorreo
                                {
                                    IdEnvio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                                    Radicado = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    Destinatario = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Asunto = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Cuerpo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Fecha = reader.IsDBNull(5) ? DateTime.Now : Convert.ToDateTime(reader.GetValue(5)),
                                    DeEmail = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    NombreCuenta = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    Username = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    Clasificacion = reader.IsDBNull(9) ? "" : reader.GetString(9)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar histórico de correos");
            }
            return lista;
        }

        private int GetOutputInt(OracleParameter param)
        {
            if (param.Value == null || param.Value == DBNull.Value) return 0;
            if (param.Value is Oracle.ManagedDataAccess.Types.OracleDecimal od)
                return od.IsNull ? 0 : (int)od.Value;
            return Convert.ToInt32(param.Value);
        }
    }
}
