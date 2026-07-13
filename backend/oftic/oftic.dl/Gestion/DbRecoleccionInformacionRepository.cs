using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion;

public class DbHechoNoticiosoRepository : IDbHechoNoticiosoRepository
{
    private readonly string _cs;
    private readonly ILogger<DbHechoNoticiosoRepository> _logger;

    private const string SchemaCoest = "USR_COEST";

    // Caché en memoria de unidades policiales (Sigla y Descripción de Dependencia) para evitar cruzamientos repetidos de la vista lenta VM_UNIDADES
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<decimal, (string Sigla, string Descripcion)> _unidadesCache = new();

    public DbHechoNoticiosoRepository(IConfiguration configuration, ILogger<DbHechoNoticiosoRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    private async Task EnsureUnidadesCacheAsync(OracleConnection conn, CancellationToken ct)
    {
        if (!_unidadesCache.IsEmpty) return;

        try
        {
            _logger.LogInformation("Cargando caché de VM_UNIDADES para optimizar la consulta de Hechos Noticiosos...");
            const string sql = "SELECT CONSECUTIVO, SIGLA_FISICA, DESCRIPCION_DEPENDENCIA FROM USR_COEST.VM_UNIDADES";
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var consecutivo = Convert.ToDecimal(reader["CONSECUTIVO"]);
                var sigla = reader.IsDBNull(reader.GetOrdinal("SIGLA_FISICA")) ? "" : reader.GetString(reader.GetOrdinal("SIGLA_FISICA"));
                var desc = reader.IsDBNull(reader.GetOrdinal("DESCRIPCION_DEPENDENCIA")) ? "" : reader.GetString(reader.GetOrdinal("DESCRIPCION_DEPENDENCIA"));
                _unidadesCache[consecutivo] = (sigla, desc);
            }
            _logger.LogInformation("Caché de VM_UNIDADES cargada con éxito. Total registros: {Count}", _unidadesCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la caché de VM_UNIDADES. Se continuará con valores nulos.");
        }
    }

    public async Task<List<DtoHechoNoticioso>> GetAllAsync(int anio, CancellationToken ct)
    {
        var resultados = new List<DtoHechoNoticioso>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await EnsureUnidadesCacheAsync(conn, ct);

            const string sql = @"
                SELECT
                    n.NOTICIA_ID,
                    n.ID_UNIDAD,
                    NULL                            AS UNIDAD_STR,
                    n.TITULAR,
                    n.FECHA,
                    n.ID_CARACTER,
                    dc1.DESCRIPCION                 AS CARACTER_STR,
                    n.ID_TEMA,
                    dc3.DESCRIPCION                 AS TEMA_STR,
                    n.ID_DESC_TEMA,
                    dc2.DESCRIPCION                 AS DESC_TEMA_STR,
                    n.ID_COBERTURA,
                    dc4.DESCRIPCION                 AS COBERTURA_STR,
                    n.ID_TENDENCIA,
                    dc5.DESCRIPCION                 AS TENDENCIA_STR,
                    n.ID_DEPENDENCIA,
                    dep.DESCRIPCION                 AS DEPENDENCIA_STR,
                    dep.SIGLA                       AS SIGLA_DEPENDENCIA_STR,
                    n.ID_MODALIDAD,
                    dc6.DESCRIPCION                 AS MODALIDAD_STR,
                    n.ID_ESTACION,
                    NULL                            AS ESTACION_STR,
                    n.VIGENTE,
                    n.IDENTIFICACION_CREACION,
                    n.MAQUINA_CREACION,
                    n.FECHA_CREACION,
                    n.IDENTIFICACION_MODIFICA,
                    n.MAQUINA_MODIFICA,
                    n.FECHA_MODIFICA,
                    (SELECT COUNT(*) FROM USR_COEST.COEST_MEDIO m WHERE m.NOTICIA_ID = n.NOTICIA_ID AND m.VIGENTE = 1) AS CONTADOR_MEDIOS
                FROM USR_COEST.COEST_NOTICIA n
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc1 ON dc1.DOMINIO_ID = n.ID_CARACTER
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc2 ON dc2.DOMINIO_ID = n.ID_DESC_TEMA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc3 ON dc3.DOMINIO_ID = n.ID_TEMA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc4 ON dc4.DOMINIO_ID = n.ID_COBERTURA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc5 ON dc5.DOMINIO_ID = n.ID_TENDENCIA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc6 ON dc6.DOMINIO_ID = n.ID_MODALIDAD
                LEFT JOIN USR_COEST.COEST_DEPENDENCIA dep  ON dep.DEPENDENCIA_ID = n.ID_DEPENDENCIA
                WHERE n.VIGENTE = 1
                  AND EXTRACT(YEAR FROM n.FECHA_CREACION) = :anio
                ORDER BY n.FECHA_CREACION DESC";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("anio", anio));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var hecho = MapHecho(reader);
                // Resolver nombres de unidades y estaciones desde caché en memoria de C#
                if (_unidadesCache.TryGetValue(hecho.IdUnidad, out var uInfo))
                {
                    hecho.UnidadStr = uInfo.Sigla;
                }
                if (hecho.IdEstacion.HasValue && _unidadesCache.TryGetValue(hecho.IdEstacion.Value, out var eInfo))
                {
                    hecho.EstacionStr = eInfo.Descripcion;
                }
                resultados.Add(hecho);
            }

            _logger.LogInformation("GetAllAsync HechoNoticioso devolvió {Count} registros año {Anio}", resultados.Count, anio);
        }
        catch (OracleException ex) { _logger.LogError(ex, "Oracle error en GetAllAsync HechoNoticioso"); }
        catch (Exception ex)       { _logger.LogError(ex, "Error general en GetAllAsync HechoNoticioso"); }

