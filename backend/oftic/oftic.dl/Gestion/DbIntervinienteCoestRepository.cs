using Comun.Dtos.RecoleccionInformacion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Datos.Gestion;

public class DbIntervinienteCoestRepository : IDbIntervinienteCoestRepository
{
    private readonly string _cs;
    private readonly ILogger<DbIntervinienteCoestRepository> _logger;

    // DOMINIO_ID=55 es "Vocería Interna - Policías" (PADRE_ID=54)
    // Se usa como marcador genérico "Con Vocería"; el frontend puede seleccionar el tipo exacto
    private const int IdConVoceria = 55;

    public DbIntervinienteCoestRepository(IConfiguration configuration, ILogger<DbIntervinienteCoestRepository> logger)
    {
        _cs = configuration.GetConnectionString("DbCoest")!;
        _logger = logger;
    }

    public async Task<DtoIntervinienteCoestResult> CreateAsync(DtoIntervinienteCoestRequest request, string maquinaAuditoria, CancellationToken ct)
    {
        var result = new DtoIntervinienteCoestResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);
            await using var tx = conn.BeginTransaction();

            var newId = Guid.NewGuid().ToString();

            // Insertar interviniente
            const string sqlInsert = @"
                INSERT INTO USR_COEST.COEST_INTERVINIENTE (
                    INTERVINIENTE_ID, MEDIO_ID, GRADO, NOMBRE, CARGO,
                    IDENTIFICACION_CREACION, FECHA_CREACION, MAQUINA_CREACION, VIGENTE
                ) VALUES (
                    :intervinienteId, :medioId, :grado, :nombre, :cargo,
                    :identificacionCreacion, SYSDATE, :maquinaCreacion, 1
                )";

            await using var cmdInsert = new OracleCommand(sqlInsert, conn) { Transaction = tx };
            cmdInsert.BindByName = true;
            cmdInsert.Parameters.Add(new OracleParameter("intervinienteId",       newId));
            cmdInsert.Parameters.Add(new OracleParameter("medioId",               request.MedioId));
            cmdInsert.Parameters.Add(new OracleParameter("grado",                 (object?)request.Grado ?? DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("nombre",                (object?)request.Nombre?.ToUpper() ?? DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("cargo",                 (object?)request.Cargo?.ToUpper()  ?? DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("identificacionCreacion",(object?)request.IdentificacionCreacion ?? DBNull.Value));
            cmdInsert.Parameters.Add(new OracleParameter("maquinaCreacion",       maquinaAuditoria));
            await cmdInsert.ExecuteNonQueryAsync(ct);

            // Actualizar vocería del medio a "Con Vocería"
            const string sqlVoceria = @"
                UPDATE USR_COEST.COEST_MEDIO
                SET ID_VOCERIA = :idVoceria
                WHERE MEDIO_ID = :medioId";

            await using var cmdVoc = new OracleCommand(sqlVoceria, conn) { Transaction = tx };
            cmdVoc.BindByName = true;
            cmdVoc.Parameters.Add(new OracleParameter("idVoceria", IdConVoceria));
            cmdVoc.Parameters.Add(new OracleParameter("medioId",   request.MedioId));
            await cmdVoc.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);

            result.Success = true;
            result.Message = "Interviniente registrado correctamente.";
            result.Id      = newId;

            _logger.LogInformation("Interviniente creado {Id} para medio {MedioId}", newId, request.MedioId);
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "Oracle error en CreateAsync Interviniente");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en CreateAsync Interviniente");
        }

        return result;
    }

    public async Task<DtoIntervinienteCoestResult> DeleteAsync(string intervinienteId, long identificacionModifica, string maquinaModifica, CancellationToken ct)
    {
        var result = new DtoIntervinienteCoestResult();
        try
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"
                UPDATE USR_COEST.COEST_INTERVINIENTE
                SET VIGENTE = 0,
                    FECHA_MODIFICA = SYSDATE,
                    MAQUINA_MODIFICA = :maquina,
                    IDENTIFICACION_MODIFICA = :identificacion
                WHERE INTERVINIENTE_ID = :intervinienteId";

            await using var cmd = new OracleCommand(sql, conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(new OracleParameter("maquina",          maquinaModifica));
            cmd.Parameters.Add(new OracleParameter("identificacion",    identificacionModifica));
            cmd.Parameters.Add(new OracleParameter("intervinienteId",   intervinienteId));

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            result.Success = rows > 0;
            result.Message = result.Success ? "Interviniente eliminado." : "Interviniente no encontrado.";
            result.Id      = intervinienteId;
        }
        catch (OracleException ex)
        {
            result.Success = false;
            result.Message = $"Error Oracle: {ex.Message}";
            _logger.LogError(ex, "Oracle error en DeleteAsync Interviniente {Id}", intervinienteId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error general en DeleteAsync Interviniente {Id}", intervinienteId);
        }

        return result;
    }
}
