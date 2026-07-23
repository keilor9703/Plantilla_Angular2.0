using System;
using System.Data.Common;

namespace Datos.Gestion
{
    public static class DbConnectionFactory
    {
        public static DbConnection CreateConnection(string connectionString, string? provider)
        {
            var p = (provider ?? "Oracle").Trim();

            if (p.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || p.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                return new MySqlConnector.MySqlConnection(connectionString);
            }

            if (p.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || p.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                return new Npgsql.NpgsqlConnection(connectionString);
            }

            return new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
        }
    }
}
