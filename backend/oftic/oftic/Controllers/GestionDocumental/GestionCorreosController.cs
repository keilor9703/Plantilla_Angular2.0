using Microsoft.AspNetCore.Mvc;
using Comun.Dtos.GestionDocumental;
using Negocio.Gestion.GestionDocumental;
using Microsoft.AspNetCore.Authorization;

namespace ofic.Controllers.GestionDocumental
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GestionCorreosController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public GestionCorreosController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("enviar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Enviar([FromForm] DtoEnvioCorreoRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Datos inválidos.");

                var (usuario, maquina) = ObtenerAuditoria();
                var result = await _emailService.EnviarCorreoAsync(request, usuario, maquina);
                
                if (result.Success)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("historico")]
        public IActionResult GetHistorico()
        {
            var result = _emailService.ConsultarHistorico();
            return Ok(result);
        }

        private (string usuario, string maquina) ObtenerAuditoria()
        {
            // Buscamos el nombre de usuario (string) en orden de prioridad
            var usuario = User.FindFirst("unique_name")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? User.FindFirst("name")?.Value
                ?? User.Identity?.Name
                ?? "SISTEMA";

            var maquina = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? Environment.MachineName
                ?? "N/A";

            return (usuario, maquina);
        }
    }
}
