using Comun.Dtos.Ubicacion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;

using Comun.Dtos.Administracion;

namespace Datos.Gestion
{
    public class DbUbicacionRepository : IDbUbicacionRepository
    {
        private readonly string _cs;

        public DbUbicacionRepository(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DbConnection")!;
        }

        public async Task<List<DtoUnidadOrg>> GetUnidadesOrganigramaAsync(CancellationToken ct)
        {
            var result = new List<DtoUnidadOrg>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
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

            return result;
        }

        public async Task<List<DtoDepartamento>> GetDepartamentosAsync(CancellationToken ct)
        {
            var result = new List<DtoDepartamento>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
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

            return result;
        }

        public async Task<List<DtoMunicipio>> GetMunicipiosByDepartamentoAsync(long? idDepartamento, CancellationToken ct)
        {
            var result = new List<DtoMunicipio>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
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
