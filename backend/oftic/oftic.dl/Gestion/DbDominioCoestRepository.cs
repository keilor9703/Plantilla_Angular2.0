using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion;

public class DbDominioCoestRepository : IDbDominioCoestRepository
{
    private readonly string _cs;
    private readonly ILogger<DbDominioCoestRepository> _logger;

    public DbDominioCoestRepository(IConfiguration configuration, ILogger<DbDominioCoestRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    public async Task<List<DtoDominioCoest>> GetByPadreIdAsync(decimal padreId, CancellationToken ct)
    {
        var resultados = new List<DtoDominioCoest>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT DOMINIO_ID, DESCRIPCION 
                FROM USR_COEST.COEST_CTRL_DOMINIO 
                WHERE PADRE_ID = :padreId AND VIGENTE = 1 
                ORDER BY DESCRIPCION";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("padreId", padreId));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                resultados.Add(new DtoDominioCoest
                {
                    DominioId = Convert.ToDecimal(reader["DOMINIO_ID"]),
                    Descripcion = reader.GetString(reader.GetOrdinal("DESCRIPCION"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetByPadreIdAsync para padreId {PadreId}", padreId);
        }
        return resultados;
    }

    public async Task<List<DtoCatalogoMedio>> GetMediosAsync(CancellationToken ct)
    {
        var resultados = new List<DtoCatalogoMedio>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT m.CTRL_MEDIOS_ID, m.DESCRIPCION, m.ID_TIPO_MEDIO, t.DESCRIPCION AS TIPO_MEDIO_STR 
                FROM USR_COEST.COEST_CTRL_MEDIOS m 
                LEFT JOIN USR_COEST.COEST_CTRL_DOMINIO t ON t.DOMINIO_ID = m.ID_TIPO_MEDIO 
                WHERE m.VIGENTE = 1 
                ORDER BY m.DESCRIPCION";

            await using var cmd = new OracleCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                resultados.Add(new DtoCatalogoMedio
                {
                    CtrlMediosId = reader.GetString(reader.GetOrdinal("CTRL_MEDIOS_ID")),
                    Descripcion = reader.GetString(reader.GetOrdinal("DESCRIPCION")),
                    TipoMedioId = Convert.ToDecimal(reader["ID_TIPO_MEDIO"]),
                    TipoMedioStr = reader.IsDBNull(reader.GetOrdinal("TIPO_MEDIO_STR")) ? null : reader.GetString(reader.GetOrdinal("TIPO_MEDIO_STR"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetMediosAsync");
        }
        return resultados;
    }

    public async Task<List<DtoCatalogoMedio>> GetMediosPorTipoYUnidadAsync(decimal tipoMedioId, decimal idUnidad, CancellationToken ct)
    {
        var resultados = new List<DtoCatalogoMedio>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT u.CTRL_MEDIOS_ID, m.DESCRIPCION, m.ID_TIPO_MEDIO 
                FROM USR_COEST.COEST_UNIDAD_MEDIOS u 
                INNER JOIN USR_COEST.COEST_CTRL_MEDIOS m ON m.CTRL_MEDIOS_ID = u.CTRL_MEDIOS_ID 
                WHERE u.VIGENTE = 1 
                  AND m.ID_TIPO_MEDIO = :tipoMedioId 
                  AND u.UNIDAD_ID = :idUnidad 
                ORDER BY m.DESCRIPCION";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(new OracleParameter("tipoMedioId", tipoMedioId));
            cmd.Parameters.Add(new OracleParameter("idUnidad", idUnidad));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                resultados.Add(new DtoCatalogoMedio
                {
                    CtrlMediosId = reader.GetString(reader.GetOrdinal("CTRL_MEDIOS_ID")),
                    Descripcion = reader.GetString(reader.GetOrdinal("DESCRIPCION")),
                    TipoMedioId = Convert.ToDecimal(reader["ID_TIPO_MEDIO"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetMediosPorTipoYUnidadAsync para tipoMedio {TipoMedioId} y unidad {IdUnidad}", tipoMedioId, idUnidad);
        }
        return resultados;
    }

    public async Task<List<DtoDependencia>> GetDependenciasAsync(CancellationToken ct)
    {
        var resultados = new List<DtoDependencia>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT DEPENDENCIA_ID, DESCRIPCION 
                FROM USR_COEST.COEST_DEPENDENCIA 
                WHERE VIGENTE = 1 
                ORDER BY DESCRIPCION";

            await using var cmd = new OracleCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                resultados.Add(new DtoDependencia
                {
                    DependenciaId = Convert.ToDecimal(reader["DEPENDENCIA_ID"]),
                    Descripcion = reader.GetString(reader.GetOrdinal("DESCRIPCION"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetDependenciasAsync");
        }
        return resultados;
    }

    public async Task<List<DtoDominioCoest>> GetUnidadesAsync(CancellationToken ct)
    {
        var resultados = new List<DtoDominioCoest>();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT MIN(CONSECUTIVO) AS CONSECUTIVO, SIGLA_PAPA 
                FROM USR_COEST.VM_UNIDADES 
                WHERE TIUN_CODIGO != 1 AND SIGLA_PAPA IS NOT NULL
                GROUP BY SIGLA_PAPA
                ORDER BY SIGLA_PAPA";

            await using var cmd = new OracleCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var sigla = reader.IsDBNull(reader.GetOrdinal("SIGLA_PAPA")) ? "" : reader.GetString(reader.GetOrdinal("SIGLA_PAPA"));
                if (string.IsNullOrWhiteSpace(sigla)) continue;

                resultados.Add(new DtoDominioCoest
                {
                    DominioId = Convert.ToDecimal(reader["CONSECUTIVO"]),
                    Descripcion = sigla
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetUnidadesAsync");
        }
        return resultados;
    }
}