        return resultados;
    }

    public async Task<List<DtoHechoNoticioso>> GetByUnidadAsync(string siglaUnidad, int anio, CancellationToken ct)
    {
        // Se reutiliza la lógica optimizada de GetAllAsync y se filtra en memoria
        var hechos = await GetAllAsync(anio, ct);
        return hechos.Where(h =>
            (h.UnidadStr != null && h.UnidadStr.Equals(siglaUnidad, StringComparison.OrdinalIgnoreCase)) ||
            (h.SiglaDependenciaStr != null && h.SiglaDependenciaStr.Equals(siglaUnidad, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public async Task<DtoHechoNoticioso?> GetByIdAsync(string id, CancellationToken ct)
    {
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await EnsureUnidadesCacheAsync(conn, ct);

            const string sql = @"
                SELECT
                    n.NOTICIA_ID,
                    n.ID_UNIDAD,
                    NULL                            AS UNIDAD_STR,
                    n.TITULAR,
                    n.FECHA,
                    n.ID_CARACTER,
                    dc1.DESCRIPCION                 AS CARACTER_STR,
                    n.ID_TEMA,
                    dc3.DESCRIPCION                 AS TEMA_STR,
                    n.ID_DESC_TEMA,
                    dc2.DESCRIPCION                 AS DESC_TEMA_STR,
                    n.ID_COBERTURA,
                    dc4.DESCRIPCION                 AS COBERTURA_STR,
                    n.ID_TENDENCIA,
                    dc5.DESCRIPCION                 AS TENDENCIA_STR,
                    n.ID_DEPENDENCIA,
                    dep.DESCRIPCION                 AS DEPENDENCIA_STR,
                    dep.SIGLA                       AS SIGLA_DEPENDENCIA_STR,
                    n.ID_MODALIDAD,
                    dc6.DESCRIPCION                 AS MODALIDAD_STR,
                    n.ID_ESTACION,
                    NULL                            AS ESTACION_STR,
                    n.VIGENTE,
                    n.IDENTIFICACION_CREACION,
                    n.MAQUINA_CREACION,
                    n.FECHA_CREACION,
                    n.IDENTIFICACION_MODIFICA,
                    n.MAQUINA_MODIFICA,
                    n.FECHA_MODIFICA,
                    (SELECT COUNT(*) FROM USR_COEST.COEST_MEDIO m WHERE m.NOTICIA_ID = n.NOTICIA_ID AND m.VIGENTE = 1) AS CONTADOR_MEDIOS
                FROM USR_COEST.COEST_NOTICIA n
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc1 ON dc1.DOMINIO_ID = n.ID_CARACTER
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc2 ON dc2.DOMINIO_ID = n.ID_DESC_TEMA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc3 ON dc3.DOMINIO_ID = n.ID_TEMA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc4 ON dc4.DOMINIO_ID = n.ID_COBERTURA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc5 ON dc5.DOMINIO_ID = n.ID_TENDENCIA
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO dc6 ON dc6.DOMINIO_ID = n.ID_MODALIDAD
                LEFT JOIN USR_COEST.COEST_DEPENDENCIA dep  ON dep.DEPENDENCIA_ID = n.ID_DEPENDENCIA
                WHERE n.VIGENTE = 1
                  AND n.NOTICIA_ID = :id";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("id", id));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var hecho = MapHecho(reader);
                // Resolver nombres de unidades y estaciones desde caché en memoria de C#
                if (_unidadesCache.TryGetValue(hecho.IdUnidad, out var uInfo))
                {
                    hecho.UnidadStr = uInfo.Sigla;
                }
                if (hecho.IdEstacion.HasValue && _unidadesCache.TryGetValue(hecho.IdEstacion.Value, out var eInfo))
                {
                    hecho.EstacionStr = eInfo.Descripcion;
                }
                return hecho;
            }
        }
        catch (OracleException ex) { _logger.LogError(ex, "Oracle error en GetByIdAsync HechoNoticioso {Id}", id); }
        catch (Exception ex)       { _logger.LogError(ex, "Error general en GetByIdAsync HechoNoticioso {Id}", id); }

        return null;
    }

    public async Task<List<DtoAnioItem>> GetAniosAsync(CancellationToken ct)
    {
        var resultados = new List<DtoAnioItem>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT DISTINCT EXTRACT(YEAR FROM FECHA_CREACION) AS ANIO
                FROM USR_COEST.COEST_NOTICIA
                WHERE VIGENTE = 1
                ORDER BY ANIO DESC";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                resultados.Add(new DtoAnioItem { Anio = Convert.ToInt32(reader["ANIO"]) });
        }
        catch (OracleException ex) { _logger.LogError(ex, "Oracle error en GetAniosAsync"); }
        catch (Exception ex)       { _logger.LogError(ex, "Error general en GetAniosAsync"); }

        return resultados;
    }

    public async Task<DtoHechoNoticiosoResult> CreateAsync(DtoHechoNoticiosoRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var result = new DtoHechoNoticiosoResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            var newId = Guid.NewGuid().ToString();

            const string sql = @"
                INSERT INTO USR_COEST.COEST_NOTICIA (
                    NOTICIA_ID, ID_UNIDAD, ID_DEPENDENCIA, TITULAR,
                    ID_CARACTER, ID_TEMA, ID_DESC_TEMA, ID_COBERTURA, ID_TENDENCIA,
                    ID_MODALIDAD, ID_ESTACION,
                    IDENTIFICACION_CREACION, FECHA_CREACION, MAQUINA_CREACION, VIGENTE
                ) VALUES (
                    :noticiaId, :idUnidad, :idDependencia, :titular,
                    :idCaracter, :idTema, :idDescTema, :idCobertura, :idTendencia,
                    :idModalidad, :idEstacion,
                    :identificacionCreacion, SYSDATE, :maquinaCreacion, 1
                )";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.BindByName = true;
            cmd.Parameters.Add(new OracleParameter("noticiaId",             newId));
            cmd.Parameters.Add(new OracleParameter("idUnidad",              request.IdUnidad));
            cmd.Parameters.Add(new OracleParameter("idDependencia",         (object?)request.IdDependencia ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("titular",               request.Titular.ToUpper()));
            cmd.Parameters.Add(new OracleParameter("idCaracter",            (object?)request.IdCaracter    ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idTema",                (object?)request.IdTema        ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idDescTema",            (object?)request.IdDescTema    ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idCobertura",           (object?)request.IdCobertura   ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idTendencia",           (object?)request.IdTendencia   ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idModalidad",           (object?)request.IdModalidad   ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("idEstacion",            (object?)request.IdEstacion    ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("identificacionCreacion",(object?)request.IdentificacionCreacion ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("maquinaCreacion",       maquinaAuditoria));

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            result.Success = rows > 0;
            result.Message = result.Success ? "Hecho noticioso registrado correctamente." : "No se pudo registrar el hecho noticioso.";
            result.Id      = result.Success ? newId : null;

            _logger.LogInformation("CreateAsync HechoNoticioso: {Success} — Id: {Id}", result.Success, newId);
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "Oracle error en CreateAsync HechoNoticioso");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en CreateAsync HechoNoticioso");
        }

        return result;
    }

    public async Task<DtoHechoNoticiosoResult> DeleteAsync(string id, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        var result = new DtoHechoNoticiosoResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = conn.BeginTransaction();

            // Inactivar intervinientes asociados (cascada lógica)
            const string sqlIntervinientes = @"
                UPDATE USR_COEST.COEST_INTERVINIENTE i
                SET i.VIGENTE = 0,
                    i.FECHA_MODIFICA = SYSDATE,
                    i.MAQUINA_MODIFICA = :maquina,
                    i.IDENTIFICACION_MODIFICA = :identificacion
                WHERE i.MEDIO_ID IN (
                    SELECT m.MEDIO_ID FROM USR_COEST.COEST_MEDIO m WHERE m.NOTICIA_ID = :noticiaId
                )";

            await using var cmdInt = new OracleCommand(sqlIntervinientes, conn) { Transaction = tx };
            cmdInt.BindByName = true;
            cmdInt.Parameters.Add(new OracleParameter("maquina",        maquinaModifica));
            cmdInt.Parameters.Add(new OracleParameter("identificacion",  identificacionModifica));
            cmdInt.Parameters.Add(new OracleParameter("noticiaId",      id));
            await cmdInt.ExecuteNonQueryAsync(ct);

            // Inactivar medios asociados
            const string sqlMedios = @"
                UPDATE USR_COEST.COEST_MEDIO m
                SET m.VIGENTE = 0,
                    m.FECHA_MODIFICA = SYSDATE,
                    m.MAQUINA_MODIFICA = :maquina,
                    m.IDENTIFICACION_MODIFICA = :identificacion
                WHERE m.NOTICIA_ID = :noticiaId";

            await using var cmdMed = new OracleCommand(sqlMedios, conn) { Transaction = tx };
            cmdMed.BindByName = true;
            cmdMed.Parameters.Add(new OracleParameter("maquina",        maquinaModifica));
            cmdMed.Parameters.Add(new OracleParameter("identificacion",  identificacionModifica));
            cmdMed.Parameters.Add(new OracleParameter("noticiaId",      id));
            await cmdMed.ExecuteNonQueryAsync(ct);

            // Inactivar la noticia
            const string sqlNoticia = @"
                UPDATE USR_COEST.COEST_NOTICIA n
                SET n.VIGENTE = 0,
                    n.FECHA_MODIFICA = SYSDATE,
                    n.MAQUINA_MODIFICA = :maquina,
                    n.IDENTIFICACION_MODIFICA = :identificacion
                WHERE n.NOTICIA_ID = :noticiaId";

            await using var cmdNot = new OracleCommand(sqlNoticia, conn) { Transaction = tx };
            cmdNot.BindByName = true;
            cmdNot.Parameters.Add(new OracleParameter("maquina",        maquinaModifica));
            cmdNot.Parameters.Add(new OracleParameter("identificacion",  identificacionModifica));
            cmdNot.Parameters.Add(new OracleParameter("noticiaId",      id));
            var rows = await cmdNot.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            result.Success = rows > 0;
            result.Message = result.Success ? "Hecho noticioso eliminado correctamente." : "No se encontró el registro.";
            result.Id      = id;
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "Oracle error en DeleteAsync HechoNoticioso {Id}", id);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en DeleteAsync HechoNoticioso {Id}", id);
        }

        return result;
    }

    private static DtoHechoNoticioso MapHecho(OracleDataReader r)
    {
        string? NullStr(string col)
        {
            var ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : r.GetString(ord);
        }

        decimal? NullDecimal(string col)
        {
            var ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : Convert.ToDecimal(r.GetValue(ord));
        }

        long? NullLong(string col)
        {
            var ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : Convert.ToInt64(r.GetValue(ord));
        }

        DateTime? NullDateTime(string col)
        {
            var ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : r.GetDateTime(ord);
        }

        bool? NullBool(string col)
        {
            var ord = r.GetOrdinal(col);
            if (r.IsDBNull(ord)) return null;
            var val = r.GetValue(ord);
            return Convert.ToInt32(val) == 1;
        }

        return new DtoHechoNoticioso
        {
            NoticiaId              = r.GetString(r.GetOrdinal("NOTICIA_ID")),
            IdUnidad               = Convert.ToDecimal(r["ID_UNIDAD"]),
            UnidadStr              = NullStr("UNIDAD_STR"),
            Titular                = r.GetString(r.GetOrdinal("TITULAR")),
            Fecha                  = NullDateTime("FECHA"),
            IdCaracter             = NullDecimal("ID_CARACTER"),
            CaracterStr            = NullStr("CARACTER_STR"),
            IdTema                 = NullDecimal("ID_TEMA"),
            TemaStr                = NullStr("TEMA_STR"),
            IdDescTema             = NullDecimal("ID_DESC_TEMA"),
            DescTemaStr            = NullStr("DESC_TEMA_STR"),
            IdCobertura            = NullDecimal("ID_COBERTURA"),
            CoberturaStr           = NullStr("COBERTURA_STR"),
            IdTendencia            = NullDecimal("ID_TENDENCIA"),
            TendenciaStr           = NullStr("TENDENCIA_STR"),
            IdDependencia          = NullDecimal("ID_DEPENDENCIA"),
            DependenciaStr         = NullStr("DEPENDENCIA_STR"),
            SiglaDependenciaStr    = NullStr("SIGLA_DEPENDENCIA_STR"),
            IdModalidad            = NullDecimal("ID_MODALIDAD"),
            ModalidadStr           = NullStr("MODALIDAD_STR"),
            IdEstacion             = NullDecimal("ID_ESTACION"),
            EstacionStr            = NullStr("ESTACION_STR"),
            Vigente                = NullBool("VIGENTE"),
            ContadorMedios         = r.IsDBNull(r.GetOrdinal("CONTADOR_MEDIOS")) ? 0 : Convert.ToInt32(r["CONTADOR_MEDIOS"]),
            IdentificacionCreacion = NullLong("IDENTIFICACION_CREACION"),
            MaquinaCreacion        = NullStr("MAQUINA_CREACION"),
            FechaCreacion          = NullDateTime("FECHA_CREACION"),
            IdentificacionModifica = NullLong("IDENTIFICACION_MODIFICA"),
            MaquinaModifica        = NullStr("MAQUINA_MODIFICA"),
            FechaModifica          = NullDateTime("FECHA_MODIFICA"),
        };
    }
}
