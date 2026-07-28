using Comun.Dtos.PersonajeMes;
using Comun.Extensions;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datos.Gestion
{
    public class DbPersonajeMesRepository : IDbPersonajeMesRepository
    {
        private readonly string _cs;
        private readonly ILogger<DbPersonajeMesRepository> _logger;

        private readonly IConfiguration _configuration;

        public DbPersonajeMesRepository(IConfiguration configuration, ILogger<DbPersonajeMesRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<DtoPersonajeMes>> GetAllAsync(CancellationToken ct)
        {
            return await GetAllAsync(null, ct);
        }

        // Nueva sobrecarga: permite filtrar por idPersonajeGrupo
        public async Task<List<DtoPersonajeMes>> GetAllAsync(int? idPersonajeGrupo, CancellationToken ct)
        {
            var result = new List<DtoPersonajeMes>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_personaje_mes, identificacion, nombres, apellidos, grado, cargo, unidad, foto_modificada, id_categoria, numero_acta, vigente, mes, anio, id_personaje_grupo
                FROM ctr_personaje_mes
                WHERE vigente = 1
                ORDER BY id_personaje_mes DESC";

            if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var conn = new MySqlConnector.MySqlConnection(_cs);
                    await conn.OpenAsync(ct);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = idPersonajeGrupo.HasValue ? selectSql.Replace("WHERE vigente = 1", $"WHERE vigente = 1 AND id_personaje_grupo = {idPersonajeGrupo.Value}") : selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapPersonajeMesGeneric(reader));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar personajes del mes en MySQL. Se retorna lista vacía.");
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
                    cmd.CommandText = idPersonajeGrupo.HasValue ? selectSql.Replace("WHERE vigente = 1", $"WHERE vigente = 1 AND id_personaje_grupo = {idPersonajeGrupo.Value}") : selectSql;
                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        result.Add(MapPersonajeMesGeneric(reader));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar personajes del mes en PostgreSQL. Se retorna lista vacía.");
                }
                return result;
            }

            await using var oracleConn = new OracleConnection(_cs);
            await oracleConn.OpenAsync(ct);

            try
            {
                await using var cmd = oracleConn.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_GET_ALL";
                cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                await using var readerSp = await cmd.ExecuteReaderAsync(ct);
                while (await readerSp.ReadAsync(ct))
                    result.Add(MapPersonajeMesGeneric(readerSp));
                return result;
            }
            catch
            {
                result.Clear();
                await using var cmdQuery = oracleConn.CreateCommand();
                cmdQuery.CommandText = selectSql;
                await using var readerQuery = await cmdQuery.ExecuteReaderAsync(ct);
                while (await readerQuery.ReadAsync(ct))
                    result.Add(MapPersonajeMesGeneric(readerQuery));
                return result;
            }
        }
        public async Task<DtoPersonajeMesResult> CreateAsync(DtoPersonajeMesRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                _logger.LogInformation("Ejecutando PKG_PERSONAJE_MES.P_CREATE con datos: Identificacion={Ident}, Nombre={Nombre}, Cargo={Cargo}",
                    request.Identificacion, request.Nombres, request.Cargo);

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_CREATE";

                // ORDEN CORRECTO según la firma del procedimiento Oracle
                cmd.Parameters.Add("p_identificacion", OracleDbType.Varchar2, 20).Value = (request.Identificacion ?? string.Empty).Length > 20 ? (request.Identificacion ?? string.Empty).Substring(0, 20) : (request.Identificacion ?? string.Empty);
                cmd.Parameters.Add("p_nombres", OracleDbType.Varchar2, 100).Value = (request.Nombres ?? string.Empty).Length > 100 ? (request.Nombres ?? string.Empty).Substring(0, 100) : (request.Nombres ?? string.Empty);
                cmd.Parameters.Add("p_apellidos", OracleDbType.Varchar2, 100).Value = (request.Apellidos ?? string.Empty).Length > 100 ? (request.Apellidos ?? string.Empty).Substring(0, 100) : (request.Apellidos ?? string.Empty);
                cmd.Parameters.Add("p_grado", OracleDbType.Varchar2, 50).Value = (request.Grado ?? string.Empty).Length > 50 ? (request.Grado ?? string.Empty).Substring(0, 50) : (request.Grado ?? string.Empty);
                cmd.Parameters.Add("p_cargo", OracleDbType.Varchar2, 100).Value = (request.Cargo ?? string.Empty).Length > 100 ? (request.Cargo ?? string.Empty).Substring(0, 100) : (request.Cargo ?? string.Empty);
                cmd.Parameters.Add("p_unidad", OracleDbType.Varchar2, 200).Value = (request.Unidad ?? string.Empty).Length > 200 ? (request.Unidad ?? string.Empty).Substring(0, 200) : (request.Unidad ?? string.Empty);
                
                // Nota: p_idcategoria es NUMBER en Oracle (Int64)
                cmd.Parameters.Add("p_idcategoria", OracleDbType.Int64).Value = (long)request.IdCategoria;
                cmd.Parameters.Add("p_numero_acta", OracleDbType.Varchar2, 50).Value = (request.NumeroActa ?? string.Empty).Length > 50 ? (request.NumeroActa ?? string.Empty).Substring(0, 50) : (request.NumeroActa ?? string.Empty);

                var usuarioStr = usuarioAuditoria.ToString();
                var maquinaStr = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStr.Length > 50 ? usuarioStr.Substring(0, 50) : usuarioStr;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStr.Length > 100 ? maquinaStr.Substring(0, 100) : maquinaStr;
                
                // p_foto_modificada va DESPUÉS de los parámetros de auditoría
                cmd.Parameters.Add("p_foto_modificada", OracleDbType.Varchar2, 200).Value = (request.FotoModificada ?? string.Empty).Length > 200 ? (request.FotoModificada ?? string.Empty).Substring(0, 200) : (request.FotoModificada ?? string.Empty);

                cmd.Parameters.Add("p_mes", OracleDbType.Int32).Value = request.Mes;
                cmd.Parameters.Add("p_anio", OracleDbType.Int32).Value = request.Anio;
                // enviar DBNull si no hay grupo; usar Int64 para FK
                var idGrupoVal = request.IdPersonajeGrupo.HasValue && request.IdPersonajeGrupo.Value > 0
                    ? (object)request.IdPersonajeGrupo.Value
                    : DBNull.Value;
                cmd.Parameters.Add("p_id_personaje_grupo", OracleDbType.Int64).Value = idGrupoVal;

                OracleParameter pId = new OracleParameter("p_id", OracleDbType.Int64);
                pId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pId);

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pIdVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_id"].Value;
                result.Id = pIdVal.IsNull ? 0 : (long)pIdVal;

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Personje del mes creado exitosamente con ID: {Id}", result.Id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al crear personaje del mes: {Message}", result.Message);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error Oracle: {ex.Message}";
                _logger.LogError(ex, "Error Oracle al crear personaje del mes - Code: {Code}, Message: {Message}", ex.Number, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error general al crear personaje del mes: {Message}", ex.Message);
            }

            return result;
        }
        public async Task<DtoPersonajeMesResult> UpdateAsync(long id, DtoPersonajeMesRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            _logger.LogInformation("UpdateAsync llamado - ID: {Id}, FotoModificada length: {Length}", id, request.FotoModificada?.Length ?? 0);

            var result = new DtoPersonajeMesResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_UPDATE";

                cmd.Parameters.Add("p_id_personaje_mes", OracleDbType.Int64).Value = id;
                cmd.Parameters.Add("p_identificacion", OracleDbType.Varchar2, 20).Value = (request.Identificacion ?? string.Empty).Length > 20 ? (request.Identificacion ?? string.Empty).Substring(0, 20) : (request.Identificacion ?? string.Empty);
                cmd.Parameters.Add("p_nombres", OracleDbType.Varchar2, 100).Value = (request.Nombres ?? string.Empty).Length > 100 ? (request.Nombres ?? string.Empty).Substring(0, 100) : (request.Nombres ?? string.Empty);
                cmd.Parameters.Add("p_apellidos", OracleDbType.Varchar2, 100).Value = (request.Apellidos ?? string.Empty).Length > 100 ? (request.Apellidos ?? string.Empty).Substring(0, 100) : (request.Apellidos ?? string.Empty);
                cmd.Parameters.Add("p_grado", OracleDbType.Varchar2, 50).Value = (request.Grado ?? string.Empty).Length > 50 ? (request.Grado ?? string.Empty).Substring(0, 50) : (request.Grado ?? string.Empty);
                cmd.Parameters.Add("p_cargo", OracleDbType.Varchar2, 100).Value = (request.Cargo ?? string.Empty).Length > 100 ? (request.Cargo ?? string.Empty).Substring(0, 100) : (request.Cargo ?? string.Empty);
                cmd.Parameters.Add("p_unidad", OracleDbType.Varchar2, 200).Value = (request.Unidad ?? string.Empty).Length > 200 ? (request.Unidad ?? string.Empty).Substring(0, 200) : (request.Unidad ?? string.Empty);

                // IdCategoria como Int64 (NUMBER en Oracle)
                cmd.Parameters.Add("p_idcategoria", OracleDbType.Int64).Value = (long)request.IdCategoria;
                cmd.Parameters.Add("p_numero_acta", OracleDbType.Varchar2, 50).Value = (request.NumeroActa ?? string.Empty).Length > 50 ? (request.NumeroActa ?? string.Empty).Substring(0, 50) : (request.NumeroActa ?? string.Empty);


                var usuarioStrU = usuarioAuditoria.ToString();
                var maquinaStrU = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrU.Length > 50 ? usuarioStrU.Substring(0, 50) : usuarioStrU;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrU.Length > 100 ? maquinaStrU.Substring(0, 100) : maquinaStrU;
                cmd.Parameters.Add("p_foto_modificada", OracleDbType.Varchar2, 200).Value = (request.FotoModificada ?? string.Empty).Length > 200 ? (request.FotoModificada ?? string.Empty).Substring(0, 200) : (request.FotoModificada ?? string.Empty);

                // Mes y Anio como Int32
                cmd.Parameters.Add("p_mes", OracleDbType.Int32).Value = request.Mes;
                cmd.Parameters.Add("p_anio", OracleDbType.Int32).Value = request.Anio;

                // IdPersonajeGrupo como Int64 y enviar DBNull si no aplica
                var idGrupoVal = request.IdPersonajeGrupo.HasValue && request.IdPersonajeGrupo.Value > 0
                    ? (object)request.IdPersonajeGrupo.Value
                    : DBNull.Value;
                cmd.Parameters.Add("p_idpersonajegrupo ", OracleDbType.Int64).Value = idGrupoVal;

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Personaje del mes actualizada exitosamente - ID: {Id}", id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al actualizar personaje del mes: {Message}", result.Message);
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
        public async Task<DtoPersonajeMesResult> DeleteAsync(long id, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_DELETE";

                cmd.Parameters.Add("p_id_personaje_mes", OracleDbType.Int64).Value = id;

                var usuarioStrD = usuarioAuditoria.ToString();
                var maquinaStrD = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrD.Length > 50 ? usuarioStrD.Substring(0, 50) : usuarioStrD;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrD.Length > 100 ? maquinaStrD.Substring(0, 100) : maquinaStrD;

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                // Reemplaza la lectura:
                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                }
                else
                {
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
        public async Task<DtoPersonajeMesResult> SetVigenteAsync(long id, int vigente, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_SET_VIGENTE";

                cmd.Parameters.Add("p_id_personaje_mes", OracleDbType.Int64).Value = id;
                cmd.Parameters.Add("p_vigente", OracleDbType.Int32).Value = vigente;

                var usuarioStrV = usuarioAuditoria.ToString();
                var maquinaStrV = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrV.Length > 50 ? usuarioStrV.Substring(0, 50) : usuarioStrV;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrV.Length > 100 ? maquinaStrV.Substring(0, 100) : maquinaStrV;

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)pSuccess.Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = pMessage.Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Vigente actualizado para personaje del mes {Id}", id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al actualizar vigente: {Message}", result.Message);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error Oracle: {ex.Message}";
                _logger.LogError(ex, "Error Oracle al actualizar vigente - Code: {Code}, Message: {Message}", ex.Number, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error general al actualizar vigente: {Message}", ex.Message);
            }

            return result;
        }


        public async Task<List<DtoPersonajeGrupo>> GetAllAsyncGrupo(CancellationToken ct)
        {
            var result = new List<DtoPersonajeGrupo>();
            var provider = (_configuration["DatabaseProvider"] ?? "Oracle").Trim();

            const string selectSql = @"
                SELECT id_personaje_grupo, nombre_grupo, foto_grupo, numero_acta, mes, anio, vigente
                FROM ctr_personaje_grupo
                WHERE vigente = 1
                ORDER BY id_personaje_grupo DESC";

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
                        result.Add(new DtoPersonajeGrupo
                        {
                            IdPersonajeGrupo = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            NombreGrupo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            FotoGrupo = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NumeroActa = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Mes = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                            Anio = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                            Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6))
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
                        result.Add(new DtoPersonajeGrupo
                        {
                            IdPersonajeGrupo = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            NombreGrupo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            FotoGrupo = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NumeroActa = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Mes = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                            Anio = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                            Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6))
                        });
                    }
                    return result;
                }

                await using var oracleConn = new OracleConnection(_cs);
                await oracleConn.OpenAsync(ct);

                try
                {
                    await using var cmd = oracleConn.CreateCommand();
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PKG_PERSONAJE_MES.P_GET_ALL_GRUPO";
                    cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        result.Add(MapPersonajeGrupo(reader));
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
                        result.Add(new DtoPersonajeGrupo
                        {
                            IdPersonajeGrupo = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                            NombreGrupo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            FotoGrupo = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NumeroActa = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Mes = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                            Anio = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                            Vigente = reader.IsDBNull(6) ? 1 : Convert.ToInt32(reader.GetValue(6))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar grupos de personajes del mes.");
            }

            return result;
        }
        public async Task<DtoPersonajeGrupoResult> CreateAsyncGrupo(DtoPersonajeGrupoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_CREATE_GRUPO";

                
                cmd.Parameters.Add("p_nombre_grupo", OracleDbType.Varchar2, 100).Value = (request.NombreGrupo ?? string.Empty).Length > 100 ? (request.NombreGrupo ?? string.Empty).Substring(0, 100) : (request.NombreGrupo ?? string.Empty);
                cmd.Parameters.Add("p_foto_grupo", OracleDbType.Varchar2, 200).Value = (request.FotoGrupo ?? string.Empty).Length > 200 ? (request.FotoGrupo ?? string.Empty).Substring(0, 200) : (request.FotoGrupo ?? string.Empty);
                cmd.Parameters.Add("p_numero_acta", OracleDbType.Varchar2, 50).Value = (request.NumeroActa ?? string.Empty).Length > 50 ? (request.NumeroActa ?? string.Empty).Substring(0, 50) : (request.NumeroActa ?? string.Empty);
                cmd.Parameters.Add("p_mes", OracleDbType.Int32).Value = (long)request.Mes;
                cmd.Parameters.Add("p_anio", OracleDbType.Int32).Value = (long)request.Anio;

                var usuarioStr = usuarioAuditoria.ToString();
                var maquinaStr = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStr.Length > 50 ? usuarioStr.Substring(0, 50) : usuarioStr;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStr.Length > 100 ? maquinaStr.Substring(0, 100) : maquinaStr;
                              
                
                OracleParameter pId = new OracleParameter("p_id", OracleDbType.Int64);
                pId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pId);

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pIdVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_id"].Value;
                result.Id = pIdVal.IsNull ? 0 : (long)pIdVal;

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Equipo o grupo de alto rendimiento creado exitosamente con ID: {Id}", result.Id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al crear Equipo o grupo de alto rendimiento: {Message}", result.Message);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error Oracle: {ex.Message}";
                _logger.LogError(ex, "Error Oracle al crear Equipo o grupo de alto rendimiento - Code: {Code}, Message: {Message}", ex.Number, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error general al crear Equipo o grupo de alto rendimiento: {Message}", ex.Message);
            }

            return result;
        }
        public async Task<DtoPersonajeGrupoResult> UpdateAsyncGrupo(long id, DtoPersonajeGrupoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            _logger.LogInformation("UpdateAsyncGrupo llamado - ID: {Id}, FotoGrupo length: {Length}", id, request.FotoGrupo?.Length ?? 0);

            var result = new DtoPersonajeGrupoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_UPDATE_GRUPO";

                cmd.Parameters.Add("p_id_personaje_grupo", OracleDbType.Int64).Value = id;
                cmd.Parameters.Add("p_nombre_grupo", OracleDbType.Varchar2, 100).Value = (request.NombreGrupo ?? string.Empty).Length > 100 ? (request.NombreGrupo ?? string.Empty).Substring(0, 100) : (request.NombreGrupo ?? string.Empty);
                cmd.Parameters.Add("p_foto_grupo", OracleDbType.Varchar2, 200).Value = (request.FotoGrupo ?? string.Empty).Length > 200 ? (request.FotoGrupo ?? string.Empty).Substring(0, 200) : (request.FotoGrupo ?? string.Empty);
                cmd.Parameters.Add("p_numero_acta", OracleDbType.Varchar2, 50).Value = (request.NumeroActa ?? string.Empty).Length > 50 ? (request.NumeroActa ?? string.Empty).Substring(0, 50) : (request.NumeroActa ?? string.Empty);
                cmd.Parameters.Add("p_mes", OracleDbType.Int32).Value = (long)request.Mes;
                cmd.Parameters.Add("p_anio", OracleDbType.Int32).Value = (long)request.Anio;

                var usuarioStrU = usuarioAuditoria.ToString();
                var maquinaStrU = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrU.Length > 50 ? usuarioStrU.Substring(0, 50) : usuarioStrU;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrU.Length > 100 ? maquinaStrU.Substring(0, 100) : maquinaStrU;
                
                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Equipo o grupo de alto rendimiento actualizada exitosamente - ID: {Id}", id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al actualizar Equipo o grupo de alto rendimiento: {Message}", result.Message);
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

            _logger.LogInformation("UpdateAsyncGrupo result - Success: {Success}, Message: {Message}", result.Success, result.Message);
            return result;
        }
        public async Task<DtoPersonajeGrupoResult> DeleteAsyncGrupo(long id, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_DELETE_GRUPO";

                cmd.Parameters.Add("p_id_personaje_grupo", OracleDbType.Int64).Value = id;

                var usuarioStrD = usuarioAuditoria.ToString();
                var maquinaStrD = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrD.Length > 50 ? usuarioStrD.Substring(0, 50) : usuarioStrD;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrD.Length > 100 ? maquinaStrD.Substring(0, 100) : maquinaStrD;

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                // Reemplaza la lectura:
                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = cmd.Parameters["p_message"].Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                }
                else
                {
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
        public async Task<DtoPersonajeGrupoResult> SetVigenteAsyncGrupo(long id, int vigente, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (OracleTransaction)transaction;
                cmd.BindByName = true;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PKG_PERSONAJE_MES.P_SET_VIGENTE_GRUPO";

                cmd.Parameters.Add("p_idpersonajegrupo ", OracleDbType.Int64).Value = id;
                cmd.Parameters.Add("p_vigente", OracleDbType.Int32).Value = vigente;

                var usuarioStrV = usuarioAuditoria.ToString();
                var maquinaStrV = maquinaAuditoria ?? "N/A";

                cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStrV.Length > 50 ? usuarioStrV.Substring(0, 50) : usuarioStrV;
                cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStrV.Length > 100 ? maquinaStrV.Substring(0, 100) : maquinaStrV;

                OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(pSuccess);

                OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                pMessage.Direction = ParameterDirection.Output;
                pMessage.Size = 4000;
                cmd.Parameters.Add(pMessage);

                await cmd.ExecuteNonQueryAsync(ct);

                var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)pSuccess.Value;
                result.Success = !pSuccessVal.IsNull && (int)pSuccessVal == 1;
                result.Message = pMessage.Value?.ToString() ?? string.Empty;

                if (result.Success)
                {
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Vigente actualizado para equipo o grupo de alto rendimiento {Id}", id);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogWarning("Error al actualizar vigente: {Message}", result.Message);
                }
            }
            catch (OracleException ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error Oracle: {ex.Message}";
                _logger.LogError(ex, "Error Oracle al actualizar vigente - Code: {Code}, Message: {Message}", ex.Number, ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error general al actualizar vigente: {Message}", ex.Message);
            }

            return result;
        }

        private static DtoPersonajeMes MapPersonajeMes(OracleDataReader reader)
        {
            return new DtoPersonajeMes
            {
                IdPersonajeMes = GetInt64OrDefault(reader, "ID_PERSONAJE_MES"),
                Identificacion = GetStringOrDefault(reader, "IDENTIFICACION"),
                Nombres = GetStringOrDefault(reader, "NOMBRES"),
                Apellidos = GetStringOrDefault(reader, "APELLIDOS"),
                Grado = GetStringOrDefault(reader, "GRADO"),
                Cargo = GetStringOrDefault(reader, "CARGO"),
                Unidad = GetStringOrDefault(reader, "UNIDAD"),
                FotoModificada = GetNullableStringOrDefault(reader, "FOTO_MODIFICADA"),
                IdCategoria = GetInt32OrDefault(reader, "ID_CATEGORIA"),
                NumeroActa = GetStringOrDefault(reader, "NUMERO_ACTA"),
                Vigente = GetInt32OrDefault(reader, "VIGENTE", 1),
                Mes = GetInt32OrDefault(reader, "MES"),
                Anio = GetInt32OrDefault(reader, "ANIO"),
                IdPersonajeGrupo = GetInt64OrDefault(reader, "ID_PERSONAJE_GRUPO")
            };
        }
        private static DtoPersonajeGrupo MapPersonajeGrupo(OracleDataReader reader)
        {
            return new DtoPersonajeGrupo
            {
                IdPersonajeGrupo = GetInt32OrDefault(reader, "ID_PERSONAJE_GRUPO"),
                NombreGrupo = GetStringOrDefault(reader, "NOMBRE_GRUPO"),
                FotoGrupo = GetNullableStringOrDefault(reader, "FOTO_GRUPO"),
                NumeroActa = GetStringOrDefault(reader, "NUMERO_ACTA"),
                Mes = GetInt32OrDefault(reader, "MES"),
                Anio = GetInt32OrDefault(reader, "ANIO"),
                Vigente = GetInt32OrDefault(reader, "VIGENTE", 1)
            };
        }

        private static bool HasColumn(OracleDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetStringOrDefault(OracleDataReader reader, string columnName, string defaultValue = "")
        {
            if (!HasColumn(reader, columnName))
            {
                return defaultValue;
            }

            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetValue(ordinal)?.ToString() ?? defaultValue;
        }

        private static string? GetNullableStringOrDefault(OracleDataReader reader, string columnName)
        {
            if (!HasColumn(reader, columnName))
            {
                return null;
            }

            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString();
        }

        private static int GetInt32OrDefault(OracleDataReader reader, string columnName, int defaultValue = 0)
        {
            if (!HasColumn(reader, columnName))
            {
                return defaultValue;
            }

            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static long GetInt64OrDefault(OracleDataReader reader, string columnName, long defaultValue = 0)
        {
            if (!HasColumn(reader, columnName))
            {
                return defaultValue;
            }

            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToInt64(reader.GetValue(ordinal));
        }

        public async Task<DtoPersonajeMesBulkResult> CreateBulkAsync(List<DtoPersonajeMesRequest> items, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesBulkResult();
            result.TotalProcesados = items?.Count ?? 0;
            result.TotalExitosos = 0;

            if (items == null || items.Count == 0)
            {
                result.Success = false;
                result.Message = "No hay elementos para procesar.";
                return result;
            }

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                _logger.LogInformation("Iniciando bulk insert de PersonajeMes - Total items: {Total}", items.Count);

                foreach (var request in items)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = (OracleTransaction)transaction;
                    cmd.BindByName = true;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PKG_PERSONAJE_MES.P_CREATE";

                    cmd.Parameters.Add("p_identificacion", OracleDbType.Varchar2, 20).Value = (request.Identificacion ?? string.Empty).Length > 20 ? request.Identificacion.Substring(0, 20) : request.Identificacion ?? string.Empty;
                    cmd.Parameters.Add("p_nombres", OracleDbType.Varchar2, 100).Value = (request.Nombres ?? string.Empty).Length > 100 ? request.Nombres.Substring(0, 100) : request.Nombres ?? string.Empty;
                    cmd.Parameters.Add("p_apellidos", OracleDbType.Varchar2, 100).Value = (request.Apellidos ?? string.Empty).Length > 100 ? request.Apellidos.Substring(0, 100) : request.Apellidos ?? string.Empty;
                    cmd.Parameters.Add("p_grado", OracleDbType.Varchar2, 50).Value = (request.Grado ?? string.Empty).Length > 50 ? request.Grado.Substring(0, 50) : request.Grado ?? string.Empty;
                    cmd.Parameters.Add("p_cargo", OracleDbType.Varchar2, 100).Value = (request.Cargo ?? string.Empty).Length > 100 ? request.Cargo.Substring(0, 100) : request.Cargo ?? string.Empty;
                    cmd.Parameters.Add("p_unidad", OracleDbType.Varchar2, 200).Value = (request.Unidad ?? string.Empty).Length > 200 ? request.Unidad.Substring(0, 200) : request.Unidad ?? string.Empty;
                    cmd.Parameters.Add("p_idcategoria", OracleDbType.Int64).Value = (long)request.IdCategoria;
                    cmd.Parameters.Add("p_numero_acta", OracleDbType.Varchar2, 50).Value = (request.NumeroActa ?? string.Empty).Length > 50 ? request.NumeroActa.Substring(0, 50) : request.NumeroActa ?? string.Empty;

                    var usuarioStr = usuarioAuditoria.ToString();
                    var maquinaStr = maquinaAuditoria ?? "N/A";
                    cmd.Parameters.Add("p_usuario_auditoria", OracleDbType.Varchar2, 50).Value = usuarioStr.Length > 50 ? usuarioStr.Substring(0, 50) : usuarioStr;
                    cmd.Parameters.Add("p_maquina_auditoria", OracleDbType.Varchar2, 100).Value = maquinaStr.Length > 100 ? maquinaStr.Substring(0, 100) : maquinaStr;
                    cmd.Parameters.Add("p_foto_modificada", OracleDbType.Varchar2, 200).Value = (request.FotoModificada ?? string.Empty).Length > 200 ? request.FotoModificada.Substring(0, 200) : request.FotoModificada ?? string.Empty;
                    cmd.Parameters.Add("p_mes", OracleDbType.Int32).Value = (long)request.Mes;
                    cmd.Parameters.Add("p_anio", OracleDbType.Int32).Value = (long)request.Anio;
                    var idGrupoValue = request.IdPersonajeGrupo > 0 ? (object)Convert.ToInt64(request.IdPersonajeGrupo) : DBNull.Value;
                    cmd.Parameters.Add("p_id_personaje_grupo", OracleDbType.Int64).Value = idGrupoValue;
                    OracleParameter pId = new OracleParameter("p_id", OracleDbType.Int64);
                    pId.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(pId);

                    OracleParameter pSuccess = new OracleParameter("p_success", OracleDbType.Int32);
                    pSuccess.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(pSuccess);

                    OracleParameter pMessage = new OracleParameter("p_message", OracleDbType.Varchar2);
                    pMessage.Direction = ParameterDirection.Output;
                    pMessage.Size = 4000;
                    cmd.Parameters.Add(pMessage);

                    await cmd.ExecuteNonQueryAsync(ct);

                    var pSuccessVal = (Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["p_success"].Value;
                    if (!pSuccessVal.IsNull && (int)pSuccessVal == 1)
                    {
                        result.TotalExitosos++;
                    }
                }

                if (result.TotalExitosos == items.Count)
                {
                    await transaction.CommitAsync(ct);
                    result.Success = true;
                    result.Message = $"Se procesaron {result.TotalExitosos} registros correctamente.";
                    _logger.LogInformation("Bulk insert completado exitosamente - Total: {Total}", result.TotalExitosos);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    result.Success = false;
                    result.Message = $"Se procesaron {result.TotalExitosos} de {items.Count} registros.";
                    _logger.LogWarning("Bulk insert parcial - Exitosos: {Exitosos}, Total: {Total}", result.TotalExitosos, items.Count);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error en bulk insert de PersonajeMes");
            }

            return result;
        }

        private static DtoPersonajeMes MapPersonajeMesGeneric(System.Data.Common.DbDataReader reader)
        {
            return new DtoPersonajeMes
            {
                IdPersonajeMes = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
                Identificacion = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Nombres = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Apellidos = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Grado = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Cargo = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Unidad = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                FotoModificada = reader.IsDBNull(7) ? null : reader.GetString(7),
                IdCategoria = reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader.GetValue(8)),
                NumeroActa = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Vigente = reader.IsDBNull(10) ? 1 : Convert.ToInt32(reader.GetValue(10)),
                Mes = reader.IsDBNull(11) ? 1 : Convert.ToInt32(reader.GetValue(11)),
                Anio = reader.IsDBNull(12) ? DateTime.Now.Year : Convert.ToInt32(reader.GetValue(12)),
                IdPersonajeGrupo = reader.FieldCount > 13 && !reader.IsDBNull(13) ? Convert.ToInt32(reader.GetValue(13)) : 0
            };
        }
    }
}
