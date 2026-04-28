using Comun.Dtos.Administracion;
using Datos.Interfaz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace Datos.Gestion
{
    public class DbBrandingRepository : IDbBrandingRepository
    {
        private readonly string _cs;
        private readonly ILogger<DbBrandingRepository> _logger;
        private readonly string _rootPath;

        public DbBrandingRepository(IConfiguration configuration, ILogger<DbBrandingRepository> logger)
        {
            _cs = configuration.GetConnectionString("DbCoest")!;
            _logger = logger;
            _rootPath = ResolveStoragePath();
        }

        public async Task<DtoBrandingConfig> ReadConfigAsync(CancellationToken ct)
        {
            var items = new List<DtoBrandingConfig>();

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PK_ADMINISTRACION.P_GET_ALL_BRANDING";
            cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(MapBranding(reader));
            }

            if (items.Count == 0)
            {
                return new DtoBrandingConfig();
            }

            var selected = items
                .OrderByDescending(x => x.idBranding ?? 0)
                .First();

            return Normalize(selected);
        }

        public async Task WriteConfigAsync(DtoBrandingConfig config, string usuarioAuditoria, string maquinaAuditoria, CancellationToken ct)
        {
            var safe = Normalize(config);
            var existing = await ReadConfigAsync(ct);
            var id = safe.idBranding ?? existing.idBranding ?? 0;

            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;

            if (id > 0)
            {
                cmd.CommandText = "PK_ADMINISTRACION.P_UPDATE_BRANDING";
                cmd.Parameters.Add("p_id_branding", OracleDbType.Int64).Value = id;
                cmd.Parameters.Add("p_sistema", OracleDbType.Varchar2).Value = safe.sistema;
                cmd.Parameters.Add("p_nombre_sistema", OracleDbType.Varchar2).Value = safe.nombreSistema;
                cmd.Parameters.Add("p_unidad_sistema_id", OracleDbType.Int32).Value = ToDb(safe.unidadSistemaId);
                cmd.Parameters.Add("p_logo_file_name", OracleDbType.Varchar2).Value = ToDb(safe.logoFileName);
                cmd.Parameters.Add("p_favicon_file_name", OracleDbType.Varchar2).Value = ToDb(safe.faviconFileName);
                cmd.Parameters.Add("p_fondo_1_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo1FileName);
                cmd.Parameters.Add("p_fondo_2_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo2FileName);
                cmd.Parameters.Add("p_fondo_3_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo3FileName);
                cmd.Parameters.Add("p_vigente", OracleDbType.Int32).Value = 1;
                cmd.Parameters.Add("p_usuario_modificacion", OracleDbType.Varchar2).Value = SafeAudit(usuarioAuditoria, 50);
                cmd.Parameters.Add("p_maquina_modificacion", OracleDbType.Varchar2).Value = SafeAudit(maquinaAuditoria, 100);

                var pSuccess = cmd.Parameters.Add("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                var pMessage = cmd.Parameters.Add("p_message", OracleDbType.Varchar2, 4000);
                pMessage.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync(ct);
                var ok = GetOracleInt(pSuccess.Value) == 1;
                if (!ok)
                {
                    throw new InvalidOperationException(pMessage.Value?.ToString() ?? "No fue posible actualizar branding.");
                }
            }
            else
            {
                cmd.CommandText = "PK_ADMINISTRACION.P_CREATE_BRANDING";
                cmd.Parameters.Add("p_sistema", OracleDbType.Varchar2).Value = safe.sistema;
                cmd.Parameters.Add("p_nombre_sistema", OracleDbType.Varchar2).Value = safe.nombreSistema;
                cmd.Parameters.Add("p_unidad_sistema_id", OracleDbType.Int32).Value = ToDb(safe.unidadSistemaId);
                cmd.Parameters.Add("p_logo_file_name", OracleDbType.Varchar2).Value = ToDb(safe.logoFileName);
                cmd.Parameters.Add("p_favicon_file_name", OracleDbType.Varchar2).Value = ToDb(safe.faviconFileName);
                cmd.Parameters.Add("p_fondo_1_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo1FileName);
                cmd.Parameters.Add("p_fondo_2_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo2FileName);
                cmd.Parameters.Add("p_fondo_3_file_name", OracleDbType.Varchar2).Value = ToDb(safe.fondo3FileName);
                cmd.Parameters.Add("p_vigente", OracleDbType.Int32).Value = 1;
                cmd.Parameters.Add("p_usuario_creacion", OracleDbType.Varchar2).Value = SafeAudit(usuarioAuditoria, 50);
                cmd.Parameters.Add("p_maquina_creacion", OracleDbType.Varchar2).Value = SafeAudit(maquinaAuditoria, 100);

                var pId = cmd.Parameters.Add("p_id_branding", OracleDbType.Int64);
                pId.Direction = ParameterDirection.Output;
                var pSuccess = cmd.Parameters.Add("p_success", OracleDbType.Int32);
                pSuccess.Direction = ParameterDirection.Output;
                var pMessage = cmd.Parameters.Add("p_message", OracleDbType.Varchar2, 4000);
                pMessage.Direction = ParameterDirection.Output;

                await cmd.ExecuteNonQueryAsync(ct);
                var ok = GetOracleInt(pSuccess.Value) == 1;
                if (!ok)
                {
                    throw new InvalidOperationException(pMessage.Value?.ToString() ?? "No fue posible crear branding.");
                }
            }
        }

        public async Task<string> SaveAssetAsync(string assetPrefix, string extension, byte[] bytes, CancellationToken ct)
        {
            var folder = GetAssetFolder(assetPrefix);
            Directory.CreateDirectory(folder);

            var safePrefix = assetPrefix.ToLowerInvariant() switch
            {
                "logo" => "logo",
                "favicon" => "favicon",
                _ => "branding"
            };

            var fileName = $"{safePrefix}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);
            return fileName;
        }

        public Task<bool> AssetExistsAsync(string fileName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.FromResult(false);
            }

            var safe = Path.GetFileName(fileName);
            var candidates = new[]
            {
                Path.Combine(GetAssetFolder("logo"), safe),
                Path.Combine(GetAssetFolder("favicon"), safe),
                Path.Combine(_rootPath, safe)
            };

            return Task.FromResult(candidates.Any(File.Exists));
        }

        public Task<string?> GetAssetPathAsync(string assetPrefix, string fileName, CancellationToken ct)
        {
            var safe = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safe))
            {
                return Task.FromResult<string?>(null);
            }

            var folder = GetAssetFolder(assetPrefix);
            var fullPath = Path.Combine(folder, safe);
            if (File.Exists(fullPath))
            {
                return Task.FromResult<string?>(fullPath);
            }

            var fallback = Path.Combine(_rootPath, safe);
            return Task.FromResult(File.Exists(fallback) ? fallback : null);
        }

        public Task DeleteAssetIfExistsAsync(string assetPrefix, string? fileName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            var safe = Path.GetFileName(fileName);
            var folderPath = Path.Combine(GetAssetFolder(assetPrefix), safe);
            if (File.Exists(folderPath))
            {
                File.Delete(folderPath);
            }

            var legacyPath = Path.Combine(_rootPath, safe);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }

            return Task.CompletedTask;
        }

        private static DtoBrandingConfig MapBranding(OracleDataReader reader)
        {
            return new DtoBrandingConfig
            {
                idBranding = GetInt64(reader, "ID_BRANDING"),
                sistema = GetString(reader, "SISTEMA") ?? "SISGE",
                nombreSistema = GetString(reader, "NOMBRE_SISTEMA") ?? "SISGE",
                unidadSistemaId = GetInt32Nullable(reader, "UNIDAD_SISTEMA_ID"),
                logoFileName = GetString(reader, "LOGO_FILE_NAME"),
                faviconFileName = GetString(reader, "FAVICON_FILE_NAME"),
                fondo1FileName = GetString(reader, "FONDO_1_FILE_NAME"),
                fondo2FileName = GetString(reader, "FONDO_2_FILE_NAME"),
                fondo3FileName = GetString(reader, "FONDO_3_FILE_NAME")
            };
        }

        private static DtoBrandingConfig Normalize(DtoBrandingConfig cfg)
        {
            var normalized = new DtoBrandingConfig
            {
                idBranding = cfg.idBranding,
                sistema = string.IsNullOrWhiteSpace(cfg.sistema) ? "SISGE" : cfg.sistema.Trim(),
                nombreSistema = string.IsNullOrWhiteSpace(cfg.nombreSistema) ? "SISGE" : cfg.nombreSistema.Trim(),
                unidadSistemaId = cfg.unidadSistemaId.HasValue && cfg.unidadSistemaId.Value > 0 ? cfg.unidadSistemaId : null,
                logoFileName = NormalizeFileName(cfg.logoFileName),
                faviconFileName = NormalizeFileName(cfg.faviconFileName),
                fondo1FileName = NormalizeFileName(cfg.fondo1FileName),
                fondo2FileName = NormalizeFileName(cfg.fondo2FileName),
                fondo3FileName = NormalizeFileName(cfg.fondo3FileName)
            };

            if (normalized.sistema.Length > 10)
            {
                normalized.sistema = normalized.sistema[..10];
            }

            if (normalized.nombreSistema.Length > 90)
            {
                normalized.nombreSistema = normalized.nombreSistema[..90];
            }

            return normalized;
        }

        private static string? NormalizeFileName(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : Path.GetFileName(value.Trim());
        }

        private string GetAssetFolder(string assetPrefix)
        {
            return assetPrefix.ToLowerInvariant() switch
            {
                "logo" => Path.Combine(_rootPath, "logo"),
                "favicon" => Path.Combine(_rootPath, "favicon"),
                "fondo1" => Path.Combine(_rootPath, "fondo"),
                "fondo2" => Path.Combine(_rootPath, "fondo"),
                "fondo3" => Path.Combine(_rootPath, "fondo"),
                _ => _rootPath
            };
        }

        private static object ToDb(object? value) => value ?? DBNull.Value;

        private static int GetOracleInt(object? value)
        {
            if (value is OracleDecimal od)
            {
                return od.IsNull ? 0 : (int)od;
            }

            if (value is null || value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(value);
        }

        private static string SafeAudit(string? value, int maxLen)
        {
            var safe = string.IsNullOrWhiteSpace(value) ? "sistema" : value.Trim();
            return safe.Length > maxLen ? safe[..maxLen] : safe;
        }

        private static long? GetInt64(OracleDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetInt64(i);
        }

        private static int? GetInt32Nullable(OracleDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetInt32(i);
        }

        private static string? GetString(OracleDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }

        private static string ResolveStoragePath()
        {
            var configured = Environment.GetEnvironmentVariable("SISGE_BRANDING_PATH");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "uploads", "branding"));
        }
    }
}
