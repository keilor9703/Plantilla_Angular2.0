-- =========================================
-- Tabla: CTR_SLIDERS
-- Administración de sliders/banners
-- Paquete: PK_ADMINISTRACION
-- =========================================

-- =========================================
-- CREATE TABLE (solo si no existe)
-- =========================================
-- CREATE TABLE CTR_SLIDERS (
--     ID_SLIDER          NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
--     TITULO             VARCHAR2(120)   NOT NULL,
--     SUBTITULO          VARCHAR2(250),
--     URL_IMAGEN         VARCHAR2(500)   NOT NULL,
--     URL_DESTINO        VARCHAR2(500),
--     ORDEN              NUMBER(3)       DEFAULT 1,
--     VIGENTE            NUMBER(1)       DEFAULT 1,
--     FECHA_INICIO       DATE,
--     FECHA_FIN          DATE,
--     FECHA_CREACION     DATE            DEFAULT SYSDATE,
--     USUARIO_CREACION   VARCHAR2(100)   NOT NULL,
--     MAQUINA_CREACION   VARCHAR2(100),
--     FECHA_MODIFICA     DATE,
--     USUARIO_MODIFICA   VARCHAR2(100),
--     MAQUINA_MODIFICA   VARCHAR2(100)
-- );
-- CREATE INDEX IDX_SLIDERS_VIGENTE ON CTR_SLIDERS(VIGENTE);
-- CREATE INDEX IDX_SLIDERS_ORDEN   ON CTR_SLIDERS(ORDEN);

-- =========================================
-- P_INS_UPD_SLIDER  (INSERT o UPDATE)
-- =========================================
CREATE OR REPLACE PROCEDURE PK_ADMINISTRACION.P_INS_UPD_SLIDER(
    P_IdSlider    IN  NUMBER,
    P_Titulo      IN  VARCHAR2,
    P_Subtitulo   IN  VARCHAR2,
    P_UrlImagen   IN  VARCHAR2,
    P_UrlDestino  IN  VARCHAR2,
    P_Orden       IN  NUMBER,
    P_Vigente     IN  NUMBER,
    P_FechaInicio IN  DATE,
    P_FechaFin    IN  DATE,
    P_Usuario     IN  VARCHAR2,
    P_Maquina     IN  VARCHAR2,
    SRV_Message   OUT VARCHAR2,
    P_Resultado   OUT NUMBER
)
IS
BEGIN
    IF P_IdSlider IS NULL OR P_IdSlider <= 0 THEN
        -- INSERT
        INSERT INTO CTR_SLIDERS (
            TITULO, SUBTITULO, URL_IMAGEN, URL_DESTINO,
            ORDEN, VIGENTE, FECHA_INICIO, FECHA_FIN,
            FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION
        ) VALUES (
            P_Titulo, P_Subtitulo, P_UrlImagen, P_UrlDestino,
            NVL(P_Orden, 1), NVL(P_Vigente, 1), P_FechaInicio, P_FechaFin,
            SYSDATE, P_Usuario, P_Maquina
        )
        RETURNING ID_SLIDER INTO P_Resultado;

        SRV_Message := 'Slider creado correctamente.';
    ELSE
        -- UPDATE
        UPDATE CTR_SLIDERS
           SET TITULO            = P_Titulo,
               SUBTITULO         = P_Subtitulo,
               URL_IMAGEN        = P_UrlImagen,
               URL_DESTINO       = P_UrlDestino,
               ORDEN             = NVL(P_Orden, ORDEN),
               VIGENTE           = NVL(P_Vigente, VIGENTE),
               FECHA_INICIO      = P_FechaInicio,
               FECHA_FIN         = P_FechaFin,
               FECHA_MODIFICA    = SYSDATE,
               USUARIO_MODIFICA  = P_Usuario,
               MAQUINA_MODIFICA  = P_Maquina
         WHERE ID_SLIDER = P_IdSlider;

        IF SQL%ROWCOUNT > 0 THEN
            P_Resultado := P_IdSlider;
            SRV_Message := 'Slider actualizado correctamente.';
        ELSE
            P_Resultado := -1;
            SRV_Message := 'No se encontró el slider con id ' || P_IdSlider;
        END IF;
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        P_Resultado := -1;
        SRV_Message := 'Error al guardar slider: ' || SQLERRM;
END P_INS_UPD_SLIDER;
/

-- =========================================
-- P_SET_ESTADO_SLIDER  (Activar / Inactivar)
-- =========================================
CREATE OR REPLACE PROCEDURE PK_ADMINISTRACION.P_SET_ESTADO_SLIDER(
    P_IdSlider    IN  NUMBER,
    P_Vigente     IN  NUMBER,
    P_Usuario     IN  VARCHAR2,
    P_Maquina     IN  VARCHAR2,
    SRV_Message   OUT VARCHAR2,
    P_Resultado   OUT NUMBER
)
IS
BEGIN
    UPDATE CTR_SLIDERS
       SET VIGENTE           = P_Vigente,
           USUARIO_MODIFICA  = P_Usuario,
           FECHA_MODIFICA    = SYSDATE,
           MAQUINA_MODIFICA  = P_Maquina
     WHERE ID_SLIDER = P_IdSlider;

    P_Resultado := P_IdSlider;
    SRV_Message := 'Estado actualizado';

    -- Auditoría (envuelto en su propio bloque para que un fallo no revierta el UPDATE)
    BEGIN
        INSERT INTO ctr_auditoria (
            evento, descripcion, anio,
            identificador, usuario_creacion, fecha_creacion, maquina_creacion
        ) VALUES (
            'PK_ADMINISTRACION.P_SET_ESTADO_SLIDER',
            SRV_Message,
            EXTRACT(YEAR FROM SYSDATE),
            'ID_SLIDER=' || P_IdSlider,
            P_Usuario,
            SYSDATE,
            P_Maquina
        );
    EXCEPTION
        WHEN OTHERS THEN NULL;  -- No bloquear la operación por fallo de auditoría
    END;

    COMMIT;

EXCEPTION
    WHEN OTHERS THEN
        P_Resultado := -1;
        SRV_Message := 'Error al actualizar estado del slider: ' || SQLERRM;
        ROLLBACK;
END P_SET_ESTADO_SLIDER;
/

-- =========================================
-- Permisos (ajustar según el usuario de app)
-- =========================================
-- GRANT EXECUTE ON PK_ADMINISTRACION.P_INS_UPD_SLIDER    TO USR_DEV_TEST;
-- GRANT EXECUTE ON PK_ADMINISTRACION.P_SET_ESTADO_SLIDER  TO USR_DEV_TEST;
