using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Datos.Gestion
{
    public class DtoMigrationResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
    }

    public static class DatabaseMigration
    {
        public static async Task<DtoMigrationResult> RunAsync(IConfiguration cfg, ILogger logger, CancellationToken ct = default)
        {
            var result = new DtoMigrationResult();
            var connectionString = Comun.Extensions.ConfigurationExtensions.GetDecryptedConnectionString(cfg);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.Success = false;
                result.Message = "No se pudo obtener la cadena de conexión para ejecutar la migración de base de datos.";
                logger.LogWarning(result.Message);
                return result;
            }

            var provider = (cfg["DatabaseProvider"] ?? "Oracle").Trim();

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    return await MigrateMySqlAsync(connectionString, logger, ct);
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    return await MigratePostgreSqlAsync(connectionString, logger, ct);
                }
                else
                {
                    return await MigrateOracleAsync(connectionString, logger, ct, cfg);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ejecutando la migración automática de base de datos ({Provider})", provider);
                result.Success = false;
                result.Message = $"Error ejecutando la migración ({provider}): {ex.Message}";
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        private static async Task<DtoMigrationResult> MigrateMySqlAsync(string cs, ILogger logger, CancellationToken ct)
        {
            var result = new DtoMigrationResult();
            await using var conn = new MySqlConnector.MySqlConnection(cs);
            await conn.OpenAsync(ct);

            string[] ddlStatements = new[]
            {
                // 1. CTR_USUARIOS
                @"CREATE TABLE IF NOT EXISTS CTR_USUARIOS (
                    id_usuario BIGINT AUTO_INCREMENT PRIMARY KEY,
                    bloqueado TINYINT(1) DEFAULT 0 NOT NULL,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    identificacion VARCHAR(50) NOT NULL UNIQUE,
                    grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                    apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                    nombres VARCHAR(255) DEFAULT '' NOT NULL,
                    funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                    unde_laborando BIGINT DEFAULT 0 NOT NULL,
                    codigocargo BIGINT DEFAULT 0 NOT NULL,
                    usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                    usuario_modifica BIGINT,
                    fecha_modifica DATETIME,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE CTR_USUARIOS
                    ADD COLUMN IF NOT EXISTS grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS nombres VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS unde_laborando BIGINT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS codigocargo BIGINT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_modifica BIGINT,
                    ADD COLUMN IF NOT EXISTS fecha_modifica DATETIME,
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(255);",

                // 2. CTR_ROLES
                @"CREATE TABLE IF NOT EXISTS CTR_ROLES (
                    id_rol INT AUTO_INCREMENT PRIMARY KEY,
                    codigo_rol VARCHAR(50) DEFAULT '' NOT NULL,
                    nombre_rol VARCHAR(255) DEFAULT '' NOT NULL,
                    descripcion VARCHAR(500),
                    estado INT DEFAULT 1 NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    maquina_creacion VARCHAR(100) DEFAULT 'LOCAL',
                    fecha_modifica DATETIME,
                    usuario_modifica VARCHAR(100),
                    maquina_modifica VARCHAR(100)
                );",
                @"ALTER TABLE CTR_ROLES
                    ADD COLUMN IF NOT EXISTS codigo_rol VARCHAR(50) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS nombre_rol VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS descripcion VARCHAR(500),
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(100) DEFAULT 'LOCAL',
                    ADD COLUMN IF NOT EXISTS fecha_modifica DATETIME,
                    ADD COLUMN IF NOT EXISTS usuario_modifica VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(100);",

                // 3. CTR_MENU
                @"CREATE TABLE IF NOT EXISTS CTR_MENU (
                    id_menu INT AUTO_INCREMENT PRIMARY KEY,
                    id_padre INT DEFAULT 0 NOT NULL,
                    titulo VARCHAR(255) NOT NULL,
                    ruta VARCHAR(255),
                    icono VARCHAR(100),
                    orden INT DEFAULT 1 NOT NULL,
                    estado INT DEFAULT 1 NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    maquina_creacion VARCHAR(255),
                    usuario_modifica VARCHAR(100),
                    fecha_modifica DATETIME,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE CTR_MENU
                    ADD COLUMN IF NOT EXISTS id_padre INT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS titulo VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS ruta VARCHAR(255),
                    ADD COLUMN IF NOT EXISTS icono VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS orden INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(255),
                    ADD COLUMN IF NOT EXISTS usuario_modifica VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS fecha_modifica DATETIME,
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(255);",

                // 4. CTR_MENU_ROLES
                @"CREATE TABLE IF NOT EXISTS CTR_MENU_ROLES (
                    id_menu_rol INT AUTO_INCREMENT PRIMARY KEY,
                    id_menu INT NOT NULL,
                    id_rol INT NOT NULL,
                    estado INT DEFAULT 1 NOT NULL
                );",

                // 5. CTR_ROLES_USER
                @"CREATE TABLE IF NOT EXISTS CTR_ROLES_USER (
                    id_user_rol BIGINT AUTO_INCREMENT PRIMARY KEY,
                    id_usuario BIGINT NOT NULL,
                    id_rol INT NOT NULL,
                    justificacion VARCHAR(500),
                    fecha_fin DATE,
                    estado INT DEFAULT 1 NOT NULL,
                    usuario_creacion BIGINT NOT NULL,
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    maquina_creacion VARCHAR(255) NOT NULL
                );",

                // 6. CTR_ROLES_USER_ADMIN
                @"CREATE TABLE IF NOT EXISTS CTR_ROLES_USER_ADMIN (
                    id_usuario BIGINT NOT NULL,
                    id_rol INT NOT NULL,
                    justificacion VARCHAR(1000),
                    fecha_fin DATE,
                    vigente INT DEFAULT 1,
                    PRIMARY KEY (id_usuario, id_rol)
                );",
                @"ALTER TABLE CTR_ROLES_USER_ADMIN
                    ADD COLUMN IF NOT EXISTS justificacion VARCHAR(1000),
                    ADD COLUMN IF NOT EXISTS fecha_fin DATE,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1;",

                // 7. CTR_DOMINIO
                @"CREATE TABLE IF NOT EXISTS CTR_DOMINIO (
                    id_dominio INT AUTO_INCREMENT PRIMARY KEY,
                    descripcion VARCHAR(500) NOT NULL,
                    id_padre INT DEFAULT 0 NOT NULL,
                    abreviatura VARCHAR(100),
                    observacion VARCHAR(500),
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP',
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
                    maquina_creacion VARCHAR(255) DEFAULT 'LOCAL',
                    usuario_modifica VARCHAR(100),
                    fecha_modifica DATETIME,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE CTR_DOMINIO
                    ADD COLUMN IF NOT EXISTS id_padre INT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS abreviatura VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS observacion VARCHAR(500),
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL;",
                @"INSERT IGNORE INTO CTR_DOMINIO (id_dominio, descripcion, id_padre, abreviatura, observacion, vigente) VALUES (1, 'DOMINIO PRINCIPAL / RAÍZ', 0, 'RAIZ', 'Dominio Semilla Raíz Principal', 1);",

                // 8. CTR_AUDITORIA
                @"CREATE TABLE IF NOT EXISTS CTR_AUDITORIA (
                    id_auditoria BIGINT AUTO_INCREMENT PRIMARY KEY,
                    fecha DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario VARCHAR(100) NOT NULL,
                    modulo VARCHAR(100) NOT NULL,
                    accion VARCHAR(50) NOT NULL,
                    registro TEXT,
                    resultado VARCHAR(50),
                    maquina VARCHAR(100),
                    observaciones TEXT
                );",
                @"ALTER TABLE CTR_AUDITORIA
                    ADD COLUMN IF NOT EXISTS fecha DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS modulo VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS accion VARCHAR(50),
                    ADD COLUMN IF NOT EXISTS registro TEXT,
                    ADD COLUMN IF NOT EXISTS resultado VARCHAR(50),
                    ADD COLUMN IF NOT EXISTS maquina VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS observaciones TEXT;",

                // 9. CTR_ERRORES_SYSTEM
                @"CREATE TABLE IF NOT EXISTS CTR_ERRORES_SYSTEM (
                    id_error BIGINT AUTO_INCREMENT PRIMARY KEY,
                    procedimiento VARCHAR(255) NOT NULL,
                    descripcion TEXT,
                    id_documento VARCHAR(255),
                    usuario BIGINT,
                    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
                );",

                // 10. CTR_BRANDING
                @"CREATE TABLE IF NOT EXISTS CTR_BRANDING (
                    id_branding BIGINT AUTO_INCREMENT PRIMARY KEY,
                    sistema VARCHAR(50) NOT NULL,
                    nombre_sistema VARCHAR(100) NOT NULL,
                    unidad_sistema_id INT,
                    logo_file_name VARCHAR(255),
                    favicon_file_name VARCHAR(255),
                    fondo_1_file_name VARCHAR(255),
                    fondo_2_file_name VARCHAR(255),
                    fondo_3_file_name VARCHAR(255),
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_creacion VARCHAR(50) DEFAULT 'SISTEMA' NOT NULL,
                    maquina_creacion VARCHAR(100) DEFAULT 'LOCAL' NOT NULL,
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_modificacion VARCHAR(50),
                    maquina_modificacion VARCHAR(100),
                    fecha_modificacion DATETIME,
                    sigla VARCHAR(100),
                    nombre_unidad VARCHAR(255)
                );",

                // 11. LOGIN_VISUAL
                @"CREATE TABLE IF NOT EXISTS LOGIN_VISUAL (
                    id_login_visual INT AUTO_INCREMENT PRIMARY KEY,
                    descripcion VARCHAR(255),
                    imagen_fondo VARCHAR(500),
                    orden INT DEFAULT 1,
                    vigente INT DEFAULT 1 NOT NULL
                );",

                // 12. CTR_LINEA_MANDO
                @"CREATE TABLE IF NOT EXISTS CTR_LINEA_MANDO (
                    id_linea_mando INT AUTO_INCREMENT PRIMARY KEY,
                    identificacion VARCHAR(20) NOT NULL,
                    nombre VARCHAR(100),
                    apellidos VARCHAR(100),
                    grado VARCHAR(50),
                    cargo VARCHAR(100),
                    unidad VARCHAR(200),
                    foto_base64 LONGTEXT,
                    orden INT DEFAULT 1,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod DATETIME,
                    maquina_mod VARCHAR(100),
                    peso VARCHAR(100)
                );",

                // 13. CTR_PERSONAJE_GRUPO
                @"CREATE TABLE IF NOT EXISTS CTR_PERSONAJE_GRUPO (
                    id_personaje_grupo INT AUTO_INCREMENT PRIMARY KEY,
                    nombre_grupo VARCHAR(100),
                    numero_acta VARCHAR(50),
                    mes INT,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod DATETIME,
                    maquina_mod VARCHAR(50),
                    foto_grupo VARCHAR(200),
                    anio INT
                );",

                // 14. CTR_PERSONAJE_MES
                @"CREATE TABLE IF NOT EXISTS CTR_PERSONAJE_MES (
                    id_personaje_mes INT AUTO_INCREMENT PRIMARY KEY,
                    identificacion VARCHAR(20),
                    nombres VARCHAR(100),
                    apellidos VARCHAR(100),
                    grado VARCHAR(50),
                    cargo VARCHAR(100),
                    unidad VARCHAR(200),
                    id_categoria INT,
                    numero_acta VARCHAR(50),
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod DATETIME,
                    maquina_mod VARCHAR(100),
                    foto_modificada VARCHAR(200),
                    mes INT,
                    anio INT,
                    id_personaje_grupo INT
                );",

                // 15. CTR_VIDEOS
                @"CREATE TABLE IF NOT EXISTS CTR_VIDEOS (
                    id_video INT AUTO_INCREMENT PRIMARY KEY,
                    descripcion VARCHAR(255) NOT NULL,
                    observaciones VARCHAR(500),
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
                    usuario_creacion VARCHAR(50) NOT NULL,
                    ruta_video VARCHAR(500),
                    vigente INT DEFAULT 1,
                    fecha_modifica DATETIME,
                    usuario_modifica VARCHAR(50),
                    maquina_modifica VARCHAR(100)
                );",

                // 16. CTR_CUENTA_EMAIL
                @"CREATE TABLE IF NOT EXISTS CTR_CUENTA_EMAIL (
                    id_cuenta INT AUTO_INCREMENT PRIMARY KEY,
                    id_dominio_grupo INT NOT NULL,
                    nombre_cuenta VARCHAR(200) NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    servidor_smtp VARCHAR(255) DEFAULT 'smtp.office365.com',
                    puerto INT DEFAULT 587,
                    usuario_smtp VARCHAR(255),
                    clave_smtp VARCHAR(1000),
                    usar_ssl INT DEFAULT 1,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(100),
                    fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    fecha_modifica DATETIME,
                    ip_registro VARCHAR(50),
                    firma_html LONGTEXT,
                    imagen_encabezado VARCHAR(500)
                );",

                // 17. CTR_CUENTA_EMAIL_USUARIO
                @"CREATE TABLE IF NOT EXISTS CTR_CUENTA_EMAIL_USUARIO (
                    id_cuenta_usuario INT AUTO_INCREMENT PRIMARY KEY,
                    id_cuenta INT NOT NULL,
                    id_usuario BIGINT NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_crea VARCHAR(100),
                    fecha_crea DATETIME DEFAULT CURRENT_TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    fecha_modifica DATETIME,
                    ip_registro VARCHAR(45)
                );",

                // Seed Data - Roles
                @"INSERT IGNORE INTO CTR_ROLES (id_rol, codigo_rol, nombre_rol, descripcion, estado, usuario_creacion, maquina_creacion)
                  VALUES (1, 'DESARROLLADOR', 'Desarrollador', 'Rol con acceso total de desarrollo y plantilla', 1, 'SETUP', 'LOCAL');",

                @"INSERT IGNORE INTO CTR_ROLES (id_rol, codigo_rol, nombre_rol, descripcion, estado, usuario_creacion, maquina_creacion)
                  VALUES (2, 'ADMINISTRADOR', 'Administrador', 'Rol administrador del sistema', 1, 'SETUP', 'LOCAL');",

                // Seed Data - Menús Base
                @"DELETE FROM CTR_MENU_ROLES WHERE id_menu = 2;",
                @"DELETE FROM CTR_MENU WHERE id_menu = 2 OR ruta = '/noticias';",
                @"INSERT IGNORE INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado, usuario_creacion) VALUES
                  (1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1, 'SETUP'),
                  (3, 0, 'Gestión Documental', NULL, 'fa-solid fa-folder-open', 3, 1, 'SETUP'),
                  (31, 3, 'Gestión Correos Electrónicos', '/gestion-documental/gestion-correos-electronicos', 'fa-solid fa-envelope', 1, 1, 'SETUP'),
                  (4, 0, 'Administración', NULL, 'fa-solid fa-gears', 4, 1, 'SETUP'),
                  (401, 4, 'Inicio Administración', '/administracion/inicio', 'fa-solid fa-sliders', 1, 1, 'SETUP'),
                  (402, 4, 'Usuarios', '/administracion/usuarios', 'fa-solid fa-users-gear', 2, 1, 'SETUP'),
                  (403, 4, 'Roles', '/administracion/roles', 'fa-solid fa-user-shield', 3, 1, 'SETUP'),
                  (404, 4, 'Menú', '/administracion/menu', 'fa-solid fa-sitemap', 4, 1, 'SETUP'),
                  (405, 4, 'Configuración Sistema', '/administracion/configuracion-sistema', 'fa-solid fa-wrench', 5, 1, 'SETUP'),
                  (406, 4, 'Línea de Mando', '/administracion/linea-mando', 'fa-solid fa-user-tie', 6, 1, 'SETUP'),
                  (407, 4, 'Personaje del Mes', '/administracion/personaje-mes', 'fa-solid fa-award', 7, 1, 'SETUP'),
                  (408, 4, 'Dominios', '/administracion/dominio', 'fa-solid fa-list-check', 8, 1, 'SETUP'),
                  (409, 4, 'Auditoría', '/administracion/auditoria', 'fa-solid fa-clock-rotate-left', 9, 1, 'SETUP'),
                  (410, 4, 'Cuentas Email', '/administracion/cuentas-email', 'fa-solid fa-at', 10, 1, 'SETUP');",

                // Seed Data - Asignación Menú-Roles (Desarrollador y Administrador)
                @"INSERT IGNORE INTO CTR_MENU_ROLES (id_menu_rol, id_menu, id_rol, estado) VALUES
                  (1, 1, 1, 1), (2, 2, 1, 1), (3, 3, 1, 1), (4, 31, 1, 1), (5, 4, 1, 1),
                  (6, 401, 1, 1), (7, 402, 1, 1), (8, 403, 1, 1), (9, 404, 1, 1), (10, 405, 1, 1),
                  (11, 406, 1, 1), (12, 407, 1, 1), (13, 408, 1, 1), (14, 409, 1, 1), (15, 410, 1, 1),
                  (16, 1, 2, 1), (17, 2, 2, 1), (18, 3, 2, 1), (19, 31, 2, 1), (20, 4, 2, 1),
                  (21, 401, 2, 1), (22, 402, 2, 1), (23, 403, 2, 1), (24, 404, 2, 1), (25, 405, 2, 1),
                  (26, 406, 2, 1), (27, 407, 2, 1), (28, 408, 2, 1), (29, 409, 2, 1), (30, 410, 2, 1);",

                @"INSERT IGNORE INTO CTR_DOMINIO (id_dominio, descripcion, id_padre, vigente, abreviatura, observacion, usuario_creacion, maquina_creacion) VALUES
                  (1, 'RAIZ', 0, 1, 'RAIZ', 'RAIZ Principal', 1143458803, 'LOCAL'),
                  (2, 'Personaje del mes', 1, 1, 'PERS_MES', 'Categorías y opciones de Personaje del mes', 1143458803, 'LOCAL'),
                  (3, 'Categoría oficiales', 2, 1, 'PEROF', 'Personajes del mes en la categoría de oficial', 1143458803, 'LOCAL'),
                  (4, 'Categoría Mando de nivel ejecutivo', 2, 1, 'CATNIVEL', 'Personajes del mes en la categoría de mando de nivel ejecutivo', 1143458803, 'LOCAL'),
                  (5, 'Categoría patrulleros', 2, 1, 'CATPT', 'Personajes del mes en la categoría de patrulleros', 1143458803, 'LOCAL'),
                  (6, 'Personaje de mes ámbito operativo', 2, 1, 'PERAMOP', 'Personaje de mes ámbito operativo', 1143458803, 'LOCAL'),
                  (7, 'Categoría no uniformados', 2, 1, 'CATNU', 'Categoría no uniformados', 1143458803, 'LOCAL'),
                  (8, 'Equipo o grupo de alto rendimiento', 2, 1, 'EQUGP', 'Equipo o grupo de alto rendimiento', 1143458803, 'LOCAL'),
                  (9, 'Categoría estudiantes', 2, 1, 'CATEST', 'Categoría estudiantes', 1143458803, 'LOCAL'),
                  (10, 'Categoría suboficial', 2, 1, 'CATSUBOF', 'Categoría suboficial', 1143458803, 'LOCAL'),
                  (11, 'Categoria agentes', 2, 1, 'CATAG', 'Categoria para agrupar agentes de policía', 1143458803, 'LOCAL'),
                  (12, 'Categoría Auxiliares de Policia', 2, 1, 'CAUXPO', 'Categoria para Auxiliares De Policia', 1143458803, 'LOCAL'),
                  (13, 'Privacidad de Documentos', 1, 1, 'PRIV_DOC', 'Privacidad de Documentos', 1143458803, 'LOCAL'),
                  (14, 'INFORMACIÓN PÚBLICA', 13, 1, 'INFPUB', 'Es toda información que un sujeto obligado genere, obtenga o controle', 1143458803, 'LOCAL'),
                  (15, 'INFORMACIÓN PÚBLICA CLASIFICADA', 13, 1, 'INFPUBCLA', 'Es aquella información que estando en poder o custodia de un sujeto obligado es clasificada', 1143458803, 'LOCAL'),
                  (16, 'INFORMACIÓN PÚBLICA RESERVADA', 13, 1, 'PUBRES', 'Es aquella información que estando en poder o custodia de un sujeto obligado es reservada', 1143458803, 'LOCAL'),
                  (17, 'GRUPOS O AREAS', 1, 1, 'GRUPOS_AREAS', 'Dominio Padre Semilla para Grupos o Áreas responsables de cuentas de correo', 1143458803, 'LOCAL'),
                  (18, 'Grupo Desarrollo de Software', 17, 1, 'GDS', 'Grupo Desarrollo de Software ejemplo responsable de correo', 1143458803, 'LOCAL');"
            };

            foreach (var stmt in ddlStatements)
            {
                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = stmt;
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error ejecutando DDL MySQL: {Sql}", stmt.Length > 100 ? stmt.Substring(0, 100) + "..." : stmt);
                    result.Errors.Add(ex.Message);
                }
            }

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                result.Message = $"Migración MySQL completada con {result.Errors.Count} advertencia(s): {string.Join(" | ", result.Errors)}";
            }
            else
            {
                result.Success = true;
                result.Message = "Migración de base de datos MySQL completada exitosamente.";
            }

            return result;
        }

        private static async Task<DtoMigrationResult> MigratePostgreSqlAsync(string cs, ILogger logger, CancellationToken ct)
        {
            var result = new DtoMigrationResult();
            await using var conn = new Npgsql.NpgsqlConnection(cs);
            await conn.OpenAsync(ct);

            // Garantizar la creación automática de esquemas personalizados (ej. SearchPath=prueba)
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(cs);
                if (!string.IsNullOrWhiteSpace(builder.SearchPath))
                {
                    var schemas = builder.SearchPath.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var s in schemas)
                    {
                        var cleanSchema = s.Replace("\"", "").Trim();
                        if (!string.IsNullOrWhiteSpace(cleanSchema) && !cleanSchema.Equals("public", StringComparison.OrdinalIgnoreCase) && !cleanSchema.StartsWith("$"))
                        {
                            await using var cmdSchema = conn.CreateCommand();
                            cmdSchema.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{cleanSchema}\";";
                            await cmdSchema.ExecuteNonQueryAsync(ct);
                            logger.LogInformation("Esquema PostgreSQL '{Schema}' verificado/creado exitosamente.", cleanSchema);
                        }
                    }

                    // Establecer explícitamente el search_path activo para la sesión de migración
                    await using var cmdSetPath = conn.CreateCommand();
                    cmdSetPath.CommandText = $"SET search_path TO {builder.SearchPath};";
                    await cmdSetPath.ExecuteNonQueryAsync(ct);
                }
            }
            catch (Exception exSchema)
            {
                logger.LogWarning(exSchema, "Advertencia verificando/creando esquema en PostgreSQL.");
            }

            string[] ddlStatements = new[]
            {
                // 1. ctr_usuarios
                @"CREATE TABLE IF NOT EXISTS ctr_usuarios (
                    id_usuario BIGSERIAL PRIMARY KEY,
                    bloqueado SMALLINT DEFAULT 0 NOT NULL,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    identificacion VARCHAR(50) NOT NULL UNIQUE,
                    grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                    apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                    nombres VARCHAR(255) DEFAULT '' NOT NULL,
                    funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                    unde_laborando BIGINT DEFAULT 0 NOT NULL,
                    codigocargo BIGINT DEFAULT 0 NOT NULL,
                    usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                    usuario_modifica BIGINT,
                    fecha_modifica TIMESTAMP,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE ctr_usuarios
                    ADD COLUMN IF NOT EXISTS grad_alfabetico VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS apellidos VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS nombres VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS funcionario VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS unde_laborando BIGINT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS codigocargo BIGINT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_modifica BIGINT,
                    ADD COLUMN IF NOT EXISTS fecha_modifica TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(255);",

                // 2. ctr_roles
                @"CREATE TABLE IF NOT EXISTS ctr_roles (
                    id_rol SERIAL PRIMARY KEY,
                    codigo_rol VARCHAR(50) DEFAULT '' NOT NULL,
                    nombre_rol VARCHAR(255) DEFAULT '' NOT NULL,
                    descripcion VARCHAR(500),
                    estado INT DEFAULT 1 NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    maquina_creacion VARCHAR(100) DEFAULT 'LOCAL',
                    fecha_modifica TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    maquina_modifica VARCHAR(100)
                );",
                @"ALTER TABLE ctr_roles
                    ADD COLUMN IF NOT EXISTS codigo_rol VARCHAR(50) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS nombre_rol VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS descripcion VARCHAR(500),
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(100) DEFAULT 'LOCAL',
                    ADD COLUMN IF NOT EXISTS fecha_modifica TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS usuario_modifica VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(100);",

                // 3. ctr_menu
                @"CREATE TABLE IF NOT EXISTS ctr_menu (
                    id_menu INT PRIMARY KEY,
                    id_padre INT DEFAULT 0 NOT NULL,
                    titulo VARCHAR(255) NOT NULL,
                    ruta VARCHAR(255),
                    icono VARCHAR(100),
                    orden INT DEFAULT 1 NOT NULL,
                    estado INT DEFAULT 1 NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    maquina_creacion VARCHAR(255),
                    usuario_modifica VARCHAR(100),
                    fecha_modifica TIMESTAMP,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE ctr_menu
                    ADD COLUMN IF NOT EXISTS id_padre INT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS titulo VARCHAR(255) DEFAULT '' NOT NULL,
                    ADD COLUMN IF NOT EXISTS ruta VARCHAR(255),
                    ADD COLUMN IF NOT EXISTS icono VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS orden INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(255),
                    ADD COLUMN IF NOT EXISTS usuario_modifica VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS fecha_modifica TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS maquina_modifica VARCHAR(255);",

                // 4. ctr_menu_roles
                @"CREATE TABLE IF NOT EXISTS ctr_menu_roles (
                    id_menu_rol SERIAL PRIMARY KEY,
                    id_menu INT NOT NULL,
                    id_rol INT NOT NULL,
                    estado INT DEFAULT 1 NOT NULL
                );",
                @"ALTER TABLE ctr_menu_roles
                    ADD COLUMN IF NOT EXISTS id_menu INT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS id_rol INT NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL;",

                // 5. ctr_roles_user
                @"CREATE TABLE IF NOT EXISTS ctr_roles_user (
                    id_user_rol BIGSERIAL PRIMARY KEY,
                    id_usuario BIGINT NOT NULL,
                    id_rol INT NOT NULL,
                    justificacion VARCHAR(500),
                    fecha_fin DATE,
                    estado INT DEFAULT 1 NOT NULL,
                    usuario_creacion BIGINT NOT NULL,
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    maquina_creacion VARCHAR(255) NOT NULL
                );",
                @"ALTER TABLE ctr_roles_user
                    ADD COLUMN IF NOT EXISTS justificacion VARCHAR(500),
                    ADD COLUMN IF NOT EXISTS fecha_fin DATE,
                    ADD COLUMN IF NOT EXISTS estado INT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS usuario_creacion BIGINT DEFAULT 1 NOT NULL,
                    ADD COLUMN IF NOT EXISTS fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    ADD COLUMN IF NOT EXISTS maquina_creacion VARCHAR(255) DEFAULT 'LOCAL' NOT NULL;",

                // 6. ctr_roles_user_admin
                @"CREATE TABLE IF NOT EXISTS ctr_roles_user_admin (
                    id_usuario BIGINT NOT NULL,
                    id_rol INT NOT NULL,
                    justificacion VARCHAR(1000),
                    fecha_fin DATE,
                    vigente INT DEFAULT 1,
                    PRIMARY KEY (id_usuario, id_rol)
                );",
                @"ALTER TABLE ctr_roles_user_admin
                    ADD COLUMN IF NOT EXISTS justificacion VARCHAR(1000),
                    ADD COLUMN IF NOT EXISTS fecha_fin DATE,
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1;",

                // 7. ctr_dominio
                @"CREATE TABLE IF NOT EXISTS ctr_dominio (
                    id_dominio SERIAL PRIMARY KEY,
                    descripcion VARCHAR(500) NOT NULL,
                    id_padre INT DEFAULT 0 NOT NULL,
                    abreviatura VARCHAR(100),
                    observacion VARCHAR(500),
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP',
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    maquina_creacion VARCHAR(255) DEFAULT 'LOCAL',
                    usuario_modifica VARCHAR(100),
                    fecha_modifica TIMESTAMP,
                    maquina_modifica VARCHAR(255)
                );",
                @"ALTER TABLE ctr_dominio
                    ADD COLUMN IF NOT EXISTS id_padre INT DEFAULT 0 NOT NULL,
                    ADD COLUMN IF NOT EXISTS abreviatura VARCHAR(100),
                    ADD COLUMN IF NOT EXISTS observacion VARCHAR(500),
                    ADD COLUMN IF NOT EXISTS vigente INT DEFAULT 1 NOT NULL;",

                // 8. ctr_auditoria
                @"CREATE TABLE IF NOT EXISTS ctr_auditoria (
                    id_auditoria BIGSERIAL PRIMARY KEY,
                    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario VARCHAR(100) NOT NULL,
                    modulo VARCHAR(100) NOT NULL,
                    accion VARCHAR(50) NOT NULL,
                    registro TEXT,
                    resultado VARCHAR(50),
                    maquina VARCHAR(100),
                    observaciones TEXT
                );",
                @"ALTER TABLE ctr_auditoria
                    ADD COLUMN IF NOT EXISTS observaciones TEXT;",

                // 9. ctr_errores_system
                @"CREATE TABLE IF NOT EXISTS ctr_errores_system (
                    id_error BIGSERIAL PRIMARY KEY,
                    procedimiento VARCHAR(255) NOT NULL,
                    descripcion TEXT,
                    id_documento VARCHAR(255),
                    usuario BIGINT,
                    fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                );",

                // 10. ctr_branding
                @"CREATE TABLE IF NOT EXISTS ctr_branding (
                    id_branding BIGSERIAL PRIMARY KEY,
                    sistema VARCHAR(50) NOT NULL,
                    nombre_sistema VARCHAR(100) NOT NULL,
                    unidad_sistema_id INT,
                    logo_file_name VARCHAR(255),
                    favicon_file_name VARCHAR(255),
                    fondo_1_file_name VARCHAR(255),
                    fondo_2_file_name VARCHAR(255),
                    fondo_3_file_name VARCHAR(255),
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_creacion VARCHAR(50) DEFAULT 'SISTEMA' NOT NULL,
                    maquina_creacion VARCHAR(100) DEFAULT 'LOCAL' NOT NULL,
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_modificacion VARCHAR(50),
                    maquina_modificacion VARCHAR(100),
                    fecha_modificacion TIMESTAMP,
                    sigla VARCHAR(100),
                    nombre_unidad VARCHAR(255)
                );",

                // 11. login_visual
                @"CREATE TABLE IF NOT EXISTS login_visual (
                    id_login_visual SERIAL PRIMARY KEY,
                    descripcion VARCHAR(255),
                    imagen_fondo VARCHAR(500),
                    orden INT DEFAULT 1,
                    vigente INT DEFAULT 1 NOT NULL
                );",

                // 12. ctr_linea_mando
                @"CREATE TABLE IF NOT EXISTS ctr_linea_mando (
                    id_linea_mando SERIAL PRIMARY KEY,
                    identificacion VARCHAR(20) NOT NULL,
                    nombre VARCHAR(100),
                    apellidos VARCHAR(100),
                    grado VARCHAR(50),
                    cargo VARCHAR(100),
                    unidad VARCHAR(200),
                    foto_base64 TEXT,
                    orden INT DEFAULT 1,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod TIMESTAMP,
                    maquina_mod VARCHAR(100),
                    peso VARCHAR(100)
                );",

                // 13. ctr_personaje_grupo
                @"CREATE TABLE IF NOT EXISTS ctr_personaje_grupo (
                    id_personaje_grupo SERIAL PRIMARY KEY,
                    nombre_grupo VARCHAR(100),
                    numero_acta VARCHAR(50),
                    mes INT,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod TIMESTAMP,
                    maquina_mod VARCHAR(50),
                    foto_grupo VARCHAR(200),
                    anio INT
                );",

                // 14. ctr_personaje_mes
                @"CREATE TABLE IF NOT EXISTS ctr_personaje_mes (
                    id_personaje_mes SERIAL PRIMARY KEY,
                    identificacion VARCHAR(20),
                    nombres VARCHAR(100),
                    apellidos VARCHAR(100),
                    grado VARCHAR(50),
                    cargo VARCHAR(100),
                    unidad VARCHAR(200),
                    id_categoria INT,
                    numero_acta VARCHAR(50),
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(50),
                    fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    maquina_crea VARCHAR(100),
                    usuario_mod VARCHAR(50),
                    fecha_mod TIMESTAMP,
                    maquina_mod VARCHAR(100),
                    foto_modificada VARCHAR(200),
                    mes INT,
                    anio INT,
                    id_personaje_grupo INT
                );",

                // 15. ctr_videos
                @"CREATE TABLE IF NOT EXISTS ctr_videos (
                    id_video SERIAL PRIMARY KEY,
                    descripcion VARCHAR(255) NOT NULL,
                    observaciones VARCHAR(500),
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    usuario_creacion VARCHAR(50) NOT NULL,
                    ruta_video VARCHAR(500),
                    vigente INT DEFAULT 1,
                    fecha_modifica TIMESTAMP,
                    usuario_modifica VARCHAR(50),
                    maquina_modifica VARCHAR(100)
                );",

                // 16. ctr_cuenta_email
                @"CREATE TABLE IF NOT EXISTS ctr_cuenta_email (
                    id_cuenta SERIAL PRIMARY KEY,
                    id_dominio_grupo INT NOT NULL,
                    nombre_cuenta VARCHAR(200) NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    servidor_smtp VARCHAR(255) DEFAULT 'smtp.office365.com',
                    puerto INT DEFAULT 587,
                    usuario_smtp VARCHAR(255),
                    clave_smtp VARCHAR(1000),
                    usar_ssl INT DEFAULT 1,
                    vigente INT DEFAULT 1,
                    usuario_crea VARCHAR(100),
                    fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    fecha_modifica TIMESTAMP,
                    ip_registro VARCHAR(50),
                    firma_html TEXT,
                    imagen_encabezado VARCHAR(500)
                );",

                // 17. ctr_cuenta_email_usuario
                @"CREATE TABLE IF NOT EXISTS ctr_cuenta_email_usuario (
                    id_cuenta_usuario SERIAL PRIMARY KEY,
                    id_cuenta INT NOT NULL,
                    id_usuario BIGINT NOT NULL,
                    vigente INT DEFAULT 1 NOT NULL,
                    usuario_crea VARCHAR(100),
                    fecha_crea TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    fecha_modifica TIMESTAMP,
                    ip_registro VARCHAR(45)
                );",

                // Seed Data - Roles
                @"INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion, vigente, usuario_creacion, maquina_creacion)
                  VALUES (1, 'DESARROLLADOR', 'Desarrollador', 'Rol con acceso total de desarrollo y plantilla', 1, 'SETUP', 'LOCAL')
                  ON CONFLICT (id_rol) DO NOTHING;",

                @"INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion, vigente, usuario_creacion, maquina_creacion)
                  VALUES (2, 'ADMINISTRADOR', 'Administrador', 'Rol administrador del sistema', 1, 'SETUP', 'LOCAL')
                  ON CONFLICT (id_rol) DO NOTHING;",

                // Seed Data - Menús Base
                @"INSERT INTO ctr_menu (id_menu, id_padre, titulo, ruta, icono, orden, estado, usuario_creacion) VALUES
                  (1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1, 'SETUP'),
                  (3, 0, 'Gestión Documental', NULL, 'fa-solid fa-folder-open', 3, 1, 'SETUP'),
                  (31, 3, 'Gestión Correos Electrónicos', '/gestion-documental/gestion-correos-electronicos', 'fa-solid fa-envelope', 1, 1, 'SETUP'),
                  (4, 0, 'Administración', NULL, 'fa-solid fa-gears', 4, 1, 'SETUP'),
                  (401, 4, 'Inicio Administración', '/administracion/inicio', 'fa-solid fa-sliders', 1, 1, 'SETUP'),
                  (402, 4, 'Usuarios', '/administracion/usuarios', 'fa-solid fa-users-gear', 2, 1, 'SETUP'),
                  (403, 4, 'Roles', '/administracion/roles', 'fa-solid fa-user-shield', 3, 1, 'SETUP'),
                  (404, 4, 'Menú', '/administracion/menu', 'fa-solid fa-sitemap', 4, 1, 'SETUP'),
                  (405, 4, 'Configuración Sistema', '/administracion/configuracion-sistema', 'fa-solid fa-wrench', 5, 1, 'SETUP'),
                  (406, 4, 'Línea de Mando', '/administracion/linea-mando', 'fa-solid fa-user-tie', 6, 1, 'SETUP'),
                  (407, 4, 'Personaje del Mes', '/administracion/personaje-mes', 'fa-solid fa-award', 7, 1, 'SETUP'),
                  (408, 4, 'Dominios', '/administracion/dominio', 'fa-solid fa-list-check', 8, 1, 'SETUP'),
                  (409, 4, 'Auditoría', '/administracion/auditoria', 'fa-solid fa-clock-rotate-left', 9, 1, 'SETUP'),
                  (410, 4, 'Cuentas Email', '/administracion/cuentas-email', 'fa-solid fa-at', 10, 1, 'SETUP')
                  ON CONFLICT (id_menu) DO NOTHING;",

                // Seed Data - Asignación Menú-Roles
                @"INSERT INTO ctr_menu_roles (id_menu_rol, id_menu, id_rol, estado) VALUES
                  (1, 1, 1, 1), (2, 2, 1, 1), (3, 3, 1, 1), (4, 31, 1, 1), (5, 4, 1, 1),
                  (6, 401, 1, 1), (7, 402, 1, 1), (8, 403, 1, 1), (9, 404, 1, 1), (10, 405, 1, 1),
                  (11, 406, 1, 1), (12, 407, 1, 1), (13, 408, 1, 1), (14, 409, 1, 1), (15, 410, 1, 1),
                  (16, 1, 2, 1), (17, 2, 2, 1), (18, 3, 2, 1), (19, 31, 2, 1), (20, 4, 2, 1),
                  (21, 401, 2, 1), (22, 402, 2, 1), (23, 403, 2, 1), (24, 404, 2, 1), (25, 405, 2, 1),
                  (26, 406, 2, 1), (27, 407, 2, 1), (28, 408, 2, 1), (29, 409, 2, 1), (30, 410, 2, 1)
                  ON CONFLICT (id_menu_rol) DO NOTHING;",

                // Seed Data - Dominios Semilla
                @"INSERT INTO ctr_dominio (id_dominio, descripcion, id_padre, vigente, abreviatura, observacion, usuario_creacion, maquina_creacion) VALUES
                  (1, 'RAIZ', 0, 1, 'RAIZ', 'RAIZ Principal', 1143458803, 'LOCAL'),
                  (2, 'Personaje del mes', 1, 1, 'PERS_MES', 'Categorías y opciones de Personaje del mes', 1143458803, 'LOCAL'),
                  (3, 'Categoría oficiales', 2, 1, 'PEROF', 'Personajes del mes en la categoría de oficial', 1143458803, 'LOCAL'),
                  (4, 'Categoría Mando de nivel ejecutivo', 2, 1, 'CATNIVEL', 'Personajes del mes en la categoría de mando de nivel ejecutivo', 1143458803, 'LOCAL'),
                  (5, 'Categoría patrulleros', 2, 1, 'CATPT', 'Personajes del mes en la categoría de patrulleros', 1143458803, 'LOCAL'),
                  (6, 'Personaje de mes ámbito operativo', 2, 1, 'PERAMOP', 'Personaje de mes ámbito operativo', 1143458803, 'LOCAL'),
                  (7, 'Categoría no uniformados', 2, 1, 'CATNU', 'Categoría no uniformados', 1143458803, 'LOCAL'),
                  (8, 'Equipo o grupo de alto rendimiento', 2, 1, 'EQUGP', 'Equipo o grupo de alto rendimiento', 1143458803, 'LOCAL'),
                  (9, 'Categoría estudiantes', 2, 1, 'CATEST', 'Categoría estudiantes', 1143458803, 'LOCAL'),
                  (10, 'Categoría suboficial', 2, 1, 'CATSUBOF', 'Categoría suboficial', 1143458803, 'LOCAL'),
                  (11, 'Categoria agentes', 2, 1, 'CATAG', 'Categoria para agrupar agentes de policía', 1143458803, 'LOCAL'),
                  (12, 'Categoría Auxiliares de Policia', 2, 1, 'CAUXPO', 'Categoria para Auxiliares De Policia', 1143458803, 'LOCAL'),
                  (13, 'Privacidad de Documentos', 1, 1, 'PRIV_DOC', 'Privacidad de Documentos', 1143458803, 'LOCAL'),
                  (14, 'INFORMACIÓN PÚBLICA', 13, 1, 'INFPUB', 'Es toda información que un sujeto obligado genere, obtenga o controle', 1143458803, 'LOCAL'),
                  (15, 'INFORMACIÓN PÚBLICA CLASIFICADA', 13, 1, 'INFPUBCLA', 'Es aquella información que estando en poder o custodia de un sujeto obligado es clasificada', 1143458803, 'LOCAL'),
                  (16, 'INFORMACIÓN PÚBLICA RESERVADA', 13, 1, 'PUBRES', 'Es aquella información que estando en poder o custodia de un sujeto obligado es reservada', 1143458803, 'LOCAL'),
                  (17, 'GRUPOS O AREAS', 1, 1, 'GRUPOS_AREAS', 'Dominio Padre Semilla para Grupos o Áreas responsables de cuentas de correo', 1143458803, 'LOCAL'),
                  (18, 'Grupo Desarrollo de Software', 17, 1, 'GDS', 'Grupo Desarrollo de Software ejemplo responsable de correo', 1143458803, 'LOCAL')
                  ON CONFLICT (id_dominio) DO NOTHING;",

                // =====================================================================
                // SI2FA - Sistema de Información de Doble Factor de Autenticación
                // =====================================================================

                // Tabla de sistemas integrados (reemplaza "SistemasConfiables" de appsettings)
                @"CREATE TABLE IF NOT EXISTS mfa_sistemas (
                    id_sistema SERIAL PRIMARY KEY,
                    sigla VARCHAR(100) NOT NULL UNIQUE,
                    nombre VARCHAR(255) NOT NULL,
                    descripcion VARCHAR(500),
                    secret_hmac TEXT NOT NULL,
                    habilitado INT DEFAULT 1 NOT NULL,
                    creado_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    actualizado_at TIMESTAMP,
                    rotado_at TIMESTAMP,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP',
                    usuario_modifica VARCHAR(100)
                );",

                // Rol CSIRT (grupo de seguridad que administra el servicio 2FA)
                @"INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion, vigente, usuario_creacion, maquina_creacion)
                  VALUES (3, 'CSIRT', 'CSIRT - Seguridad', 'Grupo de seguridad (CSIRT) administrador del servicio de doble autenticación', 1, 'SETUP', 'LOCAL')
                  ON CONFLICT (id_rol) DO NOTHING;",

                // Menús del panel SI2FA
                @"INSERT INTO ctr_menu (id_menu, id_padre, titulo, ruta, icono, orden, estado, usuario_creacion) VALUES
                  (5, 0, 'Doble Autenticación (SI2FA)', NULL, 'fa-solid fa-shield-halved', 2, 1, 'SETUP'),
                  (501, 5, 'Tablero 2FA', '/si2fa/dashboard', 'fa-solid fa-gauge-high', 1, 1, 'SETUP'),
                  (502, 5, 'Usuarios 2FA', '/si2fa/usuarios', 'fa-solid fa-user-lock', 2, 1, 'SETUP'),
                  (503, 5, 'Sistemas Integrados', '/si2fa/sistemas', 'fa-solid fa-plug-circle-check', 3, 1, 'SETUP'),
                  (504, 5, 'Dispositivos Confiables', '/si2fa/dispositivos', 'fa-solid fa-mobile-screen', 4, 1, 'SETUP'),
                  (505, 5, 'Auditoría 2FA', '/si2fa/auditoria', 'fa-solid fa-clipboard-list', 5, 1, 'SETUP'),
                  (506, 5, 'Parámetros', '/si2fa/parametros', 'fa-solid fa-sliders', 6, 1, 'SETUP')
                  ON CONFLICT (id_menu) DO NOTHING;",

                // Asignación de los menús SI2FA a los roles Desarrollador(1), Administrador(2) y CSIRT(3)
                @"INSERT INTO ctr_menu_roles (id_menu_rol, id_menu, id_rol, estado) VALUES
                  (31, 5, 1, 1), (32, 501, 1, 1), (33, 502, 1, 1), (34, 503, 1, 1), (35, 504, 1, 1), (36, 505, 1, 1),
                  (37, 5, 2, 1), (38, 501, 2, 1), (39, 502, 2, 1), (40, 503, 2, 1), (41, 504, 2, 1), (42, 505, 2, 1),
                  (43, 5, 3, 1), (44, 501, 3, 1), (45, 502, 3, 1), (46, 503, 3, 1), (47, 504, 3, 1), (48, 505, 3, 1),
                  (49, 506, 1, 1), (50, 506, 2, 1), (51, 506, 3, 1)
                  ON CONFLICT (id_menu_rol) DO NOTHING;",

                // Tabla de parámetros configurables del servicio 2FA (editables desde el panel SI2FA)
                @"CREATE TABLE IF NOT EXISTS mfa_parametros (
                    clave VARCHAR(80) PRIMARY KEY,
                    valor VARCHAR(255) NOT NULL,
                    tipo VARCHAR(20) DEFAULT 'INT' NOT NULL,
                    descripcion VARCHAR(300),
                    unidad VARCHAR(40),
                    grupo VARCHAR(60) DEFAULT 'General',
                    min_valor INT,
                    max_valor INT,
                    actualizado_at TIMESTAMP,
                    usuario_modifica VARCHAR(100)
                );",

                // Valores por defecto (coinciden con los usados hoy por el servicio/funciones)
                @"INSERT INTO mfa_parametros (clave, valor, tipo, descripcion, unidad, grupo, min_valor, max_valor) VALUES
                  ('RESET_CODE_TTL_MIN', '5',  'INT', 'Vigencia del código de restablecimiento enviado al correo', 'minutos', 'Restablecimiento', 1, 60),
                  ('RESET_RATE_MIN',     '5',  'INT', 'Tiempo mínimo entre solicitudes de restablecimiento',        'minutos', 'Restablecimiento', 1, 120),
                  ('VERIFY_MAX_INTENTOS','5',  'INT', 'Intentos fallidos de verificación antes de bloquear',        'intentos','Verificación',    1, 20),
                  ('BLOQUEO_MINUTOS',    '15', 'INT', 'Duración del bloqueo temporal por intentos fallidos',         'minutos', 'Verificación',    1, 1440),
                  ('TRUSTED_DEVICE_DIAS','30', 'INT', 'Vigencia de un dispositivo marcado como confiable',           'días',    'Dispositivos',    1, 365),
                  ('ANTIREPLAY_MIN',     '2',  'INT', 'Ventana anti-replay para la emisión del token de servicio',  'minutos', 'Seguridad',       1, 10)
                  ON CONFLICT (clave) DO NOTHING;"
            };

            foreach (var stmt in ddlStatements)
            {
                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = stmt;
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error ejecutando DDL PostgreSQL: {Sql}", stmt.Length > 100 ? stmt.Substring(0, 100) + "..." : stmt);
                    result.Errors.Add(ex.Message);
                }
            }

            // Sincronizar las secuencias SERIAL en PostgreSQL para evitar errores de clave duplicada tras sembrado de IDs explícitos
            string[] seqStatements = new[]
            {
                "SELECT setval(pg_get_serial_sequence('ctr_roles', 'id_rol'), COALESCE((SELECT MAX(id_rol) FROM ctr_roles), 1));",
                "SELECT setval(pg_get_serial_sequence('ctr_menu_roles', 'id_menu_rol'), COALESCE((SELECT MAX(id_menu_rol) FROM ctr_menu_roles), 1));",
                "SELECT setval(pg_get_serial_sequence('ctr_dominio', 'id_dominio'), COALESCE((SELECT MAX(id_dominio) FROM ctr_dominio), 1));"
            };

            foreach (var seqSql in seqStatements)
            {
                try
                {
                    await using var cmdSeq = conn.CreateCommand();
                    cmdSeq.CommandText = seqSql;
                    await cmdSeq.ExecuteNonQueryAsync(ct);
                }
                catch (Exception exSeq)
                {
                    logger.LogWarning(exSeq, "Aviso ajustando secuencia en PostgreSQL: {Sql}", seqSql);
                }
            }

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                result.Message = $"Migración PostgreSQL completada con {result.Errors.Count} advertencia(s): {string.Join(" | ", result.Errors)}";
            }
            else
            {
                result.Success = true;
                result.Message = "Migración de base de datos PostgreSQL completada exitosamente.";
            }

            return result;
        }

        private static async Task<DtoMigrationResult> MigrateOracleAsync(string cs, ILogger logger, CancellationToken ct, IConfiguration? cfg = null)
        {
            var tsDataName = (cfg?["OracleSettings:TablespaceData"] ?? "").Trim();
            var tsIndName = (cfg?["OracleSettings:TablespaceIndexes"] ?? "").Trim();

            await using var conn = new Oracle.ManagedDataAccess.Client.OracleConnection(cs);
            await conn.OpenAsync(ct);

            string tsDataClause = "";
            string tsIndClause = "";

            try
            {
                if (!string.IsNullOrWhiteSpace(tsDataName))
                {
                    await using var cmdData = conn.CreateCommand();
                    cmdData.CommandText = "SELECT COUNT(*) FROM ALL_TABLESPACES WHERE TABLESPACE_NAME = UPPER(:ts)";
                    cmdData.Parameters.Add(":ts", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = tsDataName;
                    var countData = Convert.ToInt32(await cmdData.ExecuteScalarAsync(ct));
                    if (countData > 0)
                    {
                        tsDataClause = $" TABLESPACE {tsDataName}";
                    }
                    else
                    {
                        logger.LogWarning("El tablespace de datos '{TsData}' no existe en la BD Oracle. Se usará el tablespace por defecto del usuario.", tsDataName);
                    }
                }

                if (!string.IsNullOrWhiteSpace(tsIndName))
                {
                    await using var cmdInd = conn.CreateCommand();
                    cmdInd.CommandText = "SELECT COUNT(*) FROM ALL_TABLESPACES WHERE TABLESPACE_NAME = UPPER(:ts)";
                    cmdInd.Parameters.Add(":ts", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = tsIndName;
                    var countInd = Convert.ToInt32(await cmdInd.ExecuteScalarAsync(ct));
                    if (countInd > 0)
                    {
                        tsIndClause = $" TABLESPACE {tsIndName}";
                    }
                    else
                    {
                        logger.LogWarning("El tablespace de índices '{TsInd}' no existe en la BD Oracle. Se usará el tablespace por defecto del usuario.", tsIndName);
                    }
                }
            }
            catch (Exception exCheck)
            {
                logger.LogWarning(exCheck, "Aviso verificando ALL_TABLESPACES en Oracle.");
                if (!string.IsNullOrWhiteSpace(tsDataName)) tsDataClause = $" TABLESPACE {tsDataName}";
                if (!string.IsNullOrWhiteSpace(tsIndName)) tsIndClause = $" TABLESPACE {tsIndName}";
            }

            string usingIndexClause = string.IsNullOrWhiteSpace(tsIndClause) ? "" : $" USING INDEX{tsIndClause}";

            logger.LogInformation("Migración Oracle activa. Tablespace Datos: '{TsData}' ({DataClause}), Tablespace Índices: '{TsInd}' ({IndClause})", 
                tsDataName, string.IsNullOrWhiteSpace(tsDataClause) ? "Default User TS" : tsDataClause, 
                tsIndName, string.IsNullOrWhiteSpace(tsIndClause) ? "Default User TS" : tsIndClause);

            string[] blocks = new[]
            {
                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_USUARIOS (
                      ID_USUARIO       NUMBER NOT NULL,
                      BLOQUEADO        NUMBER(1) DEFAULT 0 NOT NULL,
                      USERNAME         VARCHAR2(255 CHAR) NOT NULL,
                      IDENTIFICACION   NUMBER NOT NULL,
                      GRAD_ALFABETICO  VARCHAR2(255 CHAR) DEFAULT '''' NOT NULL,
                      APELLIDOS        VARCHAR2(255 CHAR) DEFAULT '''' NOT NULL,
                      NOMBRES          VARCHAR2(255 CHAR) DEFAULT '''' NOT NULL,
                      FUNCIONARIO      VARCHAR2(255 CHAR) DEFAULT '''' NOT NULL,
                      UNDE_LABORANDO   NUMBER DEFAULT 0 NOT NULL,
                      CODIGOCARGO      NUMBER DEFAULT 0 NOT NULL,
                      USUARIO_CREACION NUMBER DEFAULT 1 NOT NULL,
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREACION VARCHAR2(255 CHAR) DEFAULT ''LOCAL'' NOT NULL,
                      USUARIO_MODIFICA NUMBER,
                      FECHA_MODIFICA   DATE,
                      MAQUINA_MODIFICA VARCHAR2(255 CHAR),
                      CONSTRAINT CTR_USUARIOS_A1_PK PRIMARY KEY (ID_USUARIO){usingIndexClause},
                      CONSTRAINT CTR_USUARIOS_A2_UK UNIQUE (USERNAME){usingIndexClause},
                      CONSTRAINT CTR_USUARIOS_A3_UK UNIQUE (IDENTIFICACION){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN EXECUTE IMMEDIATE 'CREATE INDEX CTR_USUARIOS_B1_I ON CTR_USUARIOS (UNDE_LABORANDO){tsIndClause}'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 AND SQLCODE != -1408 THEN RAISE; END IF; END;",
                $@"BEGIN EXECUTE IMMEDIATE 'CREATE INDEX CTR_USUARIOS_B2_I ON CTR_USUARIOS (CODIGOCARGO){tsIndClause}'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 AND SQLCODE != -1408 THEN RAISE; END IF; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_ROLES (
                      ID_ROL           NUMBER NOT NULL,
                      CODIGO_ROL       VARCHAR2(50 CHAR) NOT NULL,
                      NOMBRE_ROL       VARCHAR2(255 CHAR) NOT NULL,
                      DESCRIPCION      VARCHAR2(500 CHAR),
                      ESTADO           NUMBER(1) DEFAULT 1 NOT NULL,
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO_CREACION VARCHAR2(100 CHAR) NOT NULL,
                      MAQUINA_CREACION VARCHAR2(100 CHAR),
                      FECHA_MODIFICA   DATE,
                      USUARIO_MODIFICA VARCHAR2(100 CHAR),
                      MAQUINA_MODIFICA VARCHAR2(100 CHAR),
                      CONSTRAINT CTR_ROLES_PK PRIMARY KEY (ID_ROL){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_MENU (
                      ID_MENU          NUMBER NOT NULL,
                      ID_PADRE         NUMBER DEFAULT 0 NOT NULL,
                      TITULO           VARCHAR2(255 CHAR) NOT NULL,
                      RUTA             VARCHAR2(255 CHAR),
                      ICONO            VARCHAR2(100 CHAR),
                      ORDEN            NUMBER DEFAULT 1 NOT NULL,
                      ESTADO           NUMBER(1) DEFAULT 1 NOT NULL,
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO_CREACION VARCHAR2(100 CHAR) NOT NULL,
                      CONSTRAINT CTR_MENU_PK PRIMARY KEY (ID_MENU){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU ADD MAQUINA_CREACION VARCHAR2(255 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU ADD USUARIO_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU ADD FECHA_MODIFICA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU ADD MAQUINA_MODIFICA VARCHAR2(255 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_MENU_ROLES (
                      ID_MENU_ROL      NUMBER NOT NULL,
                      ID_MENU          NUMBER NOT NULL,
                      ID_ROL           NUMBER NOT NULL,
                      ESTADO           NUMBER(1) DEFAULT 1 NOT NULL,
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO_CREACION VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      MAQUINA_CREACION VARCHAR2(255 CHAR) DEFAULT ''LOCAL'',
                      FECHA_MODIFICA   DATE,
                      USUARIO_MODIFICA VARCHAR2(100 CHAR),
                      MAQUINA_MODIFICA VARCHAR2(255 CHAR),
                      CONSTRAINT CTR_MENU_ROLES_PK PRIMARY KEY (ID_MENU_ROL){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD USUARIO_CREACION VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD FECHA_CREACION DATE DEFAULT SYSDATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD MAQUINA_CREACION VARCHAR2(255 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD USUARIO_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD FECHA_MODIFICA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_MENU_ROLES ADD MAQUINA_MODIFICA VARCHAR2(255 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_ROLES_USER_ADMIN (
                      ID_USER_ROL      NUMBER NOT NULL,
                      ID_USUARIO       NUMBER NOT NULL,
                      ID_ROL           NUMBER NOT NULL,
                      VIGENTE          NUMBER(1) DEFAULT 1 NOT NULL,
                      JUSTIFICACION    VARCHAR2(500 CHAR),
                      FECHA_INICIO     DATE,
                      FECHA_FIN        DATE,
                      USUARIO_CREACION VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREACION VARCHAR2(255 CHAR) DEFAULT ''LOCAL'' NOT NULL,
                      CONSTRAINT CTR_ROLES_USER_ADMIN_PK PRIMARY KEY (ID_USER_ROL){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES_USER_ADMIN ADD FECHA_INICIO DATE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1430 AND SQLCODE != -904 THEN NULL; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES_USER_ADMIN ADD FECHA_FIN DATE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1430 AND SQLCODE != -904 THEN NULL; END IF; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_BRANDING (
                      ID_BRANDING        NUMBER NOT NULL,
                      SISTEMA            VARCHAR2(50 CHAR) DEFAULT ''GUSOF'' NOT NULL,
                      NOMBRE_SISTEMA     VARCHAR2(255 CHAR) DEFAULT ''OFTIC'' NOT NULL,
                      UNIDAD_SISTEMA_ID  NUMBER,
                      SIGLA              VARCHAR2(100 CHAR),
                      NOMBRE_UNIDAD      VARCHAR2(255 CHAR),
                      LOGO_FILE_NAME     VARCHAR2(255 CHAR),
                      FAVICON_FILE_NAME  VARCHAR2(255 CHAR),
                      FONDO_1_FILE_NAME  VARCHAR2(255 CHAR),
                      FONDO_2_FILE_NAME  VARCHAR2(255 CHAR),
                      FONDO_3_FILE_NAME  VARCHAR2(255 CHAR),
                      VIGENTE            NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREACION   VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      FECHA_CREACION     DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREACION   VARCHAR2(255 CHAR) DEFAULT ''LOCAL'' NOT NULL,
                      USUARIO_MODIFICA   VARCHAR2(100 CHAR),
                      FECHA_MODIFICA     DATE,
                      MAQUINA_MODIFICA   VARCHAR2(255 CHAR),
                      CONSTRAINT CTR_BRANDING_PK PRIMARY KEY (ID_BRANDING){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_DOMINIOS (
                      ID_DOMINIO       NUMBER NOT NULL,
                      DESCRIPCION      VARCHAR2(3000 CHAR),
                      ID_PADRE         NUMBER DEFAULT 0,
                      VIGENTE          NUMBER DEFAULT 1,
                      ABREVIATURA      VARCHAR2(255 CHAR),
                      OBSERVACION      VARCHAR2(255 CHAR),
                      USUARIO_CREACION NUMBER,
                      FECHA_CREACION   DATE DEFAULT SYSDATE,
                      MAQUINA_CREACION VARCHAR2(255 CHAR),
                      USUARIO_MODIFICA NUMBER,
                      FECHA_MODIFICA   DATE,
                      MAQUINA_MODIFICA VARCHAR2(255 CHAR),
                      CONSTRAINT CTR_DOM_A1_PK PRIMARY KEY (ID_DOMINIO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_AUDITORIA (
                      ID_AUDITORIA     NUMBER NOT NULL,
                      FECHA            DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO          VARCHAR2(100 CHAR),
                      MODULO           VARCHAR2(100 CHAR),
                      ACCION           VARCHAR2(100 CHAR),
                      REGISTRO         VARCHAR2(500 CHAR),
                      RESULTADO        VARCHAR2(100 CHAR),
                      MAQUINA          VARCHAR2(100 CHAR),
                      OBSERVACIONES    VARCHAR2(500 CHAR),
                      CONSTRAINT CTR_AUDITORIA_PK PRIMARY KEY (ID_AUDITORIA){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD MODULO VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD ACCION VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD REGISTRO VARCHAR2(500 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD RESULTADO VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD MAQUINA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_AUDITORIA ADD OBSERVACIONES VARCHAR2(500 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES ADD VIGENTE NUMBER(1) DEFAULT 1'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES ADD ESTADO NUMBER(1) DEFAULT 1'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES ADD FECHA_MODIFICA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES ADD USUARIO_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_ROLES ADD MAQUINA_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_CUENTA_EMAIL (
                      ID_CUENTA        NUMBER NOT NULL,
                      ID_DOMINIO_GRUPO NUMBER,
                      NOMBRE_CUENTA    VARCHAR2(200 CHAR) NOT NULL,
                      EMAIL            VARCHAR2(250 CHAR) NOT NULL,
                      SERVIDOR_SMTP    VARCHAR2(200 CHAR) NOT NULL,
                      PUERTO           NUMBER DEFAULT 587 NOT NULL,
                      USUARIO_SMTP     VARCHAR2(200 CHAR) NOT NULL,
                      CLAVE_SMTP       VARCHAR2(500 CHAR),
                      USAR_SSL         NUMBER(1) DEFAULT 1 NOT NULL,
                      VIGENTE          NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREA     VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      FECHA_CREA       DATE DEFAULT SYSDATE NOT NULL,
                      FIRMA_HTML       CLOB,
                      IMAGEN_ENCABEZADO VARCHAR2(500 CHAR),
                      USUARIO_MODIFICA VARCHAR2(100 CHAR),
                      FECHA_MODIFICA   DATE,
                      IP_REGISTRO      VARCHAR2(50 CHAR),
                      PRIORIDAD_ALTA   NUMBER(1) DEFAULT 0,
                      ACUSE_RECIBIDO   NUMBER(1) DEFAULT 0,
                      COPIA_PRIVADA    NUMBER(1) DEFAULT 0,
                      CONSTRAINT CTR_CUENTA_EMAIL_PK PRIMARY KEY (ID_CUENTA){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD FIRMA_HTML CLOB'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD IMAGEN_ENCABEZADO VARCHAR2(500 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD USUARIO_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD FECHA_MODIFICA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD IP_REGISTRO VARCHAR2(50 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD PRIORIDAD_ALTA NUMBER(1) DEFAULT 0'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD ACUSE_RECIBIDO NUMBER(1) DEFAULT 0'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL ADD COPIA_PRIVADA NUMBER(1) DEFAULT 0'; EXCEPTION WHEN OTHERS THEN NULL; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_CUENTA_EMAIL_USUARIO (
                      ID_CUENTA_USUARIO NUMBER NOT NULL,
                      ID_CUENTA         NUMBER NOT NULL,
                      ID_USUARIO        NUMBER NOT NULL,
                      VIGENTE           NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREA      VARCHAR2(100 CHAR) DEFAULT ''SETUP'',
                      FECHA_CREA        DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO_MODIFICA  VARCHAR2(100 CHAR),
                      FECHA_MODIFICA    DATE,
                      IP_REGISTRO       VARCHAR2(50 CHAR),
                      CONSTRAINT CTR_CUENTA_EMAIL_USUARIO_PK PRIMARY KEY (ID_CUENTA_USUARIO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL_USUARIO ADD USUARIO_CREA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL_USUARIO ADD FECHA_CREA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL_USUARIO ADD USUARIO_MODIFICA VARCHAR2(100 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL_USUARIO ADD FECHA_MODIFICA DATE'; EXCEPTION WHEN OTHERS THEN NULL; END;",
                @"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE CTR_CUENTA_EMAIL_USUARIO ADD IP_REGISTRO VARCHAR2(50 CHAR)'; EXCEPTION WHEN OTHERS THEN NULL; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_ENVIO_CORREO (
                      ID_ENVIO         NUMBER NOT NULL,
                      ID_CUENTA_EMAIL  NUMBER,
                      RADICADO         VARCHAR2(100 CHAR),
                      DESTINATARIO     VARCHAR2(500 CHAR) NOT NULL,
                      ASUNTO           VARCHAR2(500 CHAR),
                      CUERPO           CLOB,
                      ID_CLASIFICACION NUMBER,
                      USUARIO_CREA     VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      MAQUINA_CREA     VARCHAR2(100 CHAR) DEFAULT ''LOCAL'' NOT NULL,
                      FECHA_CREA       DATE DEFAULT SYSDATE NOT NULL,
                      CONSTRAINT CTR_ENVIO_CORREO_PK PRIMARY KEY (ID_ENVIO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_CUENTA_EMAIL START WITH 1 INCREMENT BY 1 NOCACHE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN NULL; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_CUENTA_EMAIL_USUARIOS START WITH 1 INCREMENT BY 1 NOCACHE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN NULL; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_ENVIO_CORREO START WITH 1 INCREMENT BY 1 NOCACHE'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN NULL; END IF; END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_LINEA_MANDO (
                      ID_LINEA_MANDO     NUMBER NOT NULL,
                      IDENTIFICACION     VARCHAR2(50 CHAR) NOT NULL,
                      NOMBRE             VARCHAR2(100 CHAR),
                      APELLIDOS          VARCHAR2(100 CHAR),
                      GRADO              VARCHAR2(50 CHAR),
                      CARGO              VARCHAR2(100 CHAR),
                      UNIDAD             VARCHAR2(200 CHAR),
                      FOTO_BASE64        CLOB,
                      ORDEN              NUMBER DEFAULT 1 NOT NULL,
                      VIGENTE            NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREA       VARCHAR2(100 CHAR) DEFAULT ''SETUP'',
                      FECHA_CREA         DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREA       VARCHAR2(100 CHAR) DEFAULT ''LOCAL'',
                      USUARIO_MOD        VARCHAR2(100 CHAR),
                      FECHA_MOD          DATE,
                      MAQUINA_MOD        VARCHAR2(100 CHAR),
                      PESO               VARCHAR2(100 CHAR),
                      CONSTRAINT CTR_LINEA_MANDO_PK PRIMARY KEY (ID_LINEA_MANDO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_PERSONAJE_GRUPO (
                      ID_PERSONAJE_GRUPO NUMBER NOT NULL,
                      NOMBRE_GRUPO       VARCHAR2(100 CHAR),
                      NUMERO_ACTA        VARCHAR2(50 CHAR),
                      MES                NUMBER,
                      VIGENTE            NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREA       VARCHAR2(100 CHAR) DEFAULT ''SETUP'',
                      FECHA_CREA         DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREA       VARCHAR2(100 CHAR) DEFAULT ''LOCAL'',
                      USUARIO_MOD        VARCHAR2(100 CHAR),
                      FECHA_MOD          DATE,
                      MAQUINA_MOD        VARCHAR2(100 CHAR),
                      FOTO_GRUPO         VARCHAR2(200 CHAR),
                      ANIO               NUMBER,
                      CONSTRAINT CTR_PERSONAJE_GRUPO_PK PRIMARY KEY (ID_PERSONAJE_GRUPO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_PERSONAJE_MES (
                      ID_PERSONAJE_MES   NUMBER NOT NULL,
                      IDENTIFICACION     VARCHAR2(50 CHAR),
                      NOMBRES            VARCHAR2(100 CHAR),
                      APELLIDOS          VARCHAR2(100 CHAR),
                      GRADO              VARCHAR2(50 CHAR),
                      CARGO              VARCHAR2(100 CHAR),
                      UNIDAD             VARCHAR2(200 CHAR),
                      ID_CATEGORIA       NUMBER,
                      NUMERO_ACTA        VARCHAR2(50 CHAR),
                      VIGENTE            NUMBER(1) DEFAULT 1 NOT NULL,
                      USUARIO_CREA       VARCHAR2(100 CHAR) DEFAULT ''SETUP'',
                      FECHA_CREA         DATE DEFAULT SYSDATE NOT NULL,
                      MAQUINA_CREA       VARCHAR2(100 CHAR) DEFAULT ''LOCAL'',
                      USUARIO_MOD        VARCHAR2(100 CHAR),
                      FECHA_MOD          DATE,
                      MAQUINA_MOD        VARCHAR2(100 CHAR),
                      FOTO_MODIFICADA    VARCHAR2(255 CHAR),
                      MES                NUMBER,
                      ANIO               NUMBER,
                      ID_PERSONAJE_GRUPO NUMBER,
                      CONSTRAINT CTR_PERSONAJE_MES_PK PRIMARY KEY (ID_PERSONAJE_MES){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                $@"BEGIN
                    EXECUTE IMMEDIATE '
                    CREATE TABLE CTR_VIDEOS (
                      ID_VIDEO         NUMBER NOT NULL,
                      DESCRIPCION      VARCHAR2(255 CHAR) NOT NULL,
                      OBSERVACIONES    VARCHAR2(500 CHAR),
                      FECHA_CREACION   DATE DEFAULT SYSDATE NOT NULL,
                      USUARIO_CREACION VARCHAR2(100 CHAR) DEFAULT ''SETUP'' NOT NULL,
                      RUTA_VIDEO       VARCHAR2(500 CHAR),
                      VIGENTE          NUMBER(1) DEFAULT 1 NOT NULL,
                      FECHA_MODIFICA   DATE,
                      USUARIO_MODIFICA VARCHAR2(100 CHAR),
                      MAQUINA_MODIFICA VARCHAR2(100 CHAR),
                      CONSTRAINT CTR_VIDEOS_PK PRIMARY KEY (ID_VIDEO){usingIndexClause}
                    ){tsDataClause}';
                  EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;
                  END;",

                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_USUARIOS START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_ROLES START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_MENU START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_MENU_ROLES START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_ROLES_USER_ADMIN START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_BRANDING START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_DOMINIO START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_AUDITORIA START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_CUENTA_EMAIL START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_LINEA_MANDO START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_PERSONAJE_GRUPO START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_PERSONAJE_MES START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_VIDEOS START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_CUENTA_EMAIL_USUARIOS START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",
                @"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQ_CTR_ENVIO_CORREO START WITH 1 INCREMENT BY 1'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                @"BEGIN
                    MERGE INTO CTR_ROLES r USING (SELECT 1 AS id FROM DUAL) src ON (r.ID_ROL = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_ROL, CODIGO_ROL, NOMBRE_ROL, DESCRIPCION, ESTADO, FECHA_CREACION, USUARIO_CREACION)
                    VALUES (1, 'DEV', 'Desarrollador', 'Acceso total y configuración del sistema', 1, SYSDATE, 'SETUP');
                  END;",

                @"BEGIN
                    MERGE INTO CTR_ROLES r USING (SELECT 2 AS id FROM DUAL) src ON (r.ID_ROL = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_ROL, CODIGO_ROL, NOMBRE_ROL, DESCRIPCION, ESTADO, FECHA_CREACION, USUARIO_CREACION)
                    VALUES (2, 'ADMIN', 'Administrador', 'Gestión administrativa del sistema', 1, SYSDATE, 'SETUP');
                  END;",

                @"BEGIN
                    DELETE FROM CTR_MENU_ROLES WHERE ID_MENU = 2;
                    DELETE FROM CTR_MENU WHERE ID_MENU = 2 OR RUTA = '/noticias';
                  END;",

                @"BEGIN
                    MERGE INTO CTR_MENU m USING (SELECT 1 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 3 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (3, 0, 'Gestión Documental', NULL, 'fa-solid fa-folder-open', 3, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 31 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (31, 3, 'Gestión Correos Electrónicos', '/gestion-documental/gestion-correos-electronicos', 'fa-solid fa-envelope', 1, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 4 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (4, 0, 'Administración', NULL, 'fa-solid fa-gears', 4, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 401 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (401, 4, 'Inicio Administración', '/administracion/inicio', 'fa-solid fa-sliders', 1, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 402 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (402, 4, 'Usuarios', '/administracion/usuarios', 'fa-solid fa-users-gear', 2, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 403 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (403, 4, 'Roles', '/administracion/roles', 'fa-solid fa-user-shield', 3, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 404 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (404, 4, 'Menú', '/administracion/menu', 'fa-solid fa-sitemap', 4, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 405 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (405, 4, 'Configuración Sistema', '/administracion/configuracion-sistema', 'fa-solid fa-wrench', 5, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 406 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (406, 4, 'Línea de Mando', '/administracion/linea-mando', 'fa-solid fa-user-tie', 6, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 407 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (407, 4, 'Personaje del Mes', '/administracion/personaje-mes', 'fa-solid fa-award', 7, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 408 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (408, 4, 'Dominios', '/administracion/dominio', 'fa-solid fa-list-check', 8, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 409 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (409, 4, 'Auditoría', '/administracion/auditoria', 'fa-solid fa-clock-rotate-left', 9, 1, 'SETUP');

                    MERGE INTO CTR_MENU m USING (SELECT 410 AS id FROM DUAL) src ON (m.ID_MENU = src.id)
                    WHEN NOT MATCHED THEN INSERT (ID_MENU, ID_PADRE, TITULO, RUTA, ICONO, ORDEN, ESTADO, USUARIO_CREACION)
                    VALUES (410, 4, 'Cuentas Email', '/administracion/cuentas-email', 'fa-solid fa-at', 10, 1, 'SETUP');
                  END;",

                @"BEGIN
                    FOR r IN 1..2 LOOP
                        FOR m IN (SELECT id_menu FROM CTR_MENU) LOOP
                            MERGE INTO CTR_MENU_ROLES mr 
                            USING (SELECT m.id_menu AS id_m, r AS id_r FROM DUAL) src 
                            ON (mr.ID_MENU = src.id_m AND mr.ID_ROL = src.id_r)
                            WHEN NOT MATCHED THEN INSERT (ID_MENU_ROL, ID_MENU, ID_ROL, ESTADO, FECHA_CREACION, USUARIO_CREACION)
                            VALUES (SEQ_CTR_MENU_ROLES.NEXTVAL, src.id_m, src.id_r, 1, SYSDATE, 'SETUP');
                        END LOOP;
                    END LOOP;
                  END;",

                @"BEGIN
                     -- 1. RAIZ
                     MERGE INTO CTR_DOMINIOS d USING (SELECT 1 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (1, 'RAIZ', 0, 1, 'RAIZ', 'RAIZ Principal', 1143458803, SYSDATE, '::1');

                     -- 2. Padre Personaje del mes
                     MERGE INTO CTR_DOMINIOS d USING (SELECT 2 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (2, 'Personaje del mes', 1, 1, 'PERS_MES', 'Categorías y opciones de Personaje del mes', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 3 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (3, 'Categoría oficiales', 2, 1, 'PEROF', 'Personajes del mes en la categoría de oficial', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 4 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (4, 'Categoría Mando de nivel ejecutivo', 2, 1, 'CATNIVEL', 'Personajes del mes en la categoría de mando de nivel ejecutivo', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 5 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (5, 'Categoría patrulleros', 2, 1, 'CATPT', 'Personajes del mes en la categoría de patrulleros', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 6 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (6, 'Personaje de mes ámbito operativo', 2, 1, 'PERAMOP', 'Personaje de mes ámbito operativo', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 7 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (7, 'Categoría no uniformados', 2, 1, 'CATNU', 'Categoría no uniformados', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 8 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (8, 'Equipo o grupo de alto rendimiento', 2, 1, 'EQUGP', 'Equipo o grupo de alto rendimiento', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 9 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (9, 'Categoría estudiantes', 2, 1, 'CATEST', 'Categoría estudiantes', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 10 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (10, 'Categoría suboficial', 2, 1, 'CATSUBOF', 'Categoría suboficial', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 11 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (11, 'Categoria agentes', 2, 1, 'CATAG', 'Categoria para agrupar agentes de policía', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 12 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (12, 'Categoría Auxiliares de Policia', 2, 1, 'CAUXPO', 'Categoria para Auxiliares De Policia', 1143458803, SYSDATE, '::1');

                     -- 13. Padre Privacidad de Documentos
                     MERGE INTO CTR_DOMINIOS d USING (SELECT 13 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (13, 'Privacidad de Documentos', 1, 1, 'PRIV_DOC', 'Privacidad de Documentos', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 14 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (14, 'INFORMACIÓN PÚBLICA', 13, 1, 'INFPUB', 'Toda información que un sujeto obligado genere, obtenga o controle', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 15 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (15, 'INFORMACIÓN PÚBLICA CLASIFICADA', 13, 1, 'INFPUBCLA', 'Es aquella información que estando en poder o custodia de un sujeto obligado es clasificada', 1143458803, SYSDATE, '::1');

                     MERGE INTO CTR_DOMINIOS d USING (SELECT 16 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (16, 'INFORMACIÓN PÚBLICA RESERVADA', 13, 1, 'PUBRES', 'Es aquella información que estando en poder o custodia de un sujeto obligado es reservada', 1143458803, SYSDATE, '::1');

                     -- 17. Padre Grupos o Áreas
                     MERGE INTO CTR_DOMINIOS d USING (SELECT 17 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (17, 'GRUPOS O AREAS', 1, 1, 'GRUPOS_AREAS', 'Dominio Padre Semilla para Grupos o Áreas responsables de cuentas de correo', 1143458803, SYSDATE, '::1');

                     -- 18. Hijo Ejemplo Grupo Desarrollo de Software
                     MERGE INTO CTR_DOMINIOS d USING (SELECT 18 AS id FROM DUAL) src ON (d.ID_DOMINIO = src.id)
                     WHEN NOT MATCHED THEN INSERT (ID_DOMINIO, DESCRIPCION, ID_PADRE, VIGENTE, ABREVIATURA, OBSERVACION, USUARIO_CREACION, FECHA_CREACION, MAQUINA_CREACION)
                     VALUES (18, 'Grupo Desarrollo de Software', 17, 1, 'GDS', 'Grupo Desarrollo de Software ejemplo responsable de correo', 1143458803, SYSDATE, '::1');
                   END;"
            };

            var result = new DtoMigrationResult();

            foreach (var block in blocks)
            {
                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = block;
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                catch (Oracle.ManagedDataAccess.Client.OracleException oraEx)
                {
                    // ORA-00955: name is already used by an existing object
                    // ORA-01408: such column list already indexed
                    // ORA-01430: column being added already exists in table
                    // ORA-00904: invalid identifier (used when column already exists in alter)
                    if (oraEx.Number == 955 || oraEx.Number == 1408 || oraEx.Number == 1430 || oraEx.Number == 904)
                    {
                        continue;
                    }

                    // Reintento sin tablespace especifico si es problema de cuota (ORA-01950) o inexistencia de tablespace (ORA-00959)
                    if ((oraEx.Number == 1950 || oraEx.Number == 959) && (block.Contains("TABLESPACE") || block.Contains("USING INDEX")))
                    {
                        try
                        {
                            string fallbackBlock = block.Replace(tsDataClause, "").Replace(tsIndClause, "").Replace(usingIndexClause, "");
                            await using var cmdFb = conn.CreateCommand();
                            cmdFb.CommandText = fallbackBlock;
                            await cmdFb.ExecuteNonQueryAsync(ct);
                            logger.LogInformation("Bloque Oracle ejecutado con éxito en tablespace por defecto tras aviso ORA-{Code}", oraEx.Number);
                            continue;
                        }
                        catch (Exception exFb)
                        {
                            string errFallback = $"Error Oracle (ORA-{oraEx.Number}): {oraEx.Message}. Reintento en tablespace por defecto: {exFb.Message}";
                            logger.LogWarning(errFallback);
                            result.Errors.Add(errFallback);
                        }
                    }
                    else
                    {
                        string errStr = $"Error Oracle (ORA-{oraEx.Number}): {oraEx.Message}";
                        logger.LogWarning(oraEx, errStr);
                        result.Errors.Add(errStr);
                    }
                }
                catch (Exception exBlock)
                {
                    string errStr = $"Error al ejecutar bloque DDL Oracle: {exBlock.Message}";
                    logger.LogWarning(exBlock, errStr);
                    result.Errors.Add(errStr);
                }
            }

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                result.Message = $"Se presentaron {result.Errors.Count} error(es) en el esquema de Oracle durante la migración: {string.Join(" | ", result.Errors)}";
            }
            else
            {
                result.Success = true;
                result.Message = "Migración automática de esquema Oracle completada exitosamente.";
            }

            return result;
        }
    }
}
