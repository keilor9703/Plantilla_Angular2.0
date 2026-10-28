using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comun.Dtos.Mfa;
using Comun.Extensions;
using Datos.Interfaz.Mfa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Datos.Gestion.Mfa
{
    /// <summary>
    /// Repositorio del panel SI2FA sobre la base de datos PostgreSQL compartida del servicio 2FA.
    /// Requiere DatabaseProvider = PostgreSQL (el esquema mfa_* es específico de PostgreSQL).
    /// </summary>
    public class MfaAdminRepository : IMfaAdminRepository
    {
        private const string SISTEMA_ADMIN = "SI2FA-ADMIN";
        private const string MASCARA_SECRETO = "••••••••";

        private readonly string _cs;
        private readonly ILogger<MfaAdminRepository> _logger;

        public MfaAdminRepository(IConfiguration configuration, ILogger<MfaAdminRepository> logger)
        {
            _cs = configuration.GetDecryptedConnectionString();
            _logger = logger;
        }

        private async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct)
        {
            var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync(ct);
            return conn;
        }

        // ============================ MÉTRICAS ============================
        public async Task<DtoMetricas2Fa> GetMetricasAsync(int ventanaDias, CancellationToken ct)
        {
            if (ventanaDias <= 0) ventanaDias = 30;
            var m = new DtoMetricas2Fa { VentanaDias = ventanaDias };

            await using var conn = await AbrirAsync(ct);

            m.TotalUsuarios = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_usuarios", ct);
            m.UsuariosHabilitados = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_usuarios WHERE mfa_habilitado = 1", ct);
            m.UsuariosEnrolados = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_usuarios WHERE totp_secret_enc IS NOT NULL", ct);
            m.UsuariosPendientesEnrolar = await ScalarLong(conn,
                "SELECT COUNT(*) FROM mfa_usuarios WHERE mfa_habilitado = 1 AND (totp_secret_enc IS NULL OR require_reenroll = 1)", ct);
            m.UsuariosBloqueados = await ScalarLong(conn,
                "SELECT COUNT(*) FROM mfa_usuarios WHERE bloqueo_hasta IS NOT NULL AND bloqueo_hasta > CURRENT_TIMESTAMP", ct);

            m.SistemasTotales = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_sistemas", ct);
            m.SistemasActivos = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_sistemas WHERE habilitado = 1", ct);
            m.DispositivosConfiablesActivos = await ScalarLong(conn,
                "SELECT COUNT(*) FROM mfa_trusted_devices WHERE expira_at > CURRENT_TIMESTAMP", ct);

            const string wnd = " AND creado_at >= CURRENT_TIMESTAMP - make_interval(days => @d)";
            m.VerificacionesOk = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_auditoria WHERE evento = 'VERIFY_OK'" + wnd, ct, ventanaDias);
            m.VerificacionesFallidas = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_auditoria WHERE evento = 'VERIFY_FAIL'" + wnd, ct, ventanaDias);
            m.Enrolamientos = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_auditoria WHERE evento = 'ENROLL_OK'" + wnd, ct, ventanaDias);
            m.Restablecimientos = await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_auditoria WHERE evento LIKE 'RESET%'" + wnd, ct, ventanaDias);

            // Actividad por sistema
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT COALESCE(NULLIF(TRIM(sistema), ''), '(desconocido)') AS s, COUNT(*) AS t " +
                    "FROM mfa_auditoria WHERE creado_at >= CURRENT_TIMESTAMP - make_interval(days => @d) " +
                    "GROUP BY 1 ORDER BY 2 DESC LIMIT 20";
                cmd.Parameters.AddWithValue("d", ventanaDias);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    m.ActividadPorSistema.Add(new DtoConteoClave { Clave = r.GetString(0), Total = Convert.ToInt64(r.GetValue(1)) });
            }

            // Eventos por tipo
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT COALESCE(NULLIF(TRIM(evento), ''), '(sin evento)') AS e, COUNT(*) AS t " +
                    "FROM mfa_auditoria WHERE creado_at >= CURRENT_TIMESTAMP - make_interval(days => @d) " +
                    "GROUP BY 1 ORDER BY 2 DESC";
                cmd.Parameters.AddWithValue("d", ventanaDias);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    m.EventosPorTipo.Add(new DtoConteoClave { Clave = r.GetString(0), Total = Convert.ToInt64(r.GetValue(1)) });
            }

            // Serie diaria
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT to_char(creado_at::date, 'YYYY-MM-DD') AS d, " +
                    "COUNT(*) FILTER (WHERE evento = 'VERIFY_OK') AS ok, " +
                    "COUNT(*) FILTER (WHERE evento = 'VERIFY_FAIL') AS fail, " +
                    "COUNT(*) AS total " +
                    "FROM mfa_auditoria WHERE creado_at >= CURRENT_TIMESTAMP - make_interval(days => @d) " +
                    "GROUP BY 1 ORDER BY 1";
                cmd.Parameters.AddWithValue("d", ventanaDias);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    m.SerieDiaria.Add(new DtoSerieTiempo
                    {
                        Fecha = r.GetString(0),
                        VerificacionesOk = Convert.ToInt64(r.GetValue(1)),
                        VerificacionesFallidas = Convert.ToInt64(r.GetValue(2)),
                        Total = Convert.ToInt64(r.GetValue(3))
                    });
            }

            return m;
        }

        // ============================ SISTEMAS ============================
        public async Task<List<DtoSistema2Fa>> GetSistemasAsync(CancellationToken ct)
        {
            var list = new List<DtoSistema2Fa>();
            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT id_sistema, sigla, nombre, descripcion, habilitado, creado_at, actualizado_at, rotado_at, " +
                "usuario_creacion, usuario_modifica FROM mfa_sistemas ORDER BY sigla";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(MapSistema(r));
            return list;
        }

        public async Task<DtoSistema2Fa?> GetSistemaAsync(long idSistema, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT id_sistema, sigla, nombre, descripcion, habilitado, creado_at, actualizado_at, rotado_at, " +
                "usuario_creacion, usuario_modifica FROM mfa_sistemas WHERE id_sistema = @id";
            cmd.Parameters.AddWithValue("id", idSistema);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct)) return MapSistema(r);
            return null;
        }

        public async Task<DtoSistema2FaResult> CrearSistemaAsync(DtoSistema2FaRequest req, string actor, CancellationToken ct)
        {
            var sigla = (req.Sigla ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sigla))
                return new DtoSistema2FaResult { Success = false, Message = "La sigla del sistema es obligatoria." };
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return new DtoSistema2FaResult { Success = false, Message = "El nombre del sistema es obligatorio." };

            await using var conn = await AbrirAsync(ct);

            if (await ScalarLong(conn, "SELECT COUNT(*) FROM mfa_sistemas WHERE UPPER(sigla) = UPPER(@s)", ct, sigla) > 0)
                return new DtoSistema2FaResult { Success = false, Message = $"Ya existe un sistema con la sigla '{sigla}'." };

            var secreto = GenerarSecreto();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO mfa_sistemas (sigla, nombre, descripcion, secret_hmac, habilitado, usuario_creacion) " +
                "VALUES (@sigla, @nombre, @descripcion, @secret, @habilitado, @actor) RETURNING id_sistema";
            cmd.Parameters.AddWithValue("sigla", sigla);
            cmd.Parameters.AddWithValue("nombre", req.Nombre.Trim());
            cmd.Parameters.AddWithValue("descripcion", (object?)req.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("secret", secreto);
            cmd.Parameters.AddWithValue("habilitado", req.Habilitado == 0 ? 0 : 1);
            cmd.Parameters.AddWithValue("actor", actor ?? "SI2FA");
            var id = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));

            return new DtoSistema2FaResult { Success = true, Id = id, Secreto = secreto, Message = "Sistema creado. Copie el secreto; no se mostrará de nuevo." };
        }

        public async Task<DtoSistema2FaResult> ActualizarSistemaAsync(long idSistema, DtoSistema2FaRequest req, string actor, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return new DtoSistema2FaResult { Success = false, Message = "El nombre del sistema es obligatorio." };

            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            // La sigla es inmutable (se usa para firmar). Solo se actualizan nombre, descripción y estado.
            cmd.CommandText =
                "UPDATE mfa_sistemas SET nombre = @nombre, descripcion = @descripcion, habilitado = @habilitado, " +
                "usuario_modifica = @actor, actualizado_at = CURRENT_TIMESTAMP WHERE id_sistema = @id";
            cmd.Parameters.AddWithValue("nombre", req.Nombre.Trim());
            cmd.Parameters.AddWithValue("descripcion", (object?)req.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("habilitado", req.Habilitado == 0 ? 0 : 1);
            cmd.Parameters.AddWithValue("actor", actor ?? "SI2FA");
            cmd.Parameters.AddWithValue("id", idSistema);
            var n = await cmd.ExecuteNonQueryAsync(ct);
            return n > 0
                ? new DtoSistema2FaResult { Success = true, Id = idSistema, Message = "Sistema actualizado." }
                : new DtoSistema2FaResult { Success = false, Message = "Sistema no encontrado." };
        }

        public async Task<DtoSistema2FaResult> CambiarEstadoSistemaAsync(long idSistema, int habilitado, string actor, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE mfa_sistemas SET habilitado = @h, usuario_modifica = @actor, actualizado_at = CURRENT_TIMESTAMP " +
                "WHERE id_sistema = @id";
            cmd.Parameters.AddWithValue("h", habilitado == 0 ? 0 : 1);
            cmd.Parameters.AddWithValue("actor", actor ?? "SI2FA");
            cmd.Parameters.AddWithValue("id", idSistema);
            var n = await cmd.ExecuteNonQueryAsync(ct);
            return n > 0
                ? new DtoSistema2FaResult { Success = true, Id = idSistema, Message = habilitado == 0 ? "Sistema deshabilitado." : "Sistema habilitado." }
                : new DtoSistema2FaResult { Success = false, Message = "Sistema no encontrado." };
        }

        public async Task<DtoSistema2FaResult> RotarSecretoAsync(long idSistema, string actor, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            var secreto = GenerarSecreto();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE mfa_sistemas SET secret_hmac = @s, rotado_at = CURRENT_TIMESTAMP, usuario_modifica = @actor, " +
                "actualizado_at = CURRENT_TIMESTAMP WHERE id_sistema = @id";
            cmd.Parameters.AddWithValue("s", secreto);
            cmd.Parameters.AddWithValue("actor", actor ?? "SI2FA");
            cmd.Parameters.AddWithValue("id", idSistema);
            var n = await cmd.ExecuteNonQueryAsync(ct);
            return n > 0
                ? new DtoSistema2FaResult { Success = true, Id = idSistema, Secreto = secreto, Message = "Secreto rotado. Copie el nuevo valor; no se mostrará de nuevo." }
                : new DtoSistema2FaResult { Success = false, Message = "Sistema no encontrado." };
        }

        // ============================ USUARIOS ============================
        private const string USUARIO_COLS =
            "u.id_mfa, u.identificacion, u.usuario_institucional, u.mfa_habilitado, u.require_reenroll, " +
            "(u.totp_secret_enc IS NOT NULL) AS enrolado, u.intentos_fallidos, u.bloqueo_hasta, u.ultimo_ok, " +
            "(SELECT COUNT(*) FROM mfa_trusted_devices d WHERE d.id_mfa = u.id_mfa AND d.expira_at > CURRENT_TIMESTAMP) AS disp, " +
            "u.creado_at, u.actualizado_at";

        public async Task<List<DtoUsuario2Fa>> GetUsuariosAsync(string? filtro, int pagina, int tamano, CancellationToken ct)
        {
            if (pagina <= 0) pagina = 1;
            if (tamano <= 0 || tamano > 200) tamano = 50;

            var list = new List<DtoUsuario2Fa>();
            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            var where = string.Empty;
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                where = " WHERE (u.usuario_institucional ILIKE @f OR CAST(u.identificacion AS TEXT) LIKE @f)";
                cmd.Parameters.AddWithValue("f", "%" + filtro.Trim() + "%");
            }
            cmd.CommandText =
                $"SELECT {USUARIO_COLS} FROM mfa_usuarios u{where} " +
                "ORDER BY u.actualizado_at DESC NULLS LAST, u.id_mfa DESC LIMIT @lim OFFSET @off";
            cmd.Parameters.AddWithValue("lim", tamano);
            cmd.Parameters.AddWithValue("off", (pagina - 1) * tamano);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(MapUsuario(r));
            return list;
        }

        public async Task<DtoUsuario2FaDetalle?> GetUsuarioAsync(long idMfa, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            DtoUsuario2FaDetalle? det = null;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT {USUARIO_COLS} FROM mfa_usuarios u WHERE u.id_mfa = @id";
                cmd.Parameters.AddWithValue("id", idMfa);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    var u = MapUsuario(r);
                    det = new DtoUsuario2FaDetalle
                    {
                        IdMfa = u.IdMfa,
                        Identificacion = u.Identificacion,
                        UsuarioInstitucional = u.UsuarioInstitucional,
                        MfaHabilitado = u.MfaHabilitado,
                        RequireReenroll = u.RequireReenroll,
                        Enrolado = u.Enrolado,
                        IntentosFallidos = u.IntentosFallidos,
                        BloqueoHasta = u.BloqueoHasta,
                        Bloqueado = u.Bloqueado,
                        UltimoOk = u.UltimoOk,
                        DispositivosConfiables = u.DispositivosConfiables,
                        CreadoAt = u.CreadoAt,
                        ActualizadoAt = u.ActualizadoAt
                    };
                }
            }
            if (det == null) return null;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT id_device, id_mfa, device_hash, expira_at, creado_at FROM mfa_trusted_devices " +
                    "WHERE id_mfa = @id ORDER BY creado_at DESC";
                cmd.Parameters.AddWithValue("id", idMfa);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    var exp = GetDtN(r, 3);
                    det.Dispositivos.Add(new DtoDispositivo2Fa
                    {
                        IdDevice = GetLong(r, 0),
                        IdMfa = GetLong(r, 1),
                        DeviceHash = GetStrN(r, 2),
                        ExpiraAt = exp,
                        Vigente = exp.HasValue && exp.Value > DateTime.Now,
                        CreadoAt = GetDtN(r, 4)
                    });
                }
            }
            return det;
        }

        public Task<DtoAccion2FaResult> DesbloquearAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            EjecutarAccionUsuario(idMfa, actor, ip,
                "UPDATE mfa_usuarios SET intentos_fallidos = 0, bloqueo_hasta = NULL, actualizado_at = CURRENT_TIMESTAMP " +
                "WHERE id_mfa = @id RETURNING identificacion, usuario_institucional",
                "ADMIN_UNLOCK", "Desbloqueo manual del segundo factor.", "Usuario desbloqueado.", ct);

        public Task<DtoAccion2FaResult> ForzarReenrolAsync(long idMfa, string actor, string ip, CancellationToken ct) =>
            EjecutarAccionUsuario(idMfa, actor, ip,
                "UPDATE mfa_usuarios SET require_reenroll = 1, actualizado_at = CURRENT_TIMESTAMP " +
                "WHERE id_mfa = @id RETURNING identificacion, usuario_institucional",
                "ADMIN_FORCE_REENROLL", "Se forzó el re-enrolamiento del segundo factor.", "Se marcó el re-enrolamiento.", ct);

        public async Task<DtoAccion2FaResult> CambiarEstadoMfaAsync(long idMfa, int estado, string actor, string ip, CancellationToken ct)
        {
            var habilitar = estado != 0;
            return await EjecutarAccionUsuario(idMfa, actor, ip,
                "UPDATE mfa_usuarios SET mfa_habilitado = @estado, actualizado_at = CURRENT_TIMESTAMP " +
                "WHERE id_mfa = @id RETURNING identificacion, usuario_institucional",
                "ADMIN_CHANGE_MFA",
                habilitar ? "MFA habilitado por administrador." : "MFA deshabilitado por administrador.",
                habilitar ? "MFA habilitado." : "MFA deshabilitado.",
                ct, ("estado", habilitar ? 1 : 0));
        }

        public async Task<DtoAccion2FaResult> ResetMfaAsync(long idMfa, string actor, string ip, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);
            long ident; string? usuario;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = (NpgsqlTransaction)tx;
                cmd.CommandText =
                    "UPDATE mfa_usuarios SET totp_secret_enc = NULL, require_reenroll = 1, mfa_habilitado = 1, " +
                    "intentos_fallidos = 0, bloqueo_hasta = NULL, reset_estado = 0, reset_code_hash = NULL, " +
                    "reset_code_expires_at = NULL, reset_intentos = 0, reset_confirmed_at = NULL, reset_available_at = NULL, " +
                    "actualizado_at = CURRENT_TIMESTAMP WHERE id_mfa = @id RETURNING identificacion, usuario_institucional";
                cmd.Parameters.AddWithValue("id", idMfa);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct))
                {
                    await r.CloseAsync();
                    await tx.RollbackAsync(ct);
                    return new DtoAccion2FaResult { Success = false, Message = "Usuario no encontrado." };
                }
                ident = GetLong(r, 0);
                usuario = GetStrN(r, 1);
            }
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = (NpgsqlTransaction)tx;
                cmd.CommandText = "DELETE FROM mfa_trusted_devices WHERE id_mfa = @id";
                cmd.Parameters.AddWithValue("id", idMfa);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await Audit(conn, (NpgsqlTransaction)tx, ident, usuario, "ADMIN_RESET",
                $"Restablecimiento total del MFA (CSIRT: {actor}).", ip, ct);
            await tx.CommitAsync(ct);
            return new DtoAccion2FaResult { Success = true, Message = "MFA restablecido. El usuario deberá volver a enrolarse." };
        }

        public async Task<DtoAccion2FaResult> LimpiarDispositivosAsync(long idMfa, string actor, string ip, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            long ident; string? usuario;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT identificacion, usuario_institucional FROM mfa_usuarios WHERE id_mfa = @id";
                cmd.Parameters.AddWithValue("id", idMfa);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct))
                    return new DtoAccion2FaResult { Success = false, Message = "Usuario no encontrado." };
                ident = GetLong(r, 0);
                usuario = GetStrN(r, 1);
            }
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM mfa_trusted_devices WHERE id_mfa = @id";
                cmd.Parameters.AddWithValue("id", idMfa);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await Audit(conn, null, ident, usuario, "ADMIN_TRUST_CLEAR",
                $"Se eliminaron los dispositivos confiables (CSIRT: {actor}).", ip, ct);
            return new DtoAccion2FaResult { Success = true, Message = "Dispositivos confiables eliminados." };
        }

        // ============================ DISPOSITIVOS ============================
        public async Task<List<DtoDispositivo2Fa>> GetDispositivosAsync(long? idMfa, bool soloVigentes, CancellationToken ct)
        {
            var list = new List<DtoDispositivo2Fa>();
            await using var conn = await AbrirAsync(ct);
            await using var cmd = conn.CreateCommand();
            var where = new List<string>();
            if (idMfa.HasValue) { where.Add("d.id_mfa = @id"); cmd.Parameters.AddWithValue("id", idMfa.Value); }
            if (soloVigentes) where.Add("d.expira_at > CURRENT_TIMESTAMP");
            var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
            cmd.CommandText =
                "SELECT d.id_device, d.id_mfa, d.device_hash, d.expira_at, d.creado_at, u.identificacion, u.usuario_institucional " +
                "FROM mfa_trusted_devices d JOIN mfa_usuarios u ON u.id_mfa = d.id_mfa" + whereSql +
                " ORDER BY d.creado_at DESC LIMIT 500";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var exp = GetDtN(r, 3);
                list.Add(new DtoDispositivo2Fa
                {
                    IdDevice = GetLong(r, 0),
                    IdMfa = GetLong(r, 1),
                    DeviceHash = GetStrN(r, 2),
                    ExpiraAt = exp,
                    Vigente = exp.HasValue && exp.Value > DateTime.Now,
                    CreadoAt = GetDtN(r, 4),
                    Identificacion = GetLong(r, 5),
                    UsuarioInstitucional = GetStrN(r, 6)
                });
            }
            return list;
        }

        public async Task<DtoAccion2FaResult> RevocarDispositivoAsync(long idDevice, string actor, string ip, CancellationToken ct)
        {
            await using var conn = await AbrirAsync(ct);
            long ident = 0; string? usuario = null; bool encontrado = false;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM mfa_trusted_devices d USING mfa_usuarios u " +
                    "WHERE d.id_device = @id AND u.id_mfa = d.id_mfa " +
                    "RETURNING u.identificacion, u.usuario_institucional";
                cmd.Parameters.AddWithValue("id", idDevice);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    encontrado = true;
                    ident = GetLong(r, 0);
                    usuario = GetStrN(r, 1);
                }
            }
            if (!encontrado)
                return new DtoAccion2FaResult { Success = false, Message = "Dispositivo no encontrado." };
            await Audit(conn, null, ident, usuario, "ADMIN_TRUST_REVOKE",
                $"Se revocó un dispositivo confiable (CSIRT: {actor}).", ip, ct);
            return new DtoAccion2FaResult { Success = true, Message = "Dispositivo revocado." };
        }

        // ============================ AUDITORÍA ============================
        public async Task<DtoAuditoria2FaPagina> GetAuditoriaAsync(DtoAuditoria2FaFiltro filtro, CancellationToken ct)
        {
            var pagina = filtro.Pagina <= 0 ? 1 : filtro.Pagina;
            var tamano = (filtro.TamanoPagina <= 0 || filtro.TamanoPagina > 500) ? 50 : filtro.TamanoPagina;

            var res = new DtoAuditoria2FaPagina { Pagina = pagina, TamanoPagina = tamano };
            await using var conn = await AbrirAsync(ct);

            var where = new List<string>();
            void AddP(NpgsqlCommand c)
            {
                if (filtro.Identificacion.HasValue) c.Parameters.AddWithValue("ident", filtro.Identificacion.Value);
                if (!string.IsNullOrWhiteSpace(filtro.Usuario)) c.Parameters.AddWithValue("usr", "%" + filtro.Usuario.Trim() + "%");
                if (!string.IsNullOrWhiteSpace(filtro.Sistema)) c.Parameters.AddWithValue("sis", "%" + filtro.Sistema.Trim() + "%");
                if (!string.IsNullOrWhiteSpace(filtro.Evento)) c.Parameters.AddWithValue("ev", "%" + filtro.Evento.Trim() + "%");
                if (filtro.Desde.HasValue) c.Parameters.AddWithValue("desde", filtro.Desde.Value);
                if (filtro.Hasta.HasValue) c.Parameters.AddWithValue("hasta", filtro.Hasta.Value);
            }
            if (filtro.Identificacion.HasValue) where.Add("identificacion = @ident");
            if (!string.IsNullOrWhiteSpace(filtro.Usuario)) where.Add("usuario_institucional ILIKE @usr");
            if (!string.IsNullOrWhiteSpace(filtro.Sistema)) where.Add("sistema ILIKE @sis");
            if (!string.IsNullOrWhiteSpace(filtro.Evento)) where.Add("evento ILIKE @ev");
            if (filtro.Desde.HasValue) where.Add("creado_at >= @desde");
            if (filtro.Hasta.HasValue) where.Add("creado_at <= @hasta");
            var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM mfa_auditoria" + whereSql;
                AddP(cmd);
                var o = await cmd.ExecuteScalarAsync(ct);
                res.Total = o == null || o is DBNull ? 0 : Convert.ToInt64(o);
            }
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT id_auditoria, identificacion, usuario_institucional, sistema, evento, detalle, ip_origen, creado_at " +
                    "FROM mfa_auditoria" + whereSql + " ORDER BY creado_at DESC, id_auditoria DESC LIMIT @lim OFFSET @off";
                AddP(cmd);
                cmd.Parameters.AddWithValue("lim", tamano);
                cmd.Parameters.AddWithValue("off", (pagina - 1) * tamano);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    res.Items.Add(new DtoAuditoria2Fa
                    {
                        IdAuditoria = GetLong(r, 0),
                        Identificacion = r.IsDBNull(1) ? (long?)null : Convert.ToInt64(r.GetValue(1)),
                        UsuarioInstitucional = GetStrN(r, 2),
                        Sistema = GetStrN(r, 3),
                        Evento = GetStrN(r, 4),
                        Detalle = GetStrN(r, 5),
                        IpOrigen = GetStrN(r, 6),
                        CreadoAt = GetDtN(r, 7)
                    });
            }
            return res;
        }

        // ============================ HELPERS ============================
        private async Task<DtoAccion2FaResult> EjecutarAccionUsuario(
            long idMfa, string actor, string ip, string updateSql, string evento, string detalle, string okMsg,
            CancellationToken ct, params (string name, object value)[] extraParams)
        {
            await using var conn = await AbrirAsync(ct);
            long ident; string? usuario;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = updateSql;
                cmd.Parameters.AddWithValue("id", idMfa);
                foreach (var p in extraParams) cmd.Parameters.AddWithValue(p.name, p.value);
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct))
                    return new DtoAccion2FaResult { Success = false, Message = "Usuario no encontrado." };
                ident = GetLong(r, 0);
                usuario = GetStrN(r, 1);
            }
            await Audit(conn, null, ident, usuario, evento, $"{detalle} (CSIRT: {actor})", ip, ct);
            return new DtoAccion2FaResult { Success = true, Message = okMsg };
        }

        private static async Task Audit(NpgsqlConnection conn, NpgsqlTransaction? tx, long ident, string? usuario,
            string evento, string detalle, string ip, CancellationToken ct)
        {
            try
            {
                await using var cmd = conn.CreateCommand();
                if (tx != null) cmd.Transaction = tx;
                cmd.CommandText = "CALL p_audit(@id, @usr, @sis, @ev, @det, @ip)";
                cmd.Parameters.AddWithValue("id", ident);
                cmd.Parameters.AddWithValue("usr", (object?)usuario ?? DBNull.Value);
                cmd.Parameters.AddWithValue("sis", SISTEMA_ADMIN);
                cmd.Parameters.AddWithValue("ev", evento);
                cmd.Parameters.AddWithValue("det", detalle ?? string.Empty);
                cmd.Parameters.AddWithValue("ip", (object?)ip ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception)
            {
                // La auditoría no debe tumbar la operación principal; se ignora el fallo de registro.
            }
        }

        private static async Task<long> ScalarLong(NpgsqlConnection conn, string sql, CancellationToken ct, object? p = null)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (p != null)
            {
                // Convención: el único parámetro posicional es @d (ventana en días) o @s (texto).
                if (sql.Contains("@d")) cmd.Parameters.AddWithValue("d", p);
                if (sql.Contains("@s")) cmd.Parameters.AddWithValue("s", p);
            }
            var o = await cmd.ExecuteScalarAsync(ct);
            return o == null || o is DBNull ? 0 : Convert.ToInt64(o);
        }

        private static string GenerarSecreto()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static DtoSistema2Fa MapSistema(DbDataReader r) => new DtoSistema2Fa
        {
            IdSistema = GetLong(r, 0),
            Sigla = r.IsDBNull(1) ? string.Empty : r.GetString(1),
            Nombre = r.IsDBNull(2) ? string.Empty : r.GetString(2),
            Descripcion = GetStrN(r, 3),
            Habilitado = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
            CreadoAt = GetDtN(r, 5),
            ActualizadoAt = GetDtN(r, 6),
            RotadoAt = GetDtN(r, 7),
            UsuarioCreacion = GetStrN(r, 8),
            UsuarioModifica = GetStrN(r, 9),
            SecretoEnmascarado = MASCARA_SECRETO
        };

        private static DtoUsuario2Fa MapUsuario(DbDataReader r)
        {
            var bloqueo = GetDtN(r, 7);
            return new DtoUsuario2Fa
            {
                IdMfa = GetLong(r, 0),
                Identificacion = GetLong(r, 1),
                UsuarioInstitucional = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                MfaHabilitado = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                RequireReenroll = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                Enrolado = !r.IsDBNull(5) && Convert.ToBoolean(r.GetValue(5)),
                IntentosFallidos = r.IsDBNull(6) ? 0 : Convert.ToInt32(r.GetValue(6)),
                BloqueoHasta = bloqueo,
                Bloqueado = bloqueo.HasValue && bloqueo.Value > DateTime.Now,
                UltimoOk = GetDtN(r, 8),
                DispositivosConfiables = r.IsDBNull(9) ? 0 : Convert.ToInt32(r.GetValue(9)),
                CreadoAt = GetDtN(r, 10),
                ActualizadoAt = GetDtN(r, 11)
            };
        }

        private static long GetLong(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt64(r.GetValue(i));
        private static string? GetStrN(DbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetValue(i)?.ToString();
        private static DateTime? GetDtN(DbDataReader r, int i) => r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i);
    }
}
