using Comun.Dtos.PersonajeMes;
using Datos.Entities;
using Datos.Interfaz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Datos.Gestion
{
    public class DbPersonajeMesRepository : IDbPersonajeMesRepository
    {
        private readonly OfticDbContext _context;
        private readonly ILogger<DbPersonajeMesRepository> _logger;

        public DbPersonajeMesRepository(OfticDbContext context, ILogger<DbPersonajeMesRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DtoPersonajeMes>> GetAllAsync(CancellationToken ct)
        {
            return await GetAllAsync(null, ct);
        }

        public async Task<List<DtoPersonajeMes>> GetAllAsync(int? idPersonajeGrupo, CancellationToken ct)
        {
            try
            {
                // Obtener el año y mes del último registro vigente across both tables
                var latest = await _context.PersonajesMes.AsNoTracking()
                    .Where(p => p.Vigente == 1)
                    .Select(p => new { Anio = p.Anio ?? 0, Mes = p.Mes ?? 0 })
                    .Union(
                        _context.PersonajesGrupo.AsNoTracking()
                            .Where(g => g.Vigente == 1)
                            .Select(g => new { Anio = g.Anio ?? 0, Mes = g.Mes ?? 0 })
                    )
                    .OrderByDescending(x => x.Anio)
                    .ThenByDescending(x => x.Mes)
                    .FirstOrDefaultAsync(ct);

                if (latest == null)
                {
                    return new List<DtoPersonajeMes>();
                }

                var query = _context.PersonajesMes.AsNoTracking()
                    .Where(p => p.Vigente == 1 && p.Anio == latest.Anio && p.Mes == latest.Mes);

                if (idPersonajeGrupo.HasValue)
                {
                    query = query.Where(p => p.IdPersonajeGrupo == idPersonajeGrupo.Value);
                }

                var list = await query.OrderByDescending(p => p.IdPersonajeMes).ToListAsync(ct);

                return list.Select(p => new DtoPersonajeMes
                {
                    IdPersonajeMes = p.IdPersonajeMes,
                    Identificacion = p.Identificacion ?? "",
                    Nombres = p.Nombres ?? "",
                    Apellidos = p.Apellidos ?? "",
                    Grado = p.Grado ?? "",
                    Cargo = p.Cargo ?? "",
                    Unidad = p.Unidad ?? "",
                    FotoModificada = p.FotoModificada,
                    IdCategoria = p.IdCategoria ?? 0,
                    NumeroActa = p.NumeroActa ?? "",
                    Vigente = p.Vigente,
                    Mes = p.Mes ?? 1,
                    Anio = p.Anio ?? DateTime.Now.Year,
                    IdPersonajeGrupo = p.IdPersonajeGrupo ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar personajes del mes.");
                return new List<DtoPersonajeMes>();
            }
        }

        public async Task<DtoPersonajeMesResult> CreateAsync(DtoPersonajeMesRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();
            try
            {
                // Validar duplicado por identificación en registros vigentes
                var duplicado = await _context.PersonajesMes.CountAsync(p => p.Identificacion == request.Identificacion && p.Vigente == 1, ct) > 0;
                if (duplicado)
                {
                    result.Id = 0;
                    result.Success = false;
                    result.Message = "Ya existe un registro activo con esta identificación";
                    return result;
                }

                var entity = new PersonajeMesEntity
                {
                    Identificacion = request.Identificacion,
                    Nombres = request.Nombres,
                    Apellidos = request.Apellidos,
                    Grado = request.Grado,
                    Cargo = request.Cargo,
                    Unidad = request.Unidad,
                    IdCategoria = request.IdCategoria,
                    NumeroActa = request.NumeroActa,
                    Vigente = 1,
                    FotoModificada = request.FotoModificada,
                    Mes = request.Mes,
                    Anio = request.Anio,
                    IdPersonajeGrupo = request.IdPersonajeGrupo > 0 ? request.IdPersonajeGrupo : null,
                    UsuarioCrea = usuarioAuditoria.ToString(),
                    FechaCrea = DateTime.Now,
                    MaquinaCrea = maquinaAuditoria
                };

                _context.PersonajesMes.Add(entity);
                await _context.SaveChangesAsync(ct);

                result.Id = entity.IdPersonajeMes;
                result.Success = true;
                result.Message = "Registro creado exitosamente";
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "";
                _logger.LogError(ex, "Error al crear personaje del mes: {Message} | Inner: {Inner}", ex.Message, innerMsg);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error al crear: {ex.Message}" + (string.IsNullOrEmpty(innerMsg) ? "" : $" | Detalle: {innerMsg}");
            }
            return result;
        }

        public async Task<DtoPersonajeMesResult> UpdateAsync(long id, DtoPersonajeMesRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();
            try
            {
                var entity = await _context.PersonajesMes.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro";
                    return result;
                }

                entity.Identificacion = request.Identificacion;
                entity.Nombres = request.Nombres;
                entity.Apellidos = request.Apellidos;
                entity.Grado = request.Grado;
                entity.Cargo = request.Cargo;
                entity.Unidad = request.Unidad;
                entity.IdCategoria = request.IdCategoria;
                entity.NumeroActa = request.NumeroActa;
                entity.Mes = request.Mes;
                entity.Anio = request.Anio;
                entity.IdPersonajeGrupo = request.IdPersonajeGrupo > 0 ? request.IdPersonajeGrupo : null;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                // Solo actualizar la foto si se envía un valor
                if (request.FotoModificada != null)
                {
                    entity.FotoModificada = request.FotoModificada;
                }

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar personaje del mes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al actualizar: {ex.Message}";
            }
            return result;
        }

        public async Task<DtoPersonajeMesResult> DeleteAsync(long id, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();
            try
            {
                var entity = await _context.PersonajesMes.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro";
                    return result;
                }

                entity.Vigente = 0;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro eliminado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar personaje del mes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al eliminar: {ex.Message}";
            }
            return result;
        }

        public async Task<DtoPersonajeMesResult> SetVigenteAsync(long id, int vigente, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeMesResult();
            try
            {
                var entity = await _context.PersonajesMes.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = $"No se encontró el registro con ID: {id}";
                    return result;
                }

                entity.Vigente = vigente;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro actualizado correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar vigencia del personaje del mes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al actualizar: {ex.Message}";
            }
            return result;
        }

        public async Task<List<DtoPersonajeGrupo>> GetAllAsyncGrupo(CancellationToken ct)
        {
            try
            {
                // Obtener el año y mes del último registro vigente across both tables
                var latest = await _context.PersonajesMes.AsNoTracking()
                    .Where(p => p.Vigente == 1)
                    .Select(p => new { Anio = p.Anio ?? 0, Mes = p.Mes ?? 0 })
                    .Union(
                        _context.PersonajesGrupo.AsNoTracking()
                            .Where(g => g.Vigente == 1)
                            .Select(g => new { Anio = g.Anio ?? 0, Mes = g.Mes ?? 0 })
                    )
                    .OrderByDescending(x => x.Anio)
                    .ThenByDescending(x => x.Mes)
                    .FirstOrDefaultAsync(ct);

                if (latest == null)
                {
                    return new List<DtoPersonajeGrupo>();
                }

                var list = await _context.PersonajesGrupo.AsNoTracking()
                    .Where(g => g.Vigente == 1 && g.Anio == latest.Anio && g.Mes == latest.Mes)
                    .OrderByDescending(g => g.IdPersonajeGrupo)
                    .ToListAsync(ct);

                return list.Select(g => new DtoPersonajeGrupo
                {
                    IdPersonajeGrupo = g.IdPersonajeGrupo,
                    NombreGrupo = g.NombreGrupo ?? "",
                    FotoGrupo = g.FotoGrupo,
                    NumeroActa = g.NumeroActa ?? "",
                    Mes = g.Mes ?? 1,
                    Anio = g.Anio ?? DateTime.Now.Year,
                    Vigente = g.Vigente
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar grupos de personajes.");
                return new List<DtoPersonajeGrupo>();
            }
        }

        public async Task<DtoPersonajeGrupoResult> CreateAsyncGrupo(DtoPersonajeGrupoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();
            try
            {
                var entity = new PersonajeGrupoEntity
                {
                    NombreGrupo = request.NombreGrupo,
                    FotoGrupo = request.FotoGrupo,
                    NumeroActa = request.NumeroActa,
                    Mes = request.Mes,
                    Anio = request.Anio,
                    Vigente = 1,
                    UsuarioCrea = usuarioAuditoria.ToString(),
                    FechaCrea = DateTime.Now,
                    MaquinaCrea = maquinaAuditoria
                };

                _context.PersonajesGrupo.Add(entity);
                await _context.SaveChangesAsync(ct);

                result.Id = entity.IdPersonajeGrupo;
                result.Success = true;
                result.Message = "Registro creado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear grupo de personajes: {Message}", ex.Message);
                result.Id = 0;
                result.Success = false;
                result.Message = $"Error al crear: {ex.Message}";
            }
            return result;
        }

        public async Task<DtoPersonajeGrupoResult> UpdateAsyncGrupo(long id, DtoPersonajeGrupoRequest request, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();
            try
            {
                var entity = await _context.PersonajesGrupo.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro";
                    return result;
                }

                entity.NombreGrupo = request.NombreGrupo;
                entity.NumeroActa = request.NumeroActa;
                entity.Mes = request.Mes;
                entity.Anio = request.Anio;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                // Solo actualizar la foto si se envía un valor
                if (request.FotoGrupo != null)
                {
                    entity.FotoGrupo = request.FotoGrupo;
                }

                // ACTUALIZAR PERSONAJES DEL MES RELACIONADOS
                var relacionados = await _context.PersonajesMes
                    .Where(p => p.IdPersonajeGrupo == id)
                    .ToListAsync(ct);

                foreach (var p in relacionados)
                {
                    p.Mes = request.Mes;
                    p.Anio = request.Anio;
                    p.UsuarioMod = usuarioAuditoria.ToString();
                    p.FechaMod = DateTime.Now;
                    p.MaquinaMod = maquinaAuditoria;
                }

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar grupo de personajes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al actualizar: {ex.Message}";
            }
            return result;
        }

        public async Task<DtoPersonajeGrupoResult> DeleteAsyncGrupo(long id, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();
            try
            {
                var entity = await _context.PersonajesGrupo.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = "No se encontró el registro";
                    return result;
                }

                entity.Vigente = 0;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro eliminado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar grupo de personajes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al eliminar: {ex.Message}";
            }
            return result;
        }

        public async Task<DtoPersonajeGrupoResult> SetVigenteAsyncGrupo(long id, int vigente, long usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var result = new DtoPersonajeGrupoResult();
            try
            {
                var entity = await _context.PersonajesGrupo.FindAsync(new object[] { id }, ct);
                if (entity == null)
                {
                    result.Success = false;
                    result.Message = $"No se encontró el registro con ID: {id}";
                    return result;
                }

                entity.Vigente = vigente;
                entity.UsuarioMod = usuarioAuditoria.ToString();
                entity.FechaMod = DateTime.Now;
                entity.MaquinaMod = maquinaAuditoria;

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Message = "Registro actualizado correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar vigencia del grupo de personajes: {Message}", ex.Message);
                result.Success = false;
                result.Message = $"Error al actualizar: {ex.Message}";
            }
            return result;
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

            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var request in items)
                {
                    // Validar duplicado por identificación en registros vigentes
                    var duplicado = await _context.PersonajesMes.CountAsync(p => p.Identificacion == request.Identificacion && p.Vigente == 1, ct) > 0;
                    if (duplicado)
                    {
                        continue;
                    }

                    var entity = new PersonajeMesEntity
                    {
                        Identificacion = request.Identificacion,
                        Nombres = request.Nombres,
                        Apellidos = request.Apellidos,
                        Grado = request.Grado,
                        Cargo = request.Cargo,
                        Unidad = request.Unidad,
                        IdCategoria = request.IdCategoria,
                        NumeroActa = request.NumeroActa,
                        Vigente = 1,
                        FotoModificada = request.FotoModificada,
                        Mes = request.Mes,
                        Anio = request.Anio,
                        IdPersonajeGrupo = request.IdPersonajeGrupo > 0 ? request.IdPersonajeGrupo : null,
                        UsuarioCrea = usuarioAuditoria.ToString(),
                        FechaCrea = DateTime.Now,
                        MaquinaCrea = maquinaAuditoria
                    };

                    _context.PersonajesMes.Add(entity);
                    await _context.SaveChangesAsync(ct);
                    result.TotalExitosos++;
                }

                await transaction.CommitAsync(ct);
                result.Success = true;
                result.Message = $"Se procesaron {result.TotalExitosos} registros correctamente.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error en bulk insert de PersonajeMes");
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
            }

            return result;
        }
    }
}
