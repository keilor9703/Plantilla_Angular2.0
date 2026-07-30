using Comun.Dtos.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Datos.Interfaz;
using Negocio.Interfaz;
using System.Security.Claims;
using System.Globalization;

namespace Api.Controllers.Administracion
{
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IDbMenuService _service;
        private readonly IDbUsuarioRepository _dbUsuarioRepository;
        private readonly ILogger<MenuController> _logger;
        private readonly HashSet<long> _superUserIds;

        public MenuController(
            IDbMenuService service,
            IDbUsuarioRepository dbUsuarioRepository,
            IConfiguration configuration,
            ILogger<MenuController> logger)
        {
            _service = service;
            _dbUsuarioRepository = dbUsuarioRepository;
            _logger = logger;
            _superUserIds = ResolveSuperUserIds(configuration);
        }

        [HttpGet("by-user/{idUsuario:long}")]
        [Authorize]
        public async Task<ActionResult<List<DtoMenuItem>>> GetByUser(long idUsuario, CancellationToken ct)
        {
            var menu = await GetMenuForUserAsync(idUsuario, ct);
            return Ok(menu);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<List<DtoMenuItem>>> GetMyMenu(CancellationToken ct)
        {
            var rawId = User.FindFirstValue("id_usuario")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid");

            if (!long.TryParse(rawId, out var idUsuario) || idUsuario <= 0)
            {
                _logger.LogWarning("No se pudo resolver id_usuario desde JWT.");
                return Unauthorized();
            }

            var menu = await GetMenuForUserAsync(idUsuario, ct);
            _logger.LogInformation("Menu por claim para idUsuario {IdUsuario}: {Count} items", idUsuario, menu.Count);
            return Ok(menu);
        }

        [HttpGet("admin")]
        [Authorize]
        public async Task<ActionResult<List<DtoMenuItem>>> GetAdminMenu(CancellationToken ct)
        {
            var menu = await _service.GetAdminMenuAsync(ct);
            return Ok(menu);
        }

        [HttpPost("admin")]
        [Authorize]
        public async Task<IActionResult> SaveMenu([FromBody] DtoMenuSaveRequest request, CancellationToken ct)
        {
            try
            {
                if (request is null)
                {
                    return BadRequest(new { success = false, message = "Payload requerido." });
                }

                if (string.IsNullOrWhiteSpace(request.Descripcion))
                {
                    return BadRequest(new { success = false, message = "DescripciÃ³n requerida." });
                }

                if (request.Posicion < 0)
                {
                    return BadRequest(new { success = false, message = "PosiciÃ³n invÃ¡lida." });
                }

                if (string.IsNullOrWhiteSpace(request.Tipo))
                {
                    return BadRequest(new { success = false, message = "Tipo requerido." });
                }

                if (request.Vigente is not 0 and not 1)
                {
                    return BadRequest(new { success = false, message = "Vigente debe ser 0 o 1." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.SaveMenuAsync(request, usuario, maquina, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando menÃº.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error guardando menú",
                    detail = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet("admin/roles")]
        [Authorize]
        public async Task<IActionResult> GetRolesCatalog(CancellationToken ct)
        {
            try
            {
                var roles = await _service.GetRolesCatalogAsync(ct);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando catÃ¡logo de roles para menÃº.");
                return StatusCode(500, new { success = false, message = "Error consultando catÃ¡logo de roles." });
            }
        }

        [HttpGet("admin/{idMenu:long}/roles")]
        [Authorize]
        public async Task<IActionResult> GetRolesByMenu(long idMenu, CancellationToken ct)
        {
            try
            {
                if (idMenu <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de menÃº invÃ¡lido." });
                }

                var roles = await _service.GetRolesByMenuAsync(idMenu, ct);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando roles por menÃº. idMenu={IdMenu}", idMenu);
                return StatusCode(500, new { success = false, message = "Error consultando roles por menÃº." });
            }
        }

        [HttpPost("admin/{idMenu:long}/roles")]
        [Authorize]
        public async Task<IActionResult> AssignRolToMenu(long idMenu, [FromBody] DtoAssignMenuRolRequest request, CancellationToken ct)
        {
            try
            {
                if (idMenu <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de menÃº invÃ¡lido." });
                }

                if (request is null || request.IdRol <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de rol invÃ¡lido." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.AssignRolToMenuAsync(idMenu, request.IdRol, usuario, maquina, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando rol a menÃº. idMenu={IdMenu}", idMenu);
                return StatusCode(500, new { success = false, message = "Error asignando rol a menÃº." });
            }
        }

        [HttpDelete("admin/{idMenu:long}/roles/{idRol:int}")]
        [Authorize]
        public async Task<IActionResult> RemoveRolFromMenu(long idMenu, int idRol, CancellationToken ct)
        {
            try
            {
                if (idMenu <= 0 || idRol <= 0)
                {
                    return BadRequest(new { success = false, message = "ParÃ¡metros invÃ¡lidos." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.RemoveRolFromMenuAsync(idMenu, idRol, usuario, maquina, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando asignaciÃ³n menÃº-rol. idMenu={IdMenu}, idRol={IdRol}", idMenu, idRol);
                return StatusCode(500, new { success = false, message = "Error eliminando asignaciÃ³n menÃº-rol." });
            }
        }

        [HttpGet("admin/roles/{idRol:int}/menus")]
        [Authorize]
        public async Task<IActionResult> GetMenusByRol(int idRol, CancellationToken ct)
        {
            try
            {
                if (idRol <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de rol invÃ¡lido." });
                }

                var menus = await _service.GetMenusByRolAsync(idRol, ct);
                return Ok(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando menÃºs por rol. idRol={IdRol}", idRol);
                return StatusCode(500, new { success = false, message = "Error consultando menÃºs por rol." });
            }
        }

        [HttpPost("admin/roles/{idRol:int}/menus")]
        [Authorize]
        public async Task<IActionResult> AssignMenuToRol(int idRol, [FromBody] DtoAssignRoleMenuRequest request, CancellationToken ct)
        {
            try
            {
                if (idRol <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de rol invÃ¡lido." });
                }

                if (request is null || request.IdMenu <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de menÃº invÃ¡lido." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.AssignMenuToRolAsync(idRol, request.IdMenu, usuario, maquina, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando menú a rol. idRol={IdRol}", idRol);
                return StatusCode(500, new { success = false, message = "Error asignando menú a rol." });
            }
        }

        [HttpDelete("admin/roles/{idRol:int}/menus/{idMenu:long}")]
        [Authorize]
        public async Task<IActionResult> RemoveMenuFromRol(int idRol, long idMenu, CancellationToken ct)
        {
            try
            {
                if (idRol <= 0 || idMenu <= 0)
                {
                    return BadRequest(new { success = false, message = "Parámetros inválidos." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.RemoveMenuFromRolAsync(idRol, idMenu, usuario, maquina, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando asignaciÃ³n rol-menÃº. idRol={IdRol}, idMenu={IdMenu}", idRol, idMenu);
                return StatusCode(500, new { success = false, message = "Error eliminando asignaciÃ³n rol-menÃº." });
            }
        }

        [HttpPatch("admin/{idMenu:long}/estado")]
        [Authorize]
        public async Task<IActionResult> SetEstado(long idMenu, [FromBody] DtoMenuEstadoRequest request, CancellationToken ct)
        {
            try
            {
                if (idMenu <= 0)
                {
                    return BadRequest(new { success = false, message = "Id de menÃº invÃ¡lido." });
                }

                if (request is null || request.Vigente is not 0 and not 1)
                {
                    return BadRequest(new { success = false, message = "Vigente debe ser 0 o 1." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _service.SetEstadoMenuAsync(idMenu, request.Vigente, usuario, maquina, request.Observaciones, ct);

                if (result.Id <= 0)
                {
                    return BadRequest(new { success = false, id = result.Id, message = result.Message });
                }

                return Ok(new { success = true, id = result.Id, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado de menÃº.");
                return StatusCode(500, new { success = false, message = "Error actualizando estado" });
            }
        }

        private (string usuario, string maquina) ObtenerAuditoria()
        {
            var usuario = User.Identity?.Name
                ?? User.FindFirstValue("username")
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("identificacion")
                ?? User.FindFirstValue("cedula")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "SISTEMA";

            var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? Environment.MachineName
                ?? "N/A";

            return (usuario, maquina);
        }

        private async Task<List<DtoMenuItem>> GetMenuForUserAsync(long idUsuario, CancellationToken ct)
        {
            if (IsSuperUser(idUsuario))
            {
                var adminMenu = await _service.GetAdminMenuAsync(ct);
                var filtered = adminMenu.Where(x => x.Vigente == 1).ToList();
                _logger.LogInformation("Regla superusuario aplicada para idUsuario {IdUsuario}. MenÃº admin vigente: {Count}", idUsuario, filtered.Count);
                return filtered;
            }

            return await _service.GetMyMenuAsync(idUsuario, ct);
        }

        private bool IsSuperUser(long idUsuario)
        {
            return _superUserIds.Contains(idUsuario);
        }

        private static HashSet<long> ResolveSuperUserIds(IConfiguration configuration)
        {
            var ids = new HashSet<long> { 1 }; // fallback por defecto

            var configured = configuration["Menu:SuperUserIds"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return ids;
            }

            var values = configured
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var value in values)
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
    }
}


