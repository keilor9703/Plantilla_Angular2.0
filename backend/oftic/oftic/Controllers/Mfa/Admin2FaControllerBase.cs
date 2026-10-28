using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Security;

namespace Api.Controllers.Mfa
{
    /// <summary>
    /// Base común para los controladores del panel administrativo SI2FA.
    /// Exige autenticación JWT y pertenencia al grupo CSIRT.
    /// </summary>
    [ApiController]
    [Authorize]
    [CsirtAuthorize]
    public abstract class Admin2FaControllerBase : ControllerBase
    {
        /// <summary>Identifica al funcionario que ejecuta la acción (para auditoría).</summary>
        protected string ObtenerActor()
        {
            var usuario = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("unique_name")
                ?? User.FindFirstValue("id_usuario")
                ?? "SI2FA";
            return usuario;
        }

        /// <summary>IP de origen de la petición (para auditoría).</summary>
        protected string ObtenerIp()
        {
            var fwd = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(fwd))
                return fwd.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A";
        }
    }
}
