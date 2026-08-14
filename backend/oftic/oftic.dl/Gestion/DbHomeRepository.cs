using Comun.Dtos.Home;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion
{
    public class DbHomeRepository : IDbHomeRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbHomeRepository> _logger;

        public DbHomeRepository(IConfiguration configuration, ILogger<DbHomeRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DtoHomeStats> GetStatsAsync(CancellationToken ct)
        {
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new MySqlConnector.MySqlConnection(_cs);
                await conn.OpenAsync(ct);
                return new DtoHomeStats
                {
                    usuariosActivos = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM CTR_USUARIOS WHERE IFNULL(bloqueado, 0) = 0", ct),
                    modulosConfigurados = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM CTR_MENU WHERE IFNULL(activo, 1) = 1", ct),
                    rolesDefinidos = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM CTR_ROLES WHERE IFNULL(vigente, 1) = 1", ct),
                    reportesGenerados = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM CTR_AUDITORIA", ct),
                    alertasSistema = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM CTR_ROLES_USER_ADMIN WHERE IFNULL(vigente, 1) = 1 AND fecha_fin IS NOT NULL AND fecha_fin >= CURDATE() AND fecha_fin <= DATE_ADD(CURDATE(), INTERVAL 7 DAY)", ct)
                };
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                return new DtoHomeStats
                {
                    usuariosActivos = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_usuarios WHERE COALESCE(bloqueado, 0) = 0", ct),
                    modulosConfigurados = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_menu WHERE COALESCE(activo, 1) = 1", ct),
                    rolesDefinidos = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_roles WHERE COALESCE(vigente, 1) = 1", ct),
                    reportesGenerados = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_auditoria", ct),
                    alertasSistema = await SafeExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_roles_user_admin WHERE COALESCE(vigente, 1) = 1 AND fecha_fin IS NOT NULL AND fecha_fin >= CURRENT_DATE AND fecha_fin <= (CURRENT_DATE + INTERVAL '7 days')", ct)
                };
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            var stats = new DtoHomeStats
            {
                usuariosActivos = await SafeExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM CTR_USUARIOS WHERE NVL(bloqueado, 0) = 0", ct),
                modulosConfigurados = await SafeExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM CTR_MENU WHERE NVL(activo, 1) = 1", ct),
                rolesDefinidos = await SafeExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM CTR_ROLES WHERE NVL(vigente, 1) = 1", ct),
                reportesGenerados = await SafeExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM CTR_AUDITORIA WHERE fecha_creacion >= TRUNC(SYSDATE, 'MM')", ct),
                alertasSistema = await SafeExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM CTR_ROLES_USER_ADMIN WHERE NVL(vigente, 1) = 1 AND fecha_fin IS NOT NULL AND fecha_fin >= TRUNC(SYSDATE) AND fecha_fin <= (TRUNC(SYSDATE) + 7)", ct)
            };

            return stats;
        }

        private async Task<int> SafeExecuteCountGenericAsync(System.Data.Common.DbConnection conn, string sql, CancellationToken ct)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                var value = await cmd.ExecuteScalarAsync(ct);
                return (value == null || value == DBNull.Value) ? 0 : Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error ejecutando consulta de métrica Home: {Sql}", sql);
                return 0;
            }
        }

        private async Task<int> SafeExecuteCountAsync(OracleConnection conn, string sql, CancellationToken ct)
        {
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                var value = await cmd.ExecuteScalarAsync(ct);
                if (value is null || value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error ejecutando consulta de métrica Home en Oracle: {Sql}", sql);
                return 0;
            }
        }
    }
}
