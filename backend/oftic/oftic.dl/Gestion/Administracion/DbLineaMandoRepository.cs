using Comun.Dtos.Auditoria;
using Comun.Dtos.LineasMando;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Datos.Gestion
{
    public class DbLineaMandoRepository : IDbLineaMandoRepository
    {
        private readonly string _cs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbLineaMandoRepository> _logger;
        private readonly IDbAuditoriaRepository _auditoriaRepo;

        public DbLineaMandoRepository(IConfiguration configuration, ILogger<DbLineaMandoRepository> logger, IDbAuditoriaRepository auditoriaRepo)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
            _auditoriaRepo = auditoriaRepo;
        }

        public async Task<List<DtoLineaMando>> GetAllAsync(CancellationToken ct)
        {
            var result = new List<DtoLineaMando>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_linea_mando, identificacion, nombre, apellidos, grado, cargo, unidad, foto_base64, orden, vigente, peso
                FROM ctr_linea_mando
                WHERE vigente = 1
                ORDER BY orden, id_linea_mando";

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapLineaMandoGeneric(reader));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar línea de mando en MySQL. Se retorna lista vacía.");
                }
                return result;
            }

            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapLineaMandoGeneric(reader));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar línea de mando en PostgreSQL. Se retorna lista vacía.");
                }
                return result;
            }

            try
            {
                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                await using var cmdQuery = oracleConn.CreateCommand();
                cmdQuery.CommandText = selectSql;
                await using var readerQuery = await cmdQuery.ExecuteReaderAsync(ct);
                while (await readerQuery.ReadAsync(ct))
                {
                    result.Add(MapLineaMandoGeneric(readerQuery));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar línea de mando en Oracle. Se retorna lista vacía.");
            }
            return result;
        }

        public async Task<DtoLineaMando?> GetByIdAsync(long id, CancellationToken ct)
        {
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id_linea_mando, identificacion, nombre, apellidos, grado, cargo, unidad, foto_base64, orden, vigente, peso
                    FROM ctr_linea_mando
                    WHERE id_linea_mando = :pIdLineaMando";
                cmd.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = id;

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return MapLineaMandoGeneric(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar línea de mando por id={Id} en Oracle.", id);
            }

            return null;
        }

        public async Task<DtoLineaMando?> GetByIdentificacionAsync(string identificacion, CancellationToken ct)
        {
            try
            {
                await using var conn = new OracleConnection(_cs);
                await conn.OpenAsync(ct);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id_linea_mando, identificacion, nombre, apellidos, grado, cargo, unidad, foto_base64, orden, vigente, peso
                    FROM ctr_linea_mando
                    WHERE identificacion = :pIdentificacion";
                cmd.Parameters.Add("pIdentificacion", OracleDbType.Varchar2).Value = identificacion;

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return MapLineaMandoGeneric(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar línea de mando por identificación en Oracle.");
            }

            return null;
        }

        public async Task<DtoLineaMandoResult> CreateAsync(DtoLineaMandoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoLineaMandoResult();

            // Buscar duplicados activos (Mismo Peso)
            var all = await GetAllAsync(ct);
            var duplicated = all.FirstOrDefault(x => 
                !string.IsNullOrEmpty(x.Peso) &&
                !string.IsNullOrEmpty(request.Peso) &&
                request.Peso.Trim().StartsWith(x.Peso.Trim(), StringComparison.OrdinalIgnoreCase) 
                && x.Vigente == 1);

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                if (duplicated != null && !string.IsNullOrEmpty(request.Peso))
                {
                    _logger.LogInformation("Desactivando registro anterior ID {Id} para el peso {Peso}", duplicated.IdLineaMando, request.Peso);
                    await using var cmdDesactivar = conn.CreateCommand();
                    cmdDesactivar.Transaction = (OracleTransaction)transaction;
                    cmdDesactivar.CommandText = @"
                        UPDATE ctr_linea_mando
                           SET vigente = 0,
                               usuario_mod = :pUsuarioMod,
                               fecha_mod = SYSDATE,
                               maquina_mod = :pMaquinaMod
                         WHERE id_linea_mando = :pIdLineaMando";
                    
                    var uStr = usuarioAuditoria.ToString();
                    var mStr = maquinaAuditoria ?? "N/A";
                    cmdDesactivar.Parameters.Add("pUsuarioMod", OracleDbType.Varchar2, 50).Value = uStr.Length > 50 ? uStr.Substring(0, 50) : uStr;
                    cmdDesactivar.Parameters.Add("pMaquinaMod", OracleDbType.Varchar2, 100).Value = mStr.Length > 100 ? mStr.Substring(0, 100) : mStr;
                    cmdDesactivar.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = duplicated.IdLineaMando;
                    
                    await cmdDesactivar.ExecuteNonQueryAsync(ct);
                }

                _logger.LogInformation("Insertando registro en CTR_LINEA_MANDO directamente. Identificacion={Ident}, Nombre={Nombre}, Cargo={Cargo}",
                    request.Identificacion, request.Nombre, request.Cargo);

                long idNew = 1;
                await using (var cmdGetId = conn.CreateCommand())
                {
                    cmdGetId.Transaction = (OracleTransaction)transaction;
                    cmdGetId.CommandText = "SELECT NVL(MAX(id_linea_mando), 0) + 1 FROM ctr_linea_mando";
                    var val = await cmdGetId.ExecuteScalarAsync(ct);
                    if (val != null && val != DBNull.Value)
                    {
                        idNew = Convert.ToInt64(val);
                    }
                }

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.CommandText = @"
                    INSERT INTO ctr_linea_mando (
                        id_linea_mando, identificacion, nombre, apellidos, grado, cargo, orden, vigente, peso, unidad, foto_base64,
                        usuario_crea, fecha_crea, maquina_crea
                    ) VALUES (
                        :pIdLineaMando, :pIdentificacion, :pNombre, :pApellidos, :pGrado, :pCargo, :pOrden, 1, :pPeso, :pUnidad, :pFotoBase64,
                        :pUsuarioCrea, SYSDATE, :pMaquinaCrea
                    )";

                cmd.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = idNew;
                cmd.Parameters.Add("pIdentificacion", OracleDbType.Varchar2, 20).Value = (request.Identificacion ?? string.Empty).Length > 20 ? (request.Identificacion ?? string.Empty).Substring(0, 20) : (request.Identificacion ?? string.Empty);
                cmd.Parameters.Add("pNombre", OracleDbType.Varchar2, 100).Value = (request.Nombre ?? string.Empty).Length > 100 ? (request.Nombre ?? string.Empty).Substring(0, 100) : (request.Nombre ?? string.Empty);
                cmd.Parameters.Add("pApellidos", OracleDbType.Varchar2, 100).Value = (request.Apellidos ?? string.Empty).Length > 100 ? (request.Apellidos ?? string.Empty).Substring(0, 100) : (request.Apellidos ?? string.Empty);
                cmd.Parameters.Add("pGrado", OracleDbType.Varchar2, 50).Value = (request.Grado ?? string.Empty).Length > 50 ? (request.Grado ?? string.Empty).Substring(0, 50) : (request.Grado ?? string.Empty);
                cmd.Parameters.Add("pCargo", OracleDbType.Varchar2, 100).Value = (request.Cargo ?? string.Empty).Length > 100 ? (request.Cargo ?? string.Empty).Substring(0, 100) : (request.Cargo ?? string.Empty);
                cmd.Parameters.Add("pOrden", OracleDbType.Int32).Value = request.Orden;
                cmd.Parameters.Add("pPeso", OracleDbType.Varchar2, 10).Value = (request.Peso ?? string.Empty).Length > 10 ? (request.Peso ?? string.Empty).Substring(0, 10) : (request.Peso ?? string.Empty);
                cmd.Parameters.Add("pUnidad", OracleDbType.Varchar2, 200).Value = (request.Unidad ?? string.Empty).Length > 200 ? (request.Unidad ?? string.Empty).Substring(0, 200) : (request.Unidad ?? string.Empty);

                var fotoNormalizada = NormalizeBase64(request.FotoBase64);
                var clobParam = new OracleParameter("pFotoBase64", OracleDbType.Clob);
                clobParam.Value = !string.IsNullOrEmpty(fotoNormalizada) ? (object)fotoNormalizada : DBNull.Value;
                cmd.Parameters.Add(clobParam);

                var usuarioStr = usuarioAuditoria.ToString();
                var maquinaStr = maquinaAuditoria ?? "N/A";
                cmd.Parameters.Add("pUsuarioCrea", OracleDbType.Varchar2, 50).Value = usuarioStr.Length > 50 ? usuarioStr.Substring(0, 50) : usuarioStr;
                cmd.Parameters.Add("pMaquinaCrea", OracleDbType.Varchar2, 100).Value = maquinaStr.Length > 100 ? maquinaStr.Substring(0, 100) : maquinaStr;

                await cmd.ExecuteNonQueryAsync(ct);

                result.Id = idNew;
                result.Success = true;
                result.Message = "Línea de mando creada exitosamente.";

                await transaction.CommitAsync(ct);
                _logger.LogInformation("Línea de mando creada exitosamente con ID: {Id}", result.Id);
                try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria.ToString(), Modulo = "LINEA DE MANDO", Accion = "CREAR", Registro = $"Línea ID {idNew}: {request.Nombre} {request.Apellidos} - {request.Cargo}", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error Oracle: {ex.Message}";
                _logger.LogError(ex, "Error Oracle al crear línea de mando - Code: {Code}, Message: {Message}", ex.Number, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error general al crear línea de mando: {Message}", ex.Message);
            }

            return result;
        }
        
        public async Task<DtoLineaMandoResult> UpdateAsync(long id, DtoLineaMandoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            _logger.LogInformation("UpdateAsync llamado - ID: {Id}, FotoBase64 length: {Length}", id, request.FotoBase64?.Length ?? 0);

            var result = new DtoLineaMandoResult();

            // Buscar duplicados activos (Mismo Peso) pero excluyendo el que estamos editando
            var all = await GetAllAsync(ct);
            var duplicated = all.FirstOrDefault(x => 
                x.IdLineaMando != id &&
                !string.IsNullOrEmpty(x.Peso) &&
                !string.IsNullOrEmpty(request.Peso) &&
                request.Peso.Trim().StartsWith(x.Peso.Trim(), StringComparison.OrdinalIgnoreCase) 
                && x.Vigente == 1);

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                if (duplicated != null && !string.IsNullOrEmpty(request.Peso))
                {
                    _logger.LogInformation("Desactivando registro anterior ID {Id} para el peso {Peso}", duplicated.IdLineaMando, request.Peso);
                    await using var cmdDesactivar = conn.CreateCommand();
                    cmdDesactivar.Transaction = (OracleTransaction)transaction;
                    cmdDesactivar.CommandText = @"
                        UPDATE ctr_linea_mando
                           SET vigente = 0,
                               usuario_mod = :pUsuarioMod,
                               fecha_mod = SYSDATE,
                               maquina_mod = :pMaquinaMod
                         WHERE id_linea_mando = :pIdLineaMando";
                    
                    var uStr = usuarioAuditoria.ToString();
                    var mStr = maquinaAuditoria ?? "N/A";
                    cmdDesactivar.Parameters.Add("pUsuarioMod", OracleDbType.Varchar2, 50).Value = uStr.Length > 50 ? uStr.Substring(0, 50) : uStr;
                    cmdDesactivar.Parameters.Add("pMaquinaMod", OracleDbType.Varchar2, 100).Value = mStr.Length > 100 ? mStr.Substring(0, 100) : mStr;
                    cmdDesactivar.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = duplicated.IdLineaMando;
                    
                    await cmdDesactivar.ExecuteNonQueryAsync(ct);
                }

                await using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.CommandText = @"
                    UPDATE ctr_linea_mando
                       SET identificacion = :pIdentificacion,
                           nombre         = :pNombre,
                           apellidos      = :pApellidos,
                           grado          = :pGrado,
                           cargo          = :pCargo,
                           orden          = :pOrden,
                           peso           = :pPeso,
                           unidad         = :pUnidad,
                           foto_base64    = :pFotoBase64,
                           usuario_mod    = :pUsuarioMod,
                           fecha_mod      = SYSDATE,
                           maquina_mod    = :pMaquinaMod
                     WHERE id_linea_mando = :pIdLineaMando";

                cmd.Parameters.Add("pIdentificacion", OracleDbType.Varchar2, 20).Value = (request.Identificacion ?? string.Empty).Length > 20 ? (request.Identificacion ?? string.Empty).Substring(0, 20) : (request.Identificacion ?? string.Empty);
                cmd.Parameters.Add("pNombre", OracleDbType.Varchar2, 100).Value = (request.Nombre ?? string.Empty).Length > 100 ? (request.Nombre ?? string.Empty).Substring(0, 100) : (request.Nombre ?? string.Empty);
                cmd.Parameters.Add("pApellidos", OracleDbType.Varchar2, 100).Value = (request.Apellidos ?? string.Empty).Length > 100 ? (request.Apellidos ?? string.Empty).Substring(0, 100) : (request.Apellidos ?? string.Empty);
                cmd.Parameters.Add("pGrado", OracleDbType.Varchar2, 50).Value = (request.Grado ?? string.Empty).Length > 50 ? (request.Grado ?? string.Empty).Substring(0, 50) : (request.Grado ?? string.Empty);
                cmd.Parameters.Add("pCargo", OracleDbType.Varchar2, 100).Value = (request.Cargo ?? string.Empty).Length > 100 ? (request.Cargo ?? string.Empty).Substring(0, 100) : (request.Cargo ?? string.Empty);
                cmd.Parameters.Add("pOrden", OracleDbType.Int32).Value = request.Orden;
                cmd.Parameters.Add("pPeso", OracleDbType.Varchar2, 10).Value = (request.Peso ?? string.Empty).Length > 10 ? (request.Peso ?? string.Empty).Substring(0, 10) : (request.Peso ?? string.Empty);
                cmd.Parameters.Add("pUnidad", OracleDbType.Varchar2, 200).Value = (request.Unidad ?? string.Empty).Length > 200 ? (request.Unidad ?? string.Empty).Substring(0, 200) : (request.Unidad ?? string.Empty);

                var fotoNormalizada = NormalizeBase64(request.FotoBase64);
                var clobParam = new OracleParameter("pFotoBase64", OracleDbType.Clob)
                {
                    Direction = ParameterDirection.Input,
                    Value = string.IsNullOrEmpty(fotoNormalizada) ? DBNull.Value : fotoNormalizada
                };
                cmd.Parameters.Add(clobParam);

                var usuarioStrU = usuarioAuditoria.ToString();
                var maquinaStrU = maquinaAuditoria ?? "N/A";
                cmd.Parameters.Add("pUsuarioMod", OracleDbType.Varchar2, 50).Value = usuarioStrU.Length > 50 ? usuarioStrU.Substring(0, 50) : usuarioStrU;
                cmd.Parameters.Add("pMaquinaMod", OracleDbType.Varchar2, 100).Value = maquinaStrU.Length > 100 ? maquinaStrU.Substring(0, 100) : maquinaStrU;
                cmd.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = id;

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                if (rows > 0)
                {
                    result.Success = true;
                    result.Message = "Línea de mando actualizada exitosamente.";
                    await transaction.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria.ToString(), Modulo = "LINEA DE MANDO", Accion = "ACTUALIZAR", Registro = $"Línea ID {id}: {request.Nombre} {request.Apellidos} - {request.Cargo}", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                }
                else
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro para actualizar.";
                    await transaction.RollbackAsync(ct);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error de base de datos: {ex.Message}";
                _logger.LogError(ex, "Oracle error - Code: {Code}", ex.Number);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error: {Message}", ex.Message);
            }

            _logger.LogInformation("UpdateAsync result - Success: {Success}, Message: {Message}", result.Success, result.Message);
            return result;
        }

        public async Task<DtoLineaMandoResult> DeleteAsync(long id, long usuarioAuditoria, string maquinaAuditoria, string? observaciones = null, CancellationToken ct = default)
        {
            var result = new DtoLineaMandoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.CommandText = "DELETE FROM ctr_linea_mando WHERE id_linea_mando = :pIdLineaMando";
                cmd.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = id;

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                if (rows > 0)
                {
                    result.Success = true;
                    result.Message = "Línea de mando eliminada correctamente.";
                    await transaction.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria.ToString(), Modulo = "LINEA DE MANDO", Accion = "ELIMINAR", Registro = $"Línea ID {id} eliminada", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL", Observaciones = observaciones }, ct); } catch { /* no bloquear */ }
                }
                else
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro para eliminar.";
                    await transaction.RollbackAsync(ct);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error de base de datos: {ex.Message}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
            }

            return result;
        }

        public async Task<DtoLineaMandoResult> SetVigenteAsync(long id, int vigente, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoLineaMandoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    UPDATE ctr_linea_mando
                       SET vigente = :pVigente,
                           usuario_mod = :pUsuarioMod,
                           fecha_mod = SYSDATE,
                           maquina_mod = :pMaquinaMod
                     WHERE id_linea_mando = :pIdLineaMando";

                cmd.Parameters.Add("pVigente", OracleDbType.Int32).Value = vigente;
                
                var usuarioStrV = usuarioAuditoria.ToString();
                var maquinaStrV = maquinaAuditoria ?? "N/A";
                cmd.Parameters.Add("pUsuarioMod", OracleDbType.Varchar2, 50).Value = usuarioStrV.Length > 50 ? usuarioStrV.Substring(0, 50) : usuarioStrV;
                cmd.Parameters.Add("pMaquinaMod", OracleDbType.Varchar2, 100).Value = maquinaStrV.Length > 100 ? maquinaStrV.Substring(0, 100) : maquinaStrV;
                cmd.Parameters.Add("pIdLineaMando", OracleDbType.Int64).Value = id;

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                if (rows > 0)
                {
                    result.Success = true;
                    result.Message = "Estado actualizado correctamente.";
                    await transaction.CommitAsync(ct);
                    try { await _auditoriaRepo.RegistrarAuditoriaAsync(new DtoAuditoriaRegistrarRequest { Usuario = usuarioAuditoria.ToString(), Modulo = "LINEA DE MANDO", Accion = vigente == 1 ? "ACTIVAR" : "DESACTIVAR", Registro = $"Línea ID {id}: vigente={vigente}", Resultado = "EXITOSO", Maquina = maquinaAuditoria ?? "LOCAL" }, ct); } catch { /* no bloquear */ }
                }
                else
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro para actualizar.";
                    await transaction.RollbackAsync(ct);
                }
            }
            catch (OracleException ex) when (ex.Number == 1) // Unique constraint violation (ORA-00001)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning("Restricción única violada al desactivar. Borrando inactivo previo...");
                
                try
                {
                    var current = await GetByIdAsync(id, ct);
                    if (current != null)
                    {
                        var all = await GetAllAsync(ct);
                        var previousInactive = all.FirstOrDefault(x => x.Identificacion == current.Identificacion && x.Vigente == 0 && x.IdLineaMando != id);
                        
                        if (previousInactive != null)
                        {
                            await DeleteAsync(previousInactive.IdLineaMando, usuarioAuditoria, maquinaAuditoria, null, ct);
                            return await SetVigenteAsync(id, vigente, usuarioAuditoria, maquinaAuditoria, ct);
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Error en fallback de desactivación");
                }
                
                result.Success = false;
                result.Message = $"Error al desactivar: El funcionario ya tiene un registro inactivo previo y no se pudo limpiar. ({ex.Message})";
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error de base de datos: {ex.Message}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
            }

            return result;
        }

        private static DtoLineaMando MapLineaMandoGeneric(System.Data.Common.DbDataReader reader)
        {
            return new DtoLineaMando
            {
                IdLineaMando = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                Identificacion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Nombre = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Apellidos = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Grado = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Cargo = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Unidad = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                FotoBase64 = reader.IsDBNull(7) ? null : reader.GetString(7),
                Orden = reader.IsDBNull(8) ? 1 : Convert.ToInt32(reader.GetValue(8)),
                Vigente = reader.IsDBNull(9) ? 1 : Convert.ToInt32(reader.GetValue(9)),
                Peso = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty
            };
        }

        private static DtoLineaMando MapLineaMando(OracleDataReader reader)
        {
            return new DtoLineaMando
            {
                IdLineaMando = reader.GetInt64(reader.GetOrdinal("ID_LINEA_MANDO")),
                Identificacion = reader.IsDBNull(reader.GetOrdinal("IDENTIFICACION")) ? string.Empty : reader.GetString(reader.GetOrdinal("IDENTIFICACION")),
                Nombre = reader.IsDBNull(reader.GetOrdinal("NOMBRE")) ? string.Empty : reader.GetString(reader.GetOrdinal("NOMBRE")),
                Apellidos = reader.IsDBNull(reader.GetOrdinal("APELLIDOS")) ? string.Empty : reader.GetString(reader.GetOrdinal("APELLIDOS")),
                Grado = reader.IsDBNull(reader.GetOrdinal("GRADO")) ? string.Empty : reader.GetString(reader.GetOrdinal("GRADO")),
                Cargo = reader.IsDBNull(reader.GetOrdinal("CARGO")) ? string.Empty : reader.GetString(reader.GetOrdinal("CARGO")),
                Peso = reader.IsDBNull(reader.GetOrdinal("PESO")) ? string.Empty : reader.GetString(reader.GetOrdinal("PESO")),
                Unidad = reader.IsDBNull(reader.GetOrdinal("UNIDAD")) ? string.Empty : reader.GetString(reader.GetOrdinal("UNIDAD")),
                FotoBase64 = reader.IsDBNull(reader.GetOrdinal("FOTO_BASE64")) ? null : reader.GetString(reader.GetOrdinal("FOTO_BASE64")),
                Orden = reader.IsDBNull(reader.GetOrdinal("ORDEN")) ? 1 : reader.GetInt32(reader.GetOrdinal("ORDEN")),
                Vigente = reader.IsDBNull(reader.GetOrdinal("VIGENTE")) ? 1 : reader.GetInt32(reader.GetOrdinal("VIGENTE"))
            };
        }

        private static string? NormalizeBase64(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var value = raw.Trim();
            var commaIdx = value.IndexOf(',');
            if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIdx > -1)
            {
                value = value[(commaIdx + 1)..];
            }

            return value;
        }
    }
}
