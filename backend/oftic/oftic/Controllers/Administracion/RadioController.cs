using Comun.Dtos;
using Comun.Dtos.Administracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servicios.ApiInterfaz;

namespace ofic.Controllers.Administracion
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class RadioController : ControllerBase
    {
        private readonly IApiWebToken _apiWebToken;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RadioController> _logger;

        public RadioController(
            IApiWebToken apiWebToken,
            IConfiguration configuration,
            ILogger<RadioController> logger)
        {
            _apiWebToken = apiWebToken;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("publicas")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicas()
        {
            try
            {
                var usuario = User.Identity?.Name?.Trim();
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    usuario = User.FindFirst("usuario")?.Value?.Trim()
                        ?? User.FindFirst("name")?.Value?.Trim()
                        ?? "INVITADO";
                }

                var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(ip))
                {
                    ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }

                var request = new DtoConsultaSliderPublicoRequest
                {
                    FUENTE_TRANSACCION = _configuration["ApiSettings:SiglaSistema"] ?? "SISGE",
                    SISTEMA_INFORMACION = _configuration["ApiSettings:NombreSistema"] ?? "Sistema de Gestión Especializada - SISGE",
                    IP_MAQUINA = ip,
                    USUARIO = usuario
                };

                var emisoras = await _apiWebToken.GetEmisorasAsync(request, HttpContext.RequestAborted);
                if (emisoras == null || emisoras.Count == 0)
                {
                    emisoras = GetFallbackEmisoras();
                }
                return Ok(emisoras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando emisoras de radio PIP");
                return Ok(GetFallbackEmisoras());
            }
        }

        private static List<DtoRadioEmisora> GetFallbackEmisoras()
        {
            return new List<DtoRadioEmisora>
            {
                new DtoRadioEmisora
                {
                    IdEmisora = 1,
                    Nombre = "Bogotá",
                    StreamUrl = "https://radio.policia.gov.co:8080/inhouse",
                    LogoUrl = "http://srvdockergusof.policia.gov.co/sicoe/api/Radio/logo/90927871aaaf46eb80ed871274f4e1d9.png",
                    Orden = 1,
                    Activo = 1
                }
            };
        }
    }
}
