using System;

namespace Comun.Extensions
{
    public static class FechaUtility
    {
        private static readonly TimeZoneInfo BogotaTimeZone = GetBogotaTimeZone();

        private static TimeZoneInfo GetBogotaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("COT", TimeSpan.FromHours(-5), "Colombia Time (UTC-5)", "Colombia Time");
                }
            }
        }

        /// <summary>
        /// Obtiene la fecha y hora actual garantizada en la zona horaria de Bogotá/Lima/Quito (UTC-5).
        /// </summary>
        public static DateTime HoraBogota
        {
            get
            {
                try
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BogotaTimeZone);
                }
                catch
                {
                    return DateTime.UtcNow.AddHours(-5);
                }
            }
        }
    }
}
