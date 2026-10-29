using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Services
{
    /// <summary>
    /// Genera y valida tokens de sesión MFA de corta vida.
    ///
    /// Tras validar credenciales (OUD/local) pero ANTES de emitir el JWT real, se emite un
    /// MfaSessionToken con los datos necesarios para construir el JWT final una vez completado
    /// el segundo factor. Evita que se llame a /MfaVerify o /MfaEnrollConfirm sin haber pasado
    /// por la validación de credenciales.
    ///
    /// Vida útil: 5 min (Jwt:MfaSessionMinutes). Firma: Jwt:MfaSessionKey (fallback a Jwt:Key).
    /// </summary>
    public class MfaSessionTokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly int _minutes;
        private const string Issuer = "si2fa.mfa.session";
        private const string Audience = "si2fa.mfa.session";

        public MfaSessionTokenService(IConfiguration cfg)
        {
            var rawKey = cfg["Jwt:MfaSessionKey"]
                      ?? cfg["Jwt:Key"]
                      ?? "CAMBIAR_EN_PRODUCCION_ClaveSeguraMinimo32Caracteres_2026";

            if (rawKey.Length < 32) rawKey = rawKey.PadRight(32, '#');

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            _minutes = int.TryParse(cfg["Jwt:MfaSessionMinutes"], out var m) && m > 0 ? m : 5;
        }

        public record MfaSessionData(
            long IdUsuario,
            string Usuario,
            long Identificacion,   // cédula (OUD) o idUsuario (fallback)
            string RolesJson,      // JSON array de longs
            string Ip
        );

        public string CreateToken(MfaSessionData d)
        {
            var claims = new[]
            {
                new Claim("id_usuario",     d.IdUsuario.ToString()),
                new Claim("usuario",        d.Usuario),
                new Claim("identificacion", d.Identificacion.ToString()),
                new Claim("roles_json",     d.RolesJson),
                new Claim("ip",             d.Ip)
            };

            var jwt = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_minutes),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public MfaSessionData? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = _key,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);

                string G(string n) => principal.FindFirstValue(n) ?? "";
                long L(string n) => long.TryParse(G(n), out var v) ? v : 0;

                return new MfaSessionData(
                    IdUsuario: L("id_usuario"),
                    Usuario: G("usuario"),
                    Identificacion: L("identificacion"),
                    RolesJson: G("roles_json"),
                    Ip: G("ip")
                );
            }
            catch
            {
                return null;
            }
        }
    }
}
