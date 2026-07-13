using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion;

public class DbMedioCoestRepository : IDbMedioCoestRepository
{
    private readonly string _cs;
    private readonly ILogger<DbMedioCoestRepository> _logger;

    // DOMINIO_ID=3 es "Sin vocería" en COEST_CTRL_DOMINIO (PADRE_ID=1)
    private const int IdSinVoceria = 3;

    public DbMedioCoestRepository(IConfiguration configuration, ILogger<DbMedioCoestRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    public async Task<List<DtoMedioCoest>> GetByNoticiaAsync(string noticiaId, CancellationToken ct)
    {
        var resultados = new List<DtoMedioCoest>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT
                    m.MEDIO_ID,
                    m.NOTICIA_ID,
                    m.ID_MEDIO,
                    dm.DESCRIPCION      AS MEDIO_STR,
                    m.ID_DESC_MEDIO,
                    cm.DESCRIPCION      AS DESC_MEDIO_STR,
                    m.TITULAR,
                    m.URL,
                    m.FECHA,
                    m.ID_VOCERIA,
                    dv.DESCRIPCION      AS VOCERIA_STR,
                    m.ID_REPLICA,
                    dr.DESCRIPCION      AS REPLICA_STR,
                    m.CANTIDAD_REPLICA,
                    m.ID_EVIDENCIA,
                    de.DESCRIPCION      AS EVIDENCIA_STR,
                    m.DURACION,
                    m.VIGENTE,
                    m.IDENTIFICACION_CREACION,
                    m.MAQUINA_CREACION,
                    m.FECHA_CREACION
                FROM USR_COEST.COEST_MEDIO m
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dm ON dm.DOMINIO_ID = m.ID_MEDIO
                LEFT JOIN USR_COEST.COEST_CTRL_MEDIOS  cm ON cm.CTRL_MEDIOS_ID = m.ID_DESC_MEDIO
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dv ON dv.DOMINIO_ID = m.ID_VOCERIA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dr ON dr.DOMINIO_ID = m.ID_REPLICA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO de ON de.DOMINIO_ID = m.ID_EVIDENCIA
                WHERE m.NOTICIA_ID = :noticiaId
                  AND m.VIGENTE = 1
                ORDER BY m.FECHA_CREACION DESC";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("noticiaId", noticiaId));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var medio = MapMedio(reader);

                // Cargar intervinientes de este medio
                medio.Intervinientes = await GetIntervinientesByMedioAsync(conn, medio.MedioId, ct);
                resultados.Add(medio);
            }
        }
        catch (OracleException ex) { _logger.LogError(ex, "Oracle error en GetByNoticiaAsync Medios {NoticiaId}", noticiaId); }
        catch (Exception ex)       { _logger.LogError(ex, "Error general en GetByNoticiaAsync Medios {NoticiaId}", noticiaId); }

