using Comun.Dtos.Administracion;
using Datos.Entities;
using Datos.Interfaz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Datos.Gestion
{
    public class DbCuentaEmailRepository : IDbCuentaEmailRepository
    {
        private readonly OfticDbContext _context;
        private readonly ILogger<DbCuentaEmailRepository> _logger;

        public DbCuentaEmailRepository(OfticDbContext context, ILogger<DbCuentaEmailRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<DtoCuentaEmail> ConsultarCuentas(int idCuenta)
        {
            try
            {
                var query = _context.CuentasEmail.AsNoTracking();
                if (idCuenta > 0)
                {
                    query = query.Where(c => c.IdCuenta == idCuenta);
                }

                var lista = (from c in query
                             join d in _context.Dominios.AsNoTracking() on c.IdDominioGrupo equals d.IdDominio into doms
                             from d in doms.DefaultIfEmpty()
                             orderby c.NombreCuenta
                             select new DtoCuentaEmail
                             {
                                 IdCuenta = (int)c.IdCuenta,
                                 IdDominioGrupo = c.IdDominioGrupo ?? 0,
                                 NombreCuenta = c.NombreCuenta ?? "",
                                 Email = c.Email ?? "",
                                 ServidorSmtp = c.ServidorSmtp ?? "",
                                 Puerto = c.Puerto,
                                 UsuarioSmtp = c.UsuarioSmtp ?? "",
                                 ClaveSmtp = c.ClaveSmtp ?? "",
                                 UsarSsl = c.UsarSsl,
                                 Vigente = c.Vigente,
                                 NombreGrupo = d != null ? d.Descripcion ?? "" : "",
                                 FirmaHtml = c.FirmaHtml,
                                 ImagenEncabezado = c.ImagenEncabezado
                             }).ToList();

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar cuentas de email");
                throw;
            }
        }

        public bool GestionarCuenta(DtoCuentaEmailRequest request)
        {
            try
            {
                if (request.IdCuenta <= 0)
                {
                    var entity = new CuentaEmailEntity
                    {
                        IdDominioGrupo = request.IdDominioGrupo,
                        NombreCuenta = request.NombreCuenta,
                        Email = request.Email,
                        ServidorSmtp = string.IsNullOrWhiteSpace(request.ServidorSmtp) ? "smtp.office365.com" : request.ServidorSmtp,
                        Puerto = request.Puerto > 0 ? request.Puerto : 587,
                        UsuarioSmtp = string.IsNullOrWhiteSpace(request.UsuarioSmtp) ? request.Email : request.UsuarioSmtp,
                        ClaveSmtp = request.ClaveSmtp,
                        UsarSsl = request.UsarSsl,
                        Vigente = request.Vigente,
                        FirmaHtml = request.FirmaHtml,
                        ImagenEncabezado = request.ImagenEncabezado,
                        PrioridadAlta = 0,
                        AcuseRecibido = 0,
                        CopiaPrivada = 0,
                        UsuarioCrea = request.UsuarioAuditoria ?? "SISTEMA",
                        FechaCrea = DateTime.Now,
                        IpRegistro = request.IpAuditoria
                    };

                    _context.CuentasEmail.Add(entity);
                    _context.SaveChanges();
                    request.IdCuenta = (int)entity.IdCuenta;
                }
                else
                {
                    var entity = _context.CuentasEmail.FirstOrDefault(c => c.IdCuenta == request.IdCuenta);
                    if (entity == null) return false;

                    entity.IdDominioGrupo = request.IdDominioGrupo;
                    entity.NombreCuenta = request.NombreCuenta;
                    entity.Email = request.Email;
                    entity.ServidorSmtp = request.ServidorSmtp;
                    entity.Puerto = request.Puerto;
                    entity.UsuarioSmtp = request.UsuarioSmtp;
                    if (!string.IsNullOrEmpty(request.ClaveSmtp))
                    {
                        entity.ClaveSmtp = request.ClaveSmtp;
                    }
                    entity.UsarSsl = request.UsarSsl;
                    entity.Vigente = request.Vigente;
                    entity.FirmaHtml = request.FirmaHtml;
                    if (request.ImagenEncabezado != null)
                    {
                        entity.ImagenEncabezado = request.ImagenEncabezado;
                    }
                    entity.UsuarioModifica = request.UsuarioAuditoria ?? "SISTEMA";
                    entity.FechaModifica = DateTime.Now;
                    entity.IpRegistro = request.IpAuditoria;

                    _context.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al gestionar cuenta de email");
                throw;
            }
        }

        public bool EliminarCuenta(int idCuenta)
        {
            try
            {
                var entity = _context.CuentasEmail.FirstOrDefault(c => c.IdCuenta == idCuenta);
                if (entity == null) return false;

                entity.Vigente = 0;
                entity.FechaModifica = DateTime.Now;
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cuenta de email");
                throw;
            }
        }

        public List<DtoCuentaEmail> ConsultarCuentasPorUsuario(int idUsuario)
        {
            try
            {
                var lista = (from c in _context.CuentasEmail.AsNoTracking()
                             join ceu in _context.CuentasEmailUsuarios.AsNoTracking() on c.IdCuenta equals ceu.IdCuenta
                             where ceu.IdUsuario == idUsuario && ceu.Vigente == 1 && c.Vigente == 1
                             orderby c.NombreCuenta
                             select new DtoCuentaEmail
                             {
                                 IdCuenta = (int)c.IdCuenta,
                                 NombreCuenta = c.NombreCuenta ?? "",
                                 Email = c.Email ?? "",
                                 Vigente = 1
                             }).ToList();

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar cuentas autorizadas por usuario {IdUsuario}", idUsuario);
                throw;
            }
        }

        public List<DtoCuentaEmailUsuario> ConsultarAutorizaciones(int? idCuenta, int? idUsuario, int? vigente)
        {
            try
            {
                var query = _context.CuentasEmailUsuarios.AsNoTracking();

                if (idCuenta.HasValue) query = query.Where(ceu => ceu.IdCuenta == idCuenta.Value);
                if (idUsuario.HasValue) query = query.Where(ceu => ceu.IdUsuario == idUsuario.Value);
                if (vigente.HasValue) query = query.Where(ceu => ceu.Vigente == vigente.Value);

                var rawList = (from ceu in query
                               join c in _context.CuentasEmail.AsNoTracking() on ceu.IdCuenta equals c.IdCuenta
                               join u in _context.Usuarios.AsNoTracking() on ceu.IdUsuario equals u.IdUsuario into us
                               from u in us.DefaultIfEmpty()
                               orderby ceu.IdCuentaUsuario descending
                               select new
                               {
                                   Ceu = ceu,
                                   NombreCuenta = c.NombreCuenta,
                                   Email = c.Email,
                                   Username = u != null ? u.Username : null,
                                   Nombres = u != null ? u.Nombres : null,
                                   Apellidos = u != null ? u.Apellidos : null,
                                   Identificacion = u != null ? (long?)u.Identificacion : null
                               }).ToList();

                var lista = rawList.Select(x => new DtoCuentaEmailUsuario
                {
                    IdCuentaUsuario = (int)x.Ceu.IdCuentaUsuario,
                    IdCuenta = (int)x.Ceu.IdCuenta,
                    NombreCuenta = x.NombreCuenta ?? "",
                    Email = x.Email ?? "",
                    IdUsuario = (int)x.Ceu.IdUsuario,
                    UsuarioLogin = x.Username ?? "",
                    NombreCompleto = $"{x.Nombres} {x.Apellidos}".Trim(),
                    Identificacion = x.Identificacion.HasValue ? x.Identificacion.Value.ToString() : "",
                    Vigente = x.Ceu.Vigente,
                    UsuarioCrea = x.Ceu.UsuarioCrea ?? "",
                    FechaCrea = x.Ceu.FechaCrea,
                    UsuarioModifica = x.Ceu.UsuarioModifica ?? "",
                    FechaModifica = x.Ceu.FechaModifica,
                    IpRegistro = x.Ceu.IpRegistro ?? ""
                }).ToList();

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar autorizaciones de cuenta-email por usuario");
                throw;
            }
        }

        public bool GuardarAutorizacion(DtoCuentaEmailUsuarioRequest request)
        {
            try
            {
                var existing = _context.CuentasEmailUsuarios
                    .FirstOrDefault(ceu => ceu.IdCuenta == request.IdCuenta && ceu.IdUsuario == request.IdUsuario);

                if (existing != null)
                {
                    existing.Vigente = 1;
                    existing.UsuarioModifica = request.UsuarioAuditoria ?? "SISTEMA";
                    existing.FechaModifica = DateTime.Now;
                    existing.IpRegistro = request.IpAuditoria ?? "127.0.0.1";
                }
                else
                {
                    var newEntity = new CuentaEmailUsuarioEntity
                    {
                        IdCuenta = request.IdCuenta,
                        IdUsuario = request.IdUsuario,
                        Vigente = 1,
                        UsuarioCrea = request.UsuarioAuditoria ?? "SISTEMA",
                        FechaCrea = DateTime.Now,
                        IpRegistro = request.IpAuditoria ?? "127.0.0.1"
                    };
                    _context.CuentasEmailUsuarios.Add(newEntity);
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar autorización de cuenta-email por usuario");
                throw;
            }
        }

        public bool ActualizarAutorizacion(DtoCuentaEmailUsuarioEstadoRequest request)
        {
            try
            {
                var entity = _context.CuentasEmailUsuarios
                    .FirstOrDefault(ceu => ceu.IdCuentaUsuario == request.IdCuentaUsuario);

                if (entity == null) return false;

                entity.Vigente = request.Vigente;
                entity.UsuarioModifica = request.UsuarioAuditoria ?? "SISTEMA";
                entity.FechaModifica = DateTime.Now;
                entity.IpRegistro = request.IpAuditoria ?? "127.0.0.1";

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar autorización de cuenta-email por usuario");
                throw;
            }
        }

        public bool EliminarAutorizacion(DtoCuentaEmailUsuarioEstadoRequest request)
        {
            try
            {
                var entity = _context.CuentasEmailUsuarios
                    .FirstOrDefault(ceu => ceu.IdCuentaUsuario == request.IdCuentaUsuario);

                if (entity == null) return false;

                entity.Vigente = 0;
                entity.UsuarioModifica = request.UsuarioAuditoria ?? "SISTEMA";
                entity.FechaModifica = DateTime.Now;
                entity.IpRegistro = request.IpAuditoria ?? "127.0.0.1";

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar autorización de cuenta-email por usuario");
                throw;
            }
        }

        public bool ValidarAutorizacionEnvio(int idCuenta, int idUsuario)
        {
            try
            {
                var count = (from ceu in _context.CuentasEmailUsuarios.AsNoTracking()
                             join c in _context.CuentasEmail.AsNoTracking() on ceu.IdCuenta equals c.IdCuenta
                             where ceu.IdCuenta == idCuenta && ceu.IdUsuario == idUsuario && ceu.Vigente == 1 && c.Vigente == 1
                             select ceu).Count();

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando autorización de envío para cuenta {IdCuenta}, usuario {IdUsuario}", idCuenta, idUsuario);
                throw;
            }
        }

        public string GetNextRadicado(int idCuentaEmail)
        {
            try
            {
                int anioActual = DateTime.Now.Year;

                // 1. Unidad / Sigla desde Branding (CTR_BRANDING)
                string siglaBranding = "OFTIC";
                try
                {
                    var branding = _context.Brandings.AsNoTracking()
                        .Where(b => b.Vigente == 1 && !string.IsNullOrEmpty(b.Sigla))
                        .OrderByDescending(b => b.IdBranding)
                        .FirstOrDefault();

                    if (branding != null && !string.IsNullOrWhiteSpace(branding.Sigla))
                    {
                        siglaBranding = branding.Sigla.Trim().ToUpper();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener la sigla de Branding para el radicado. Se usará fallback.");
                }

                // 2. Abreviatura del Grupo / Área desde la tabla de Dominios (CTR_DOMINIOS)
                string abrevGrupo = "GEN";
                try
                {
                    var cuenta = _context.CuentasEmail.AsNoTracking()
                        .FirstOrDefault(c => c.IdCuenta == idCuentaEmail);

                    if (cuenta != null && cuenta.IdDominioGrupo.HasValue && cuenta.IdDominioGrupo.Value > 0)
                    {
                        var dominio = _context.Dominios.AsNoTracking()
                            .FirstOrDefault(d => d.IdDominio == cuenta.IdDominioGrupo.Value);

                        if (dominio != null)
                        {
                            if (!string.IsNullOrWhiteSpace(dominio.Abreviatura))
                            {
                                abrevGrupo = dominio.Abreviatura.Trim().ToUpper();
                            }
                            else if (!string.IsNullOrWhiteSpace(dominio.Descripcion))
                            {
                                var limpia = dominio.Descripcion.Trim().ToUpper();
                                var palabras = limpia.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                abrevGrupo = palabras.Length > 0 ? palabras[0] : "GEN";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener la abreviatura del grupo para el radicado. Se usará fallback.");
                }

                // 3. Consecutivo Anual (Se reinicia a 1 con cada cambio de año)
                int count = 0;
                try
                {
                    count = _context.EnviosCorreo.AsNoTracking()
                        .Count(e => e.FechaCrea.HasValue && e.FechaCrea.Value.Year == anioActual);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener el conteo de envíos para el año {Anio}.", anioActual);
                }

                int consecutivo = count + 1;

                // Estructura: [SIGLA_BRANDING]-[ABREVIATURA_DOMINIO]-[CONSECUTIVO]-[AÑO]
                return $"{siglaBranding}-{abrevGrupo}-{consecutivo:D6}-{anioActual}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar radicado para cuenta {Id}", idCuentaEmail);
                return $"OFTIC-GEN-000001-{DateTime.Now.Year}";
            }
        }

        public string GetSiglaSistema()
        {
            try
            {
                var branding = _context.Brandings.AsNoTracking()
                    .Where(b => b.Vigente == 1 && (!string.IsNullOrEmpty(b.Sistema) || !string.IsNullOrEmpty(b.Sigla)))
                    .OrderByDescending(b => b.IdBranding)
                    .FirstOrDefault();

                if (branding != null)
                {
                    if (!string.IsNullOrWhiteSpace(branding.Sistema))
                        return branding.Sistema.Trim().ToUpper();
                    if (!string.IsNullOrWhiteSpace(branding.Sigla))
                        return branding.Sigla.Trim().ToUpper();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener la sigla del sistema desde Branding. Se usará fallback.");
            }

            return "DYCK";
        }
    }
}
