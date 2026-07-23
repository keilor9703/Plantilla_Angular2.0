using Comun.Dtos.Ubicacion;
using Comun.Dtos.Administracion;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion
{
    public class DbUbicacionRepository : IDbUbicacionRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;

        public DbUbicacionRepository(IConfiguration configuration)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
        }

        public async Task<List<DtoUnidadOrg>> GetUnidadesOrganigramaAsync(CancellationToken ct)
        {
            var result = new List<DtoUnidadOrg>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_dominio AS consecutivo, abreviatura AS sigla_fisica, descripcion AS descripcion_dependencia
                FROM ctr_dominio
                WHERE id_padre = 0 OR id_padre IS NULL
                ORDER BY id_dominio";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoUnidadOrg
                        {
                            Consecutivo = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Sigla = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nombre = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoUnidadOrg
                        {
                            Consecutivo = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Sigla = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nombre = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                try
                {
                    await using var cmd = oracleConn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "SP_UNIDADES_DIRECCIONES_OFICINAS";
                    cmd.Parameters.Add("P_CURSOR", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoUnidadOrg
                        {
                            Consecutivo = (int)GetInt64(reader, "consecutivo"),
                            Sigla = GetString(reader, "sigla_fisica"),
                            Nombre = GetString(reader, "descripcion_dependencia")
                        });
                    }
                }
                catch
                {
                    result.Clear();
                    await using var cmdQuery = oracleConn.CreateCommand();
                    cmdQuery.CommandText = selectSql;
                    await using var reader = await cmdQuery.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoUnidadOrg
                        {
                            Consecutivo = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                            Sigla = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nombre = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }
            catch
            {
                // Fallback seguro
            }

            return result;
        }

        public async Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct)
        {
            var result = new List<DtoDepartamento>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_departamento, codigo_dane, nombre
                FROM ctr_departamento
                ORDER BY nombre";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoDepartamento
                        {
                            IdDepartamento = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            CodigoDane = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nombre = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoDepartamento
                        {
                            IdDepartamento = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            CodigoDane = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Nombre = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                try
                {
                    await using var cmd = oracleConn.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PK_ADMINISTRACION.P_GET_DEPARTAMENTOS";
                    cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoDepartamento
                        {
                            IdDepartamento = GetInt64(reader, "ID_DEPARTAMENTO"),
                            CodigoDane = GetString(reader, "CODIGO_DANE"),
                            Nombre = GetString(reader, "NOMBRE")
                        });
                    }
                }
                catch
                {
                    result.Clear();
                }
            }
            catch
            {
                // Fallback
            }

            return result;
        }

        public async Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct)
        {
            var result = new List<DtoMunicipio>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_municipio, id_departamento, codigo_dane, nombre, longitud, latitud
                FROM ctr_municipio
                WHERE (@id IS NULL OR id_departamento = @id)
                ORDER BY nombre";

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    cmd.Parameters.AddWithValue("@id", idDepartamento.HasValue ? idDepartamento.Value : DBNull.Value);
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoMunicipio
                        {
                            IdMunicipio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            IdDepartamento = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)),
                            CodigoDane = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nombre = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Longitud = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4)),
                            Latitud = reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5))
                        });
                    }
                    return result;
                }

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    cmd.Parameters.AddWithValue("@id", idDepartamento.HasValue ? idDepartamento.Value : DBNull.Value);
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoMunicipio
                        {
                            IdMunicipio = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            IdDepartamento = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)),
                            CodigoDane = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Nombre = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Longitud = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4)),
                            Latitud = reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5))
                        });
                    }
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                try
                {
                    await using var cmd = oracleConn.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PK_ADMINISTRACION.P_GET_MUNICIPIOS_BY_DEPTO";
                    cmd.Parameters.Add("p_id_departamento", OracleDbType.Int64).Value = idDepartamento.HasValue ? idDepartamento.Value : DBNull.Value;
                    cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(new DtoMunicipio
                        {
                            IdMunicipio = GetInt64(reader, "ID_MUNICIPIO"),
                            IdDepartamento = GetInt64(reader, "ID_DEPARTAMENTO"),
                            CodigoDane = GetString(reader, "CODIGO_DANE"),
                            Nombre = GetString(reader, "NOMBRE"),
                            Longitud = GetNullableDecimal(reader, "LONGITUD"),
                            Latitud = GetNullableDecimal(reader, "LATITUD")
                        });
                    }
                }
                catch
                {
                    result.Clear();
                }
            }
            catch
            {
                // Fallback
            }

            return result;
        }

        private static long GetInt64(OracleDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return 0L;
            }

            return Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static string GetString(OracleDataReader reader, string column)
        {
            if (!HasColumn(reader, column))
            {
                return string.Empty;
            }

            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static decimal? GetNullableDecimal(OracleDataReader reader, string column)
        {
            if (!HasColumn(reader, column))
            {
                return null;
            }

            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static bool HasColumn(OracleDataReader reader, string column)
        {
            try
            {
                _ = reader.GetOrdinal(column);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }
    }
}
