using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Negocio.Interfaz;

namespace Negocio.Gestion
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _cfg;
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtService(IConfiguration cfg)
        {
            _cfg = cfg;

            _key = Environment.GetEnvironmentVariable("Jwt__Key") ?? cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(_key))
            {
                // Fallback temporal para permitir inicio en modo desarrollo / setup
                _key = "CAMBIAR_EN_PRODUCCION_ClaveSeguraMinimo32Caracteres_2026";
            }

            if (_key.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key debe tener al menos 32 caracteres.");

            _issuer   = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? cfg["Jwt:Issuer"] ?? "oftic.api";
            _audience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? cfg["Jwt:Audience"] ?? _issuer;
        }

        public string CreateToken(long idUsuario, string usuario, List<long> roles)
        {
            var minutes = int.Parse(_cfg["Jwt:Minutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim("id_usuario", idUsuario.ToString()),
                new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario ?? "")
            };

            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r.ToString()));

            return BuildToken(claims, TimeSpan.FromMinutes(minutes));
        }

        public string CreateImpersonationToken(
            long idUsuario,
            string usuario,
            List<long> roles,
            long actorId,
            string actorUsuario)
        {
            var minutes = int.Parse(_cfg["Jwt:ImpersonationMinutes"] ?? "15");

            var claims = new List<Claim>
            {
                new Claim("id_usuario", idUsuario.ToString()),
                new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario ?? ""),
                new Claim("impersonating", "1"),
                new Claim("impersonated_by_id", actorId.ToString()),
                new Claim("impersonated_by_user", actorUsuario ?? string.Empty)
            };

            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r.ToString()));

            return BuildToken(claims, TimeSpan.FromMinutes(minutes));
        }

        public string? RefreshToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            // Rechazar refresh si el token fue emitido hace más de MaxRefreshHours.
            // Previene que tokens robados/viejos se renueven indefinidamente.
            var maxRefreshHours = int.Parse(_cfg["Jwt:MaxRefreshHours"] ?? "24");
            var iatRaw = principal.FindFirstValue(JwtRegisteredClaimNames.Iat);
            if (long.TryParse(iatRaw, out var iatUnix))
            {
                var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
                if (DateTime.UtcNow - issuedAt > TimeSpan.FromHours(maxRefreshHours))
                    return null;
            }

            var idUsuario = long.Parse(principal.FindFirstValue("id_usuario") ?? "0");
            var usuario = principal.FindFirstValue(ClaimTypes.Name) ?? "";
            var roles = principal.FindAll(ClaimTypes.Role)
                .Select(x => long.TryParse(x.Value, out var r) ? r : 0)
                .Where(r => r > 0)
                .ToList();

            if (principal.FindFirstValue("impersonating") == "1")
            {
                var actorId = long.Parse(principal.FindFirstValue("impersonated_by_id") ?? "0");
                var actorUser = principal.FindFirstValue("impersonated_by_user") ?? "";
                return CreateImpersonationToken(idUsuario, usuario, roles, actorId, actorUser);
            }

            return CreateToken(idUsuario, usuario, roles);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key))
                };
                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        private string BuildToken(List<Claim> claims, TimeSpan lifetime)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
