using Microsoft.AspNetCore.Mvc;
using Comun.Dtos.Administracion;
using Datos.Interfaz;
using Microsoft.AspNetCore.Authorization;
using Comun.Security;

namespace ofic.Controllers.Administracion
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CuentaEmailController : ControllerBase
    {
        private readonly IDbCuentaEmailRepository _repository;

        public CuentaEmailController(IDbCuentaEmailRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var data = _repository.ConsultarCuentas(0);
                // Descifrar para uso interno si es necesario (aunque al front no deberíamos mandarla)
                foreach(var item in data) {
                    item.ClaveSmtp = CryptoUtility.Decrypt(item.ClaveSmtp);
                }
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
                if (data != null) data.ClaveSmtp = CryptoUtility.Decrypt(data.ClaveSmtp);
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
                request.ClaveSmtp = CryptoUtility.Encrypt(request.ClaveSmtp);
                
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
                request.ClaveSmtp = CryptoUtility.Encrypt(request.ClaveSmtp);
                
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
    }
}
