using Microsoft.Extensions.Configuration;
using Comun.Security;

namespace Comun.Extensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Obtiene la cadena de conexión "DbConnection" descifrando automáticamente
        /// si se encuentra cifrada con AES (prefijo v2:).
        /// </summary>
        public static string GetDecryptedConnectionString(this IConfiguration configuration, string name = "DbConnection")
        {
            var rawCs = configuration.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(rawCs))
            {
                return string.Empty;
            }

            return CryptoUtility.Decrypt(rawCs, configuration);
        }
    }
}
