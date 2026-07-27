using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Datos.Interfaz;
using Comun.Extensions;

namespace Datos.Gestion
{
    public class DbAuthRepository : IDbAuthRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _cfg;
        private readonly ILogger<DbAuthRepository> _logger;

        public DbAuthRepository(IConfiguration cfg, ILogger<DbAuthRepository> logger)
        {
            _cs = cfg.GetDecryptedConnectionString();
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<(long? idUsuario, List<long> roles)> GetUsuarioYRolesAsync(string usuario, CancellationToken ct)
        {
            var provider = (_cfg["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    _logger.LogInformation("Buscando usuario en MySQL: {Usuario}", usuario);
                    long? idUsuario = null;

                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id_usuario FROM CTR_USUARIOS WHERE (LOWER(username) = LOWER(@pUser) OR identificacion = @pUser) AND bloqueado = 0";
                        cmd.Parameters.AddWithValue("@pUser", usuario);
                        var obj = await cmd.ExecuteScalarAsync(ct);
                        if (obj != null && obj != DBNull.Value) idUsuario = Convert.ToInt64(obj);
                    }

                    if (idUsuario is null) return (null, new List<long>());

                    var roles = new List<long>();
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT id_rol FROM CTR_ROLES_USER_ADMIN WHERE id_usuario = @pIdUser AND IFNULL(vigente, 0) = 1";
                        cmd.Parameters.AddWithValue("@pIdUser", idUsuario.Value);
                        await using var reader = await cmd.ExecuteReaderAsync(ct);
                        while (await reader.ReadAsync(ct)) roles.Add(reader.GetInt64(0));
                    }

                    if (roles.Count == 0) roles.Add(1);
                    return (idUsuario.Value, roles);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar usuario en MySQL. Se retornará null para auto-aprovisionar.");
                    return (null, new List<long>());
                }
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);

                    _logger.LogInformation("Buscando usuario en PostgreSQL: {Usuario}", usuario);
                    long? idUsuario = null;

                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id_usuario FROM ctr_usuarios WHERE (LOWER(username) = LOWER(@pUser) OR identificacion = @pUser) AND bloqueado = 0";
                        cmd.Parameters.AddWithValue("@pUser", usuario);
                        var obj = await cmd.ExecuteScalarAsync(ct);
                        if (obj != null && obj != DBNull.Value) idUsuario = Convert.ToInt64(obj);
                    }

                    if (idUsuario is null) return (null, new List<long>());

                    var roles = new List<long>();
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT id_rol FROM CTR_ROLES_USER_ADMIN WHERE id_usuario = @pIdUser AND COALESCE(vigente, 0) = 1";
                        cmd.Parameters.AddWithValue("@pIdUser", idUsuario.Value);
                        await using var reader = await cmd.ExecuteReaderAsync(ct);
                        while (await reader.ReadAsync(ct)) roles.Add(reader.GetInt64(0));
                    }

                    if (roles.Count == 0) roles.Add(1);
                    return (idUsuario.Value, roles);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar usuario en PostgreSQL. Se retornará null para auto-aprovisionar.");
                    return (null, new List<long>());
                }
            }

            // Oracle por defecto
            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                _logger.LogInformation("Buscando usuario en Oracle: {Usuario}", usuario);
                long? idUserOracle = null;

                await using (var cmd = oracleConn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"SELECT id_usuario FROM CTR_USUARIOS WHERE (UPPER(username) = UPPER(:pUsuario) OR identificacion = :pUsuario) AND bloqueado = 0";
                    cmd.Parameters.Add(":pUsuario", OracleDbType.Varchar2).Value = usuario;

                    var obj = await cmd.ExecuteScalarAsync(ct);
                    if (obj != null && obj != DBNull.Value) idUserOracle = Convert.ToInt64(obj);
                }

                if (idUserOracle is null) return (null, new List<long>());

                var oracleRoles = new List<long>();
                await using (var cmd = oracleConn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;
                    cmd.CommandText = @"
SELECT DISTINCT rua.id_rol
  FROM CTR_ROLES_USER_ADMIN rua
 WHERE rua.id_usuario = :pIdUsuario
   AND NVL(rua.vigente, 0) = 1";
                    cmd.Parameters.Add(":pIdUsuario", OracleDbType.Int64).Value = idUserOracle.Value;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct)) oracleRoles.Add(reader.GetInt64(0));
                }

                if (oracleRoles.Count == 0) oracleRoles.Add(1);
                return (idUserOracle.Value, oracleRoles);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar usuario en Oracle. Se retornará null para auto-aprovisionar.");
                return (null, new List<long>());
            }
        }
    }
}