        return resultados;
    }

    public async Task<DtoMedioCoestResult> CreateAsync(DtoMedioCoestRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var result = new DtoMedioCoestResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            var newId = Guid.NewGuid().ToString();

            const string sql = @"
                INSERT INTO USR_COEST.COEST_MEDIO (
                    MEDIO_ID, NOTICIA_ID, ID_MEDIO, ID_DESC_MEDIO,
                    ID_VOCERIA, ID_EVIDENCIA, TITULAR, URL, FECHA,
                    IDENTIFICACION_CREACION, FECHA_CREACION, MAQUINA_CREACION,
                    VIGENTE, DURACION
                ) VALUES (
                    :medioId, :noticiaId, :idMedio, :idDescMedio,
                    :idVoceria, :idEvidencia, :titular, :url, :fecha,
                    :identificacionCreacion, SYSDATE, :maquinaCreacion,
                    1, :duracion
                )";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.BindByName = true;
            cmd.Parameters.Add(new OracleParameter("medioId",               newId));
            cmd.Parameters.Add(new OracleParameter("noticiaId",             request.NoticiaId));
            cmd.Parameters.Add(new OracleParameter("idMedio",               (object?)request.IdMedio    ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idDescMedio",           (object?)request.IdDescMedio ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idVoceria",             IdSinVoceria));
            cmd.Parameters.Add(new OracleParameter("idEvidencia",           (object?)request.IdEvidencia ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("titular",               string.IsNullOrWhiteSpace(request.Titular) ? DBNull.Value : (object)request.Titular.ToUpper()));
            cmd.Parameters.Add(new OracleParameter("url",                   (object?)request.Url         ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("fecha",                 (object?)request.Fecha       ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("identificacionCreacion",(object?)request.IdentificacionCreacion ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("maquinaCreacion",       maquinaAuditoria));
            cmd.Parameters.Add(new OracleParameter("duracion",              (object?)request.Duracion    ?? DBNull.Value));

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            result.Success = rows > 0;
            result.Message = result.Success ? "Medio registrado correctamente." : "No se pudo registrar el medio.";
            result.Id      = result.Success ? newId : null;
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "Oracle error en CreateAsync Medio");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en CreateAsync Medio");
        }

        return result;
    }

    public async Task<DtoMedioCoestResult> DeleteAsync(string medioId, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        var result = new DtoMedioCoestResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = conn.BeginTransaction();

            // Inactivar intervinientes
            const string sqlInt = @"
                UPDATE USR_COEST.COEST_INTERVINIENTE
                SET VIGENTE = 0,
                    FECHA_MODIFICA = SYSDATE,
                    MAQUINA_MODIFICA = :maquina,
                    IDENTIFICACION_MODIFICA = :identificacion
                WHERE MEDIO_ID = :medioId";

            await using var cmdInt = new OracleCommand(sqlInt, conn) { Transaction = tx };
            cmdInt.BindByName = true;
            cmdInt.Parameters.Add(new OracleParameter("maquina",        maquinaModifica));
            cmdInt.Parameters.Add(new OracleParameter("identificacion",  identificacionModifica));
            cmdInt.Parameters.Add(new OracleParameter("medioId",        medioId));
            await cmdInt.ExecuteNonQueryAsync(ct);

            // Inactivar medio
            const string sqlMed = @"
                UPDATE USR_COEST.COEST_MEDIO
                SET VIGENTE = 0,
                    FECHA_MODIFICA = SYSDATE,
                    MAQUINA_MODIFICA = :maquina,
                    IDENTIFICACION_MODIFICA = :identificacion
                WHERE MEDIO_ID = :medioId";

            await using var cmdMed = new OracleCommand(sqlMed, conn) { Transaction = tx };
            cmdMed.BindByName = true;
            cmdMed.Parameters.Add(new OracleParameter("maquina",        maquinaModifica));
            cmdMed.Parameters.Add(new OracleParameter("identificacion",  identificacionModifica));
            cmdMed.Parameters.Add(new OracleParameter("medioId",        medioId));
            var rows = await cmdMed.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            result.Success = rows > 0;
            result.Message = result.Success ? "Medio eliminado correctamente." : "No se encontró el medio.";
            result.Id      = medioId;
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "Oracle error en DeleteAsync Medio {Id}", medioId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en DeleteAsync Medio {Id}", medioId);
        }

        return result;
    }

    public async Task<DtoMedioCoestResult> ActualizarVoceriaAsync(string medioId, decimal idVoceria, CancellationToken ct)
    {
        var result = new DtoMedioCoestResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                UPDATE USR_COEST.COEST_MEDIO
                SET ID_VOCERIA = :idVoceria
                WHERE MEDIO_ID = :medioId";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(new OracleParameter("idVoceria", idVoceria));
            cmd.Parameters.Add(new OracleParameter("medioId",   medioId));

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            result.Success = rows > 0;
            result.Message = result.Success ? "Voc\u00eda actualizada." : "Medio no encontrado.";
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error Oracle: {ex.Message}";
            _logger.LogError(ex, "Oracle error en ActualizarVoceriaAsync {MedioId}", medioId);
        }

        return result;
    }

    private static async Task<List<DtoIntervinienteCoest>> GetIntervinientesByMedioAsync(OracleConnection conn, string medioId, CancellationToken ct)
    {
        var lista = new List<DtoIntervinienteCoest>();

        const string sql = @"
            SELECT
                i.INTERVINIENTE_ID,
                i.MEDIO_ID,
                i.GRADO,
                dg.DESCRIPCION AS GRADO_STR,
                i.NOMBRE,
                i.CARGO,
                i.VIGENTE,
                i.IDENTIFICACION_CREACION,
                i.MAQUINA_CREACION,
                i.FECHA_CREACION
            FROM USR_COEST.COEST_INTERVINIENTE i
            LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dg ON dg.DOMINIO_ID = i.GRADO
            WHERE i.MEDIO_ID = :medioId AND i.VIGENTE = 1";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("medioId", medioId));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new DtoIntervinienteCoest
            {
                IntervinienteId        = reader.GetString(reader.GetOrdinal("INTERVINIENTE_ID")),
                MedioId                = reader.GetString(reader.GetOrdinal("MEDIO_ID")),
                Grado                  = reader.IsDBNull(reader.GetOrdinal("GRADO")) ? null : Convert.ToDecimal(reader["GRADO"]),
                GradoStr               = reader.IsDBNull(reader.GetOrdinal("GRADO_STR")) ? null : reader.GetString(reader.GetOrdinal("GRADO_STR")),
                Nombre                 = reader.IsDBNull(reader.GetOrdinal("NOMBRE")) ? null : reader.GetString(reader.GetOrdinal("NOMBRE")),
                Cargo                  = reader.IsDBNull(reader.GetOrdinal("CARGO")) ? null : reader.GetString(reader.GetOrdinal("CARGO")),
                Vigente                = reader.IsDBNull(reader.GetOrdinal("VIGENTE")) ? null : Convert.ToBoolean(reader["VIGENTE"]),
                IdentificacionCreacion = reader.IsDBNull(reader.GetOrdinal("IDENTIFICACION_CREACION")) ? null : reader.GetInt64(reader.GetOrdinal("IDENTIFICACION_CREACION")),
                MaquinaCreacion        = reader.IsDBNull(reader.GetOrdinal("MAQUINA_CREACION")) ? null : reader.GetString(reader.GetOrdinal("MAQUINA_CREACION")),
                FechaCreacion          = reader.IsDBNull(reader.GetOrdinal("FECHA_CREACION")) ? null : reader.GetDateTime(reader.GetOrdinal("FECHA_CREACION")),
            });
        }

        return lista;
    }

    private static DtoMedioCoest MapMedio(OracleDataReader r)
    {
        string? NullStr(string col) { var o = r.GetOrdinal(col); return r.IsDBNull(o) ? null : r.GetString(o); }
        T? Nul<T>(string col) where T : struct { var o = r.GetOrdinal(col); return r.IsDBNull(o) ? null : (T?)Convert.ChangeType(r.GetValue(o), typeof(T)); }

        return new DtoMedioCoest
        {
            MedioId               = r.GetString(r.GetOrdinal("MEDIO_ID")),
            NoticiaId             = r.GetString(r.GetOrdinal("NOTICIA_ID")),
            IdMedio               = Nul<decimal>("ID_MEDIO"),
            MedioStr              = NullStr("MEDIO_STR"),
            IdDescMedio           = NullStr("ID_DESC_MEDIO"),
            DescMedioStr          = NullStr("DESC_MEDIO_STR"),
            Titular               = NullStr("TITULAR"),
            Url                   = NullStr("URL"),
            Fecha                 = Nul<DateTime>("FECHA"),
            IdVoceria             = Nul<decimal>("ID_VOCERIA"),
            VoceriaStr            = NullStr("VOCERIA_STR"),
            IdReplica             = Nul<decimal>("ID_REPLICA"),
            ReplicaStr            = NullStr("REPLICA_STR"),
            CantidadReplica       = Nul<decimal>("CANTIDAD_REPLICA"),
            IdEvidencia           = Nul<decimal>("ID_EVIDENCIA"),
            EvidenciaStr          = NullStr("EVIDENCIA_STR"),
            Duracion              = Nul<decimal>("DURACION"),
            Vigente               = Nul<bool>("VIGENTE"),
            IdentificacionCreacion= Nul<long>("IDENTIFICACION_CREACION"),
            MaquinaCreacion       = NullStr("MAQUINA_CREACION"),
            FechaCreacion         = Nul<DateTime>("FECHA_CREACION"),
        };
    }
}
