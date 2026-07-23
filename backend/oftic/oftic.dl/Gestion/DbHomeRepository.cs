using Comun.Dtos.Home;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion
{
    public class DbHomeRepository : IDbHomeRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;

        public DbHomeRepository(IConfiguration configuration)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
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
                    usuariosActivos = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_usuarios WHERE bloqueado = 0", ct),
                    modulosConfigurados = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_menu WHERE activo = 1", ct),
                    rolesDefinidos = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_roles WHERE vigente = 1", ct),
                    reportesGenerados = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_auditoria", ct),
                    alertasSistema = 0
                };
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new Npgsql.NpgsqlConnection(_cs);
                await conn.OpenAsync(ct);
                return new DtoHomeStats
                {
                    usuariosActivos = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_usuarios WHERE bloqueado = 0", ct),
                    modulosConfigurados = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_menu WHERE activo = 1", ct),
                    rolesDefinidos = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_roles WHERE vigente = 1", ct),
                    reportesGenerados = await ExecuteCountGenericAsync(conn, "SELECT COUNT(*) FROM ctr_auditoria", ct),
                    alertasSistema = 0
                };
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            var stats = new DtoHomeStats
            {
                usuariosActivos = await ExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM ctr_usuarios WHERE NVL(bloqueado, 0) = 0", ct),
                modulosConfigurados = await ExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM ctr_menu WHERE NVL(activo, 1) = 1", ct),
                rolesDefinidos = await ExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM ctr_roles WHERE NVL(vigente, 1) = 1", ct),
                reportesGenerados = await ExecuteCountAsync(oracleConn, "SELECT COUNT(1) FROM ctr_auditoria WHERE fecha_creacion >= TRUNC(SYSDATE, 'MM')", ct),
                alertasSistema = 0
            };

            return stats;
        }

        private static async Task<int> ExecuteCountGenericAsync(System.Data.Common.DbConnection conn, string sql, CancellationToken ct)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var value = await cmd.ExecuteScalarAsync(ct);
            return (value == null || value == DBNull.Value) ? 0 : Convert.ToInt32(value);
        }

        private static async Task<int> ExecuteCountAsync(OracleConnection conn, string sql, CancellationToken ct)
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
    }
}
