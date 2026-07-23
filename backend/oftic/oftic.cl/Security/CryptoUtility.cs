using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Comun.Security
{
    public static class CryptoUtility
    {
        // Clave legacy usada antes de migrar a config — solo para descifrar datos viejos en BD.
        private static readonly byte[] LegacyKey = Encoding.UTF8.GetBytes("SICOE_COEST_2024_SECURE_KEY_32CH");
        private static readonly byte[] LegacyIv  = Encoding.UTF8.GetBytes("SICOE_IV_VECTOR_");

        // Clave por defecto de 32 caracteres (256-bit)
        private const string DefaultSecretKey = "OFTIC_SISGE_DB_SECRET_KEY_2026!";
        private const string V2Prefix = "v2:";

        public static byte[] GetEncryptionKey(IConfiguration? cfg = null)
        {
            var keyStr = Environment.GetEnvironmentVariable("Crypto__Key") 
                ?? cfg?["Crypto:Key"] 
                ?? DefaultSecretKey;

            if (string.IsNullOrWhiteSpace(keyStr))
                keyStr = DefaultSecretKey;

            // Asegurar exactamente 32 bytes (256 bits) para AES-256
            if (keyStr.Length < 32)
                keyStr = keyStr.PadRight(32, '0');
            else if (keyStr.Length > 32)
                keyStr = keyStr[..32];

            return Encoding.UTF8.GetBytes(keyStr);
        }

        public static string Encrypt(string plainText, IConfiguration? cfg = null)
        {
            return Encrypt(plainText, GetEncryptionKey(cfg));
        }

        public static string Decrypt(string cipherText, IConfiguration? cfg = null)
        {
            return Decrypt(cipherText, GetEncryptionKey(cfg));
        }

        /// <summary>
        /// Cifra con IV aleatorio y clave de configuración.
        /// Formato de salida: "v2:" + Base64(IV_16bytes + ciphertext)
        /// </summary>
        public static string Encrypt(string plainText, byte[] key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
                sw.Write(plainText);

            return V2Prefix + Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Descifra automáticamente formato v2 (IV aleatorio + clave config)
        /// o formato legacy (IV fijo + clave hardcodeada) para datos existentes en BD.
        /// </summary>
        public static string Decrypt(string cipherText, byte[] key)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            return cipherText.StartsWith(V2Prefix, StringComparison.Ordinal)
                ? DecryptV2(cipherText[V2Prefix.Length..], key)
                : DecryptLegacy(cipherText);
        }

        private static string DecryptV2(string base64, byte[] key)
        {
            try
            {
                var data = Convert.FromBase64String(base64);
                var iv   = data[..16];
                var body = data[16..];

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV  = iv;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(body);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch { return base64; }
        }

        private static string DecryptLegacy(string cipherText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = LegacyKey;
                aes.IV  = LegacyIv;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch { return cipherText; }
        }
    }
}
