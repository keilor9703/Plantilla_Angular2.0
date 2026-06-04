using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Negocio.Interfaz
{
    public interface IJwtService
    {
        string CreateToken(long idUsuario, string usuario, List<long> roles);
        string CreateImpersonationToken(
            long idUsuario,
            string usuario,
            List<long> roles,
            long actorId,
            string actorUsuario);
        string GenerateToken(string usuario);
        string? RefreshToken(string token);
        System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
    }
}
