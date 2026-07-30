using Comun.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Negocio.Interfaz;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Controllers.Administracion
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Route("api/Usuario/Roles/Admin")]
    public class RolController : ControllerBase
    {
        private readonly IDbRolService _rolService;
        private readonly ILogger<RolController> _logger;

        public RolController(IDbRolService rolService, ILogger<RolController> logger)
        {
            _rolService = rolService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var roles = await _rolService.GetRolesAdminAsync(HttpContext.RequestAborted);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando catálogo de roles.");
                return StatusCode(500, new { success = false, message = "Error al consultar roles." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] DtoSaveRolRequest request)
        {
            try
            {
                if (request is null)
                {
                    return BadRequest(new { success = false, message = "Payload inválido." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _rolService.SaveRolAdminAsync(request, usuario, maquina, HttpContext.RequestAborted);

                if (result.idRol <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.message) ? "No fue posible guardar el rol." : result.message
                    });
                }

                return Ok(new
                {
                    success = true,
                    idRol = result.idRol,
                    message = string.IsNullOrWhiteSpace(result.message) ? "Rol guardado correctamente." : result.message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando rol");
                return StatusCode(500, new { success = false, message = "Error al guardar rol." });
            }
        }

        [HttpPatch("{idRol}/Estado")]
        public async Task<IActionResult> SetEstado([FromRoute] long idRol, [FromBody] DtoSetRolEstadoRequest request)
        {
            try
            {
                if (request is null)
                {
                    return BadRequest(new { success = false, message = "Payload inválido." });
                }

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _rolService.SetRolEstadoAsync(idRol, request.vigente, usuario, maquina, request.observaciones, HttpContext.RequestAborted);

                if (result.idRol <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(result.message) ? "No fue posible cambiar el estado del rol." : result.message
                    });
                }

                return Ok(new
                {
                    success = true,
                    idRol = result.idRol,
                    message = result.message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando estado de rol {IdRol}", idRol);
                return StatusCode(500, new { success = false, message = "Error al actualizar estado del rol." });
            }
        }

        private (string usuario, string maquina) ObtenerAuditoria()
        {
            var usuario = User.Identity?.Name
                ?? User.FindFirstValue("username")
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("id_usuario")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "ADMIN";

            var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? Environment.MachineName
                ?? "N/A";

            return (usuario, maquina);
        }
    }
}
