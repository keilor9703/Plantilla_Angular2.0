-- ============================================================
-- Migración: opciones de envío por cuenta de correo
-- Tabla: USR_COEST.CTR_CUENTA_EMAIL
-- Nuevas columnas:
--   PRIORIDAD_ALTA  NUMBER(1) DEFAULT 0  → marcar correo como importancia alta
--   ACUSE_RECIBIDO  NUMBER(1) DEFAULT 0  → solicitar acuse de recibido al destinatario
--   COPIA_PRIVADA   NUMBER(1) DEFAULT 0  → ocultar destinatarios entre sí (BCC)
-- ============================================================

-- 1. Nuevas columnas en la tabla
ALTER TABLE USR_COEST.CTR_CUENTA_EMAIL
  ADD PRIORIDAD_ALTA NUMBER(1) DEFAULT 0 NOT NULL;

ALTER TABLE USR_COEST.CTR_CUENTA_EMAIL
  ADD ACUSE_RECIBIDO NUMBER(1) DEFAULT 0 NOT NULL;

ALTER TABLE USR_COEST.CTR_CUENTA_EMAIL
  ADD COPIA_PRIVADA NUMBER(1) DEFAULT 0 NOT NULL;

-- ============================================================
-- 2. Actualizar PR_CONSULTAR_CUENTAS_EMAIL
--    Agrega las tres nuevas columnas al cursor de salida
-- ============================================================
CREATE OR REPLACE PROCEDURE USR_COEST.PK_ADMINISTRACION.PR_CONSULTAR_CUENTAS_EMAIL(
    P_ID_CUENTA  IN  NUMBER,
    P_RESULTADO  OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN P_RESULTADO FOR
        SELECT
            C.ID_CUENTA,
            C.ID_DOMINIO_GRUPO,
            C.NOMBRE_CUENTA,
            C.EMAIL,
            C.SERVIDOR_SMTP,
            C.PUERTO,
            C.USUARIO_SMTP,
            C.CLAVE_SMTP,
            C.USAR_SSL,
            C.VIGENTE,
            C.FIRMA_HTML,
            C.IMAGEN_ENCABEZADO,
            C.PRIORIDAD_ALTA,
            C.ACUSE_RECIBIDO,
            C.COPIA_PRIVADA,
            C.USUARIO_CREA,
            C.FECHA_CREA,
            D.DESCRIPCION AS NOMBRE_GRUPO
        FROM USR_COEST.CTR_CUENTA_EMAIL C
        LEFT JOIN USR_COEST.CTR_DOMINIO D
            ON D.ID_DOMINIO = C.ID_DOMINIO_GRUPO
        WHERE (P_ID_CUENTA = 0 OR C.ID_CUENTA = P_ID_CUENTA)
        ORDER BY C.NOMBRE_CUENTA;
END PR_CONSULTAR_CUENTAS_EMAIL;
/

-- ============================================================
-- 3. Actualizar PR_GESTIONAR_CUENTA_EMAIL
--    Agrega P_PRIORIDAD_ALTA, P_ACUSE_RECIBIDO, P_COPIA_PRIVADA
-- ============================================================
CREATE OR REPLACE PROCEDURE USR_COEST.PK_ADMINISTRACION.PR_GESTIONAR_CUENTA_EMAIL(
    P_ID_CUENTA         IN  NUMBER,
    P_ID_DOMINIO_GRUPO  IN  NUMBER,
    P_NOMBRE_CUENTA     IN  VARCHAR2,
    P_EMAIL             IN  VARCHAR2,
    P_SERVIDOR_SMTP     IN  VARCHAR2,
    P_PUERTO            IN  NUMBER,
    P_USUARIO_SMTP      IN  VARCHAR2,
    P_CLAVE_SMTP        IN  VARCHAR2,
    P_USAR_SSL          IN  NUMBER,
    P_VIGENTE           IN  NUMBER,
    P_USUARIO_AUD       IN  VARCHAR2,
    P_IP_AUD            IN  VARCHAR2,
    P_FIRMA_HTML        IN  CLOB,
    P_IMAGEN_ENCABEZADO IN  VARCHAR2,
    P_PRIORIDAD_ALTA    IN  NUMBER,
    P_ACUSE_RECIBIDO    IN  NUMBER,
    P_COPIA_PRIVADA     IN  NUMBER,
    P_ID_OUT            OUT NUMBER
) AS
BEGIN
    IF P_ID_CUENTA = 0 THEN
        -- INSERT
        INSERT INTO USR_COEST.CTR_CUENTA_EMAIL (
            ID_DOMINIO_GRUPO, NOMBRE_CUENTA, EMAIL,
            SERVIDOR_SMTP, PUERTO, USUARIO_SMTP, CLAVE_SMTP,
            USAR_SSL, VIGENTE, USUARIO_CREA, FECHA_CREA, IP_CREA,
            FIRMA_HTML, IMAGEN_ENCABEZADO,
            PRIORIDAD_ALTA, ACUSE_RECIBIDO, COPIA_PRIVADA
        ) VALUES (
            P_ID_DOMINIO_GRUPO, P_NOMBRE_CUENTA, P_EMAIL,
            P_SERVIDOR_SMTP, P_PUERTO, P_USUARIO_SMTP, P_CLAVE_SMTP,
            P_USAR_SSL, P_VIGENTE, P_USUARIO_AUD, SYSDATE, P_IP_AUD,
            P_FIRMA_HTML, P_IMAGEN_ENCABEZADO,
            P_PRIORIDAD_ALTA, P_ACUSE_RECIBIDO, P_COPIA_PRIVADA
        )
        RETURNING ID_CUENTA INTO P_ID_OUT;
    ELSE
        -- UPDATE
        UPDATE USR_COEST.CTR_CUENTA_EMAIL SET
            ID_DOMINIO_GRUPO   = P_ID_DOMINIO_GRUPO,
            NOMBRE_CUENTA      = P_NOMBRE_CUENTA,
            EMAIL              = P_EMAIL,
            SERVIDOR_SMTP      = P_SERVIDOR_SMTP,
            PUERTO             = P_PUERTO,
            USUARIO_SMTP       = P_USUARIO_SMTP,
            CLAVE_SMTP         = P_CLAVE_SMTP,
            USAR_SSL           = P_USAR_SSL,
            VIGENTE            = P_VIGENTE,
            USUARIO_MODIFICA   = P_USUARIO_AUD,
            FECHA_MODIFICA     = SYSDATE,
            FIRMA_HTML         = P_FIRMA_HTML,
            IMAGEN_ENCABEZADO  = P_IMAGEN_ENCABEZADO,
            PRIORIDAD_ALTA     = P_PRIORIDAD_ALTA,
            ACUSE_RECIBIDO     = P_ACUSE_RECIBIDO,
            COPIA_PRIVADA      = P_COPIA_PRIVADA
        WHERE ID_CUENTA = P_ID_CUENTA;
        P_ID_OUT := P_ID_CUENTA;
    END IF;
    COMMIT;
END PR_GESTIONAR_CUENTA_EMAIL;
/
