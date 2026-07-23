using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio.Interfaz;
using Servicios.ApiInterfaz;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UbicacionController : ControllerBase
    {
        private readonly IDbUbicacionService _service;
        private readonly IApiWebToken _tokenService;
        private readonly ILogger<UbicacionController> _logger;

        public UbicacionController(
            IDbUbicacionService service,
            IApiWebToken tokenService,
            ILogger<UbicacionController> logger)
        {
            _service = service;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet("departamentos")]
        public async Task<IActionResult> GetDepartamentos()
        {
            try
            {
                var result = await _service.GetDepartamentosAsync(CancellationToken.None);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando departamentos");
                return StatusCode(500, new { success = false, message = "Error consultando departamentos." });
            }
        }

        [HttpGet("municipios")]
        public async Task<IActionResult> GetMunicipios([FromQuery] long? idDepartamento)
        {
            try
            {
                var result = await _service.GetMunicipiosByDepartamentoAsync(idDepartamento, CancellationToken.None);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando municipios para idDepartamento={IdDepartamento}", idDepartamento);
                return StatusCode(500, new { success = false, message = "Error consultando municipios." });
            }
        }

        [HttpGet("UnidadesOrganigrama")]
        public async Task<IActionResult> GetUnidadesOrganigrama()
        {
            try
            {
                var unidadesPip = await _tokenService.GetUnidadesPipAsync(CancellationToken.None);
                if (unidadesPip != null && unidadesPip.Count > 0)
                {
                    return Ok(unidadesPip);
                }

                var result = await _service.GetUnidadesOrganigramaAsync(CancellationToken.None);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando unidades del organigrama");
                return StatusCode(500, new { success = false, message = "Error consultando unidades del organigrama." });
            }
        }
    }
}
