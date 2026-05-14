-- ============================================================
-- Migración: imagen de encabezado por cuenta de correo
-- Tabla: USR_COEST.CTR_CUENTA_EMAIL
-- ============================================================

-- 1. Nueva columna en la tabla
ALTER TABLE USR_COEST.CTR_CUENTA_EMAIL
  ADD IMAGEN_ENCABEZADO VARCHAR2(500);

-- ============================================================
-- 2. Actualizar PR_CONSULTAR_CUENTAS_EMAIL
--    Agrega IMAGEN_ENCABEZADO al cursor de salida
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
--    Agrega P_IMAGEN_ENCABEZADO al INSERT/UPDATE
-- ============================================================
CREATE OR REPLACE PROCEDURE USR_COEST.PK_ADMINISTRACION.PR_GESTIONAR_CUENTA_EMAIL(
    P_ID_CUENTA       IN  NUMBER,
    P_ID_DOMINIO_GRUPO IN NUMBER,
    P_NOMBRE_CUENTA   IN  VARCHAR2,
    P_EMAIL           IN  VARCHAR2,
    P_SERVIDOR_SMTP   IN  VARCHAR2,
    P_PUERTO          IN  NUMBER,
    P_USUARIO_SMTP    IN  VARCHAR2,
    P_CLAVE_SMTP      IN  VARCHAR2,
    P_USAR_SSL        IN  NUMBER,
    P_VIGENTE         IN  NUMBER,
    P_USUARIO_AUD     IN  VARCHAR2,
    P_IP_AUD          IN  VARCHAR2,
    P_FIRMA_HTML      IN  CLOB,
    P_IMAGEN_ENCABEZADO IN VARCHAR2,
    P_ID_OUT          OUT NUMBER
) AS
BEGIN
    IF P_ID_CUENTA = 0 THEN
        -- INSERT
        INSERT INTO USR_COEST.CTR_CUENTA_EMAIL (
            ID_DOMINIO_GRUPO, NOMBRE_CUENTA, EMAIL,
            SERVIDOR_SMTP, PUERTO, USUARIO_SMTP, CLAVE_SMTP,
            USAR_SSL, VIGENTE, USUARIO_CREA, FECHA_CREA, IP_CREA,
            FIRMA_HTML, IMAGEN_ENCABEZADO
        ) VALUES (
            P_ID_DOMINIO_GRUPO, P_NOMBRE_CUENTA, P_EMAIL,
            P_SERVIDOR_SMTP, P_PUERTO, P_USUARIO_SMTP, P_CLAVE_SMTP,
            P_USAR_SSL, P_VIGENTE, P_USUARIO_AUD, SYSDATE, P_IP_AUD,
            P_FIRMA_HTML, P_IMAGEN_ENCABEZADO
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
            IMAGEN_ENCABEZADO  = P_IMAGEN_ENCABEZADO
        WHERE ID_CUENTA = P_ID_CUENTA;
        P_ID_OUT := P_ID_CUENTA;
    END IF;
    COMMIT;
END PR_GESTIONAR_CUENTA_EMAIL;
/
