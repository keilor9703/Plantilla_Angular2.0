using Microsoft.AspNetCore.Mvc;
using Comun.Dtos.Administracion;
using Datos.Interfaz;
using Microsoft.AspNetCore.Authorization;
using Comun.Security;
using System.Security.Claims;

namespace ofic.Controllers.Administracion
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CuentaEmailController : ControllerBase
    {
        private readonly IDbCuentaEmailRepository _repository;
        private readonly byte[] _cryptoKey;

        public CuentaEmailController(IDbCuentaEmailRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            var keyStr = configuration["Crypto:Key"] ?? string.Empty;
            if (keyStr.Length < 32)
                throw new InvalidOperationException("Crypto:Key debe tener al menos 32 caracteres.");
            _cryptoKey = System.Text.Encoding.UTF8.GetBytes(keyStr[..32]);
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var data = _repository.ConsultarCuentas(0);
                // Descifrar para uso interno si es necesario (aunque al front no deberíamos mandarla)
                foreach(var item in data) {
                    item.ClaveSmtp = CryptoUtility.Decrypt(item.ClaveSmtp, _cryptoKey);
                }
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("mis-cuentas")]
        public IActionResult GetMisCuentas()
        {
            try
            {
                var rawId = User.FindFirstValue("id_usuario")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("nameid");
                if (!int.TryParse(rawId, out var idUsuario) || idUsuario <= 0)
                    return Unauthorized(new { success = false, message = "No fue posible identificar el usuario autenticado." });

                var data = _repository.ConsultarCuentasPorUsuario(idUsuario);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var data = _repository.ConsultarCuentas(id).FirstOrDefault();
                if (data != null) data.ClaveSmtp = CryptoUtility.Decrypt(data.ClaveSmtp, _cryptoKey);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Post([FromBody] DtoCuentaEmailRequest request)
        {
            try
            {
                request.IdCuenta = 0; 
                request.UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA";
                request.IpAuditoria = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Cifrar la contraseña antes de guardar
                request.ClaveSmtp = CryptoUtility.Encrypt(request.ClaveSmtp, _cryptoKey);
                
                var success = _repository.GestionarCuenta(request);
                return Ok(new { success, message = "Cuenta creada correctamente y protegida" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DtoCuentaEmailRequest request)
        {
            try
            {
                request.IdCuenta = id;
                request.UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA";
                request.IpAuditoria = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Cifrar la contraseña antes de actualizar
                request.ClaveSmtp = CryptoUtility.Encrypt(request.ClaveSmtp, _cryptoKey);
                
                var success = _repository.GestionarCuenta(request);
                return Ok(new { success, message = "Cuenta actualizada y protegida" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var success = _repository.EliminarCuenta(id);
                return Ok(new { success, message = "Cuenta eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("autorizaciones")]
        public IActionResult GetAutorizaciones([FromQuery] int? idCuenta, [FromQuery] int? idUsuario, [FromQuery] int? vigente)
        {
            try
            {
                var data = _repository.ConsultarAutorizaciones(idCuenta, idUsuario, vigente);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("autorizaciones")]
        public IActionResult GuardarAutorizacion([FromBody] DtoCuentaEmailUsuarioRequest request)
        {
            try
            {
                request.UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA";
                request.IpAuditoria = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var success = _repository.GuardarAutorizacion(request);
                return Ok(new { success, message = success ? "Autorización guardada correctamente." : "No fue posible guardar la autorización." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("autorizaciones/{idCuentaUsuario}")]
        public IActionResult ActualizarAutorizacion(int idCuentaUsuario, [FromBody] DtoCuentaEmailUsuarioEstadoRequest request)
        {
            try
            {
                request.IdCuentaUsuario = idCuentaUsuario;
                request.UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA";
                request.IpAuditoria = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var success = _repository.ActualizarAutorizacion(request);
                return Ok(new { success, message = success ? "Autorización actualizada correctamente." : "No fue posible actualizar la autorización." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("autorizaciones/{idCuentaUsuario}")]
        public IActionResult EliminarAutorizacion(int idCuentaUsuario)
        {
            try
            {
                var request = new DtoCuentaEmailUsuarioEstadoRequest
                {
                    IdCuentaUsuario = idCuentaUsuario,
                    UsuarioAuditoria = User.Identity?.Name ?? "SISTEMA",
                    IpAuditoria = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                };
                var success = _repository.EliminarAutorizacion(request);
                return Ok(new { success, message = success ? "Autorización inactivada correctamente." : "No fue posible inactivar la autorización." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
