using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Datos.Gestion
{
    public static class DatabaseMigration
    {
        public static async Task RunAsync(IConfiguration cfg, ILogger logger, CancellationToken ct = default)
        {
            var connectionString = Comun.Extensions.ConfigurationExtensions.GetDecryptedConnectionString(cfg);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("No se pudo obtener la cadena de conexión para ejecutar la migración de base de datos.");
                return;
            }

            var provider = (cfg["DatabaseProvider"] ?? "Oracle").Trim();

            try
            {
                if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    await MigrateMySqlAsync(connectionString, logger, ct);
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                {
                    await MigratePostgreSqlAsync(connectionString, logger, ct);
                }
                else
                {
                    logger.LogInformation("DatabaseProvider es Oracle. La migración automática se omite o se maneja vía scripts Oracle.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ejecutando la migración automática de base de datos ({Provider})", provider);
            }
        }

        private static async Task MigrateMySqlAsync(string cs, ILogger logger, CancellationToken ct)
        {
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
                    fecha_creacion DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL
                );",

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
                    dominio VARCHAR(100) NOT NULL,
                    valor VARCHAR(255) NOT NULL,
                    descripcion VARCHAR(500),
                    orden INT DEFAULT 1,
                    vigente INT DEFAULT 1 NOT NULL
                );",

                // 8. CTR_AUDITORIA
                @"CREATE TABLE IF NOT EXISTS CTR_AUDITORIA (
                    id_auditoria BIGINT AUTO_INCREMENT PRIMARY KEY,
                    evento VARCHAR(100) NOT NULL,
                    descripcion TEXT,
                    identificador BIGINT,
                    usuario BIGINT,
                    maquina VARCHAR(255),
                    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
                );",

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
                    sistema VARCHAR(10) NOT NULL,
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
                @"INSERT IGNORE INTO CTR_MENU (id_menu, id_padre, titulo, ruta, icono, orden, estado, usuario_creacion) VALUES
                  (1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1, 'SETUP'),
                  (2, 0, 'Noticias', '/noticias', 'fa-solid fa-newspaper', 2, 1, 'SETUP'),
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
                  (26, 406, 2, 1), (27, 407, 2, 1), (28, 408, 2, 1), (29, 409, 2, 1), (30, 410, 2, 1);"
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
                }
            }

            logger.LogInformation("Migración de base de datos MySQL completada exitosamente.");
        }

        private static async Task MigratePostgreSqlAsync(string cs, ILogger logger, CancellationToken ct)
        {
            await using var conn = new Npgsql.NpgsqlConnection(cs);
            await conn.OpenAsync(ct);

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
                    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                    usuario_creacion VARCHAR(100) DEFAULT 'SETUP' NOT NULL,
                    maquina_creacion VARCHAR(100) DEFAULT 'LOCAL',
                    fecha_modifica TIMESTAMP,
                    usuario_modifica VARCHAR(100),
                    maquina_modifica VARCHAR(100)
                );",

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

                // Seed Data - Roles
                @"INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion, estado, usuario_creacion, maquina_creacion)
                  VALUES (1, 'DESARROLLADOR', 'Desarrollador', 'Rol con acceso total de desarrollo y plantilla', 1, 'SETUP', 'LOCAL')
                  ON CONFLICT (id_rol) DO NOTHING;",

                @"INSERT INTO ctr_roles (id_rol, codigo_rol, nombre_rol, descripcion, estado, usuario_creacion, maquina_creacion)
                  VALUES (2, 'ADMINISTRADOR', 'Administrador', 'Rol administrador del sistema', 1, 'SETUP', 'LOCAL')
                  ON CONFLICT (id_rol) DO NOTHING;",

                // Seed Data - Menús Base
                @"INSERT INTO ctr_menu (id_menu, id_padre, titulo, ruta, icono, orden, estado, usuario_creacion) VALUES
                  (1, 0, 'Inicio', '/home', 'fa-solid fa-house', 1, 1, 'SETUP'),
                  (2, 0, 'Noticias', '/noticias', 'fa-solid fa-newspaper', 2, 1, 'SETUP'),
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
                  ON CONFLICT (id_menu_rol) DO NOTHING;"
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
                }
            }

            logger.LogInformation("Migración de base de datos PostgreSQL completada exitosamente.");
        }
    }
}
