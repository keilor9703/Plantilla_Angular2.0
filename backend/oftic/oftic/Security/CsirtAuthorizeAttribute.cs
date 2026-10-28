using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Api.Security
{
    /// <summary>
    /// Restringe el acceso a los endpoints del panel SI2FA a los roles autorizados del grupo CSIRT.
    /// Los roles van en el claim de rol como IDs numéricos (ver JwtService). La lista de roles
    /// permitidos se configura en "Seguridad:RolesCsirt" (por defecto "1,2,3": Desarrollador,
    /// Administrador y CSIRT).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CsirtAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var cfg = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var raw = cfg?["Seguridad:RolesCsirt"];
            var permitidos = (string.IsNullOrWhiteSpace(raw) ? "1,2,3" : raw)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll("role").Select(c => c.Value));

            if (!roles.Any(r => permitidos.Contains(r)))
            {
                context.Result = new ObjectResult(new { success = false, message = "Acceso restringido al grupo CSIRT." })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
