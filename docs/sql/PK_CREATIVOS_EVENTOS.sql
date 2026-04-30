-- PK_CREATIVOS - CRUD de eventos institucionales
-- Oracle 12c+

-- =========================================================
-- 1) OBJETOS BASE
-- =========================================================
CREATE TABLE ADM_EVENTO_INSTITUCIONAL (
    ID_EVENTO            NUMBER(19)       NOT NULL,
    UNIDAD               VARCHAR2(100)    NOT NULL,
    TITULO               VARCHAR2(300)    NOT NULL,
    SUBTITULO            VARCHAR2(500),
    DESCRIPCION          CLOB,
    LUGAR                VARCHAR2(200),
    CIUDAD               VARCHAR2(100),
    FECHA_INICIO         DATE             NOT NULL,
    FECHA_FIN            DATE             NOT NULL,
    HORA_INICIO          VARCHAR2(5),
    HORA_FIN             VARCHAR2(5),
    IMAGEN_PORTADA       VARCHAR2(500),
    ENLACE_EVENTO        VARCHAR2(500),
    TIPO_EVENTO          VARCHAR2(50),
    ORDEN                NUMBER(10)       DEFAULT 1 NOT NULL,
    VIGENTE              NUMBER(1)        DEFAULT 1 NOT NULL,
    FECHA_CREACION       DATE             DEFAULT SYSDATE NOT NULL,
    USUARIO_CREACION     VARCHAR2(100)    NOT NULL,
    MAQUINA_CREACION     VARCHAR2(100),
    FECHA_MODIFICA       DATE,
    USUARIO_MODIFICA     VARCHAR2(100),
    MAQUINA_MODIFICA     VARCHAR2(100),

    CONSTRAINT PK_ADM_EVENTO_INSTITUCIONAL PRIMARY KEY (ID_EVENTO),
    CONSTRAINT CK_ADM_EVENTO_VIGENTE CHECK (VIGENTE IN (0,1)),
    CONSTRAINT CK_ADM_EVENTO_FECHAS CHECK (FECHA_FIN >= FECHA_INICIO)
);

CREATE SEQUENCE SQ_ADM_EVENTO_INSTITUCIONAL
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE OR REPLACE TRIGGER TRG_BI_ADM_EVENTO_INST
BEFORE INSERT ON ADM_EVENTO_INSTITUCIONAL
FOR EACH ROW
BEGIN
    IF :NEW.ID_EVENTO IS NULL THEN
        :NEW.ID_EVENTO := SQ_ADM_EVENTO_INSTITUCIONAL.NEXTVAL;
    END IF;

    IF :NEW.FECHA_CREACION IS NULL THEN
        :NEW.FECHA_CREACION := SYSDATE;
    END IF;
END;
/

CREATE INDEX IX_ADM_EVENTO_VIGENTE ON ADM_EVENTO_INSTITUCIONAL (VIGENTE);
CREATE INDEX IX_ADM_EVENTO_FECHAS ON ADM_EVENTO_INSTITUCIONAL (FECHA_INICIO, FECHA_FIN);
CREATE INDEX IX_ADM_EVENTO_ORDEN   ON ADM_EVENTO_INSTITUCIONAL (ORDEN);

-- =========================================================
-- 2) PAQUETE PK_CREATIVOS
-- =========================================================
CREATE OR REPLACE PACKAGE PK_CREATIVOS AS

    PROCEDURE P_GET_EVENTOS(
        P_ID_EVENTO IN NUMBER,
        P_RESULT    OUT SYS_REFCURSOR
    );

    PROCEDURE P_GET_EVENTOS_ACTIVOS(
        P_FECHA_REFERENCIA IN DATE,
        P_RESULT           OUT SYS_REFCURSOR
    );

    PROCEDURE P_INS_EVENTO(
        P_UNIDAD            IN VARCHAR2,
        P_TITULO            IN VARCHAR2,
        P_SUBTITULO         IN VARCHAR2,
        P_DESCRIPCION       IN CLOB,
        P_LUGAR             IN VARCHAR2,
        P_CIUDAD            IN VARCHAR2,
        P_FECHA_INICIO      IN DATE,
        P_FECHA_FIN         IN DATE,
        P_HORA_INICIO       IN VARCHAR2,
        P_HORA_FIN          IN VARCHAR2,
        P_IMAGEN_PORTADA    IN VARCHAR2,
        P_ENLACE_EVENTO     IN VARCHAR2,
        P_TIPO_EVENTO       IN VARCHAR2,
        P_ORDEN             IN NUMBER,
        P_USUARIO_CREACION  IN VARCHAR2,
        P_MAQUINA_CREACION  IN VARCHAR2,
        P_ID_EVENTO         OUT NUMBER,
        P_RESULTADO         OUT NUMBER,
        P_MENSAJE           OUT VARCHAR2
    );

    PROCEDURE P_UPD_EVENTO(
        P_ID_EVENTO          IN NUMBER,
        P_UNIDAD             IN VARCHAR2,
        P_TITULO             IN VARCHAR2,
        P_SUBTITULO          IN VARCHAR2,
        P_DESCRIPCION        IN CLOB,
        P_LUGAR              IN VARCHAR2,
        P_CIUDAD             IN VARCHAR2,
        P_FECHA_INICIO       IN DATE,
        P_FECHA_FIN          IN DATE,
        P_HORA_INICIO        IN VARCHAR2,
        P_HORA_FIN           IN VARCHAR2,
        P_IMAGEN_PORTADA     IN VARCHAR2,
        P_ENLACE_EVENTO      IN VARCHAR2,
        P_TIPO_EVENTO        IN VARCHAR2,
        P_ORDEN              IN NUMBER,
        P_VIGENTE            IN NUMBER,
        P_USUARIO_MODIFICA   IN VARCHAR2,
        P_MAQUINA_MODIFICA   IN VARCHAR2,
        P_RESULTADO          OUT NUMBER,
        P_MENSAJE            OUT VARCHAR2
    );

    PROCEDURE P_DEL_EVENTO(
        P_ID_EVENTO          IN NUMBER,
        P_USUARIO_MODIFICA   IN VARCHAR2,
        P_MAQUINA_MODIFICA   IN VARCHAR2,
        P_RESULTADO          OUT NUMBER,
        P_MENSAJE            OUT VARCHAR2
    );

END PK_CREATIVOS;
/

CREATE OR REPLACE PACKAGE BODY PK_CREATIVOS AS

    PROCEDURE P_GET_EVENTOS(
        P_ID_EVENTO IN NUMBER,
        P_RESULT    OUT SYS_REFCURSOR
    )
    AS
    BEGIN
        OPEN P_RESULT FOR
            SELECT ID_EVENTO, UNIDAD, TITULO, SUBTITULO, DESCRIPCION,
                   LUGAR, CIUDAD, FECHA_INICIO, FECHA_FIN, HORA_INICIO, HORA_FIN,
                   IMAGEN_PORTADA, ENLACE_EVENTO, TIPO_EVENTO, ORDEN,
                   VIGENTE, FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION,
                   FECHA_MODIFICA, USUARIO_MODIFICA, MAQUINA_MODIFICA
              FROM ADM_EVENTO_INSTITUCIONAL
             WHERE P_ID_EVENTO IS NULL OR ID_EVENTO = P_ID_EVENTO
             ORDER BY ORDEN, FECHA_INICIO DESC, ID_EVENTO DESC;
    END P_GET_EVENTOS;


    PROCEDURE P_GET_EVENTOS_ACTIVOS(
        P_FECHA_REFERENCIA IN DATE,
        P_RESULT           OUT SYS_REFCURSOR
    )
    AS
        V_FECHA DATE := NVL(P_FECHA_REFERENCIA, TRUNC(SYSDATE));
    BEGIN
        OPEN P_RESULT FOR
            SELECT ID_EVENTO, UNIDAD, TITULO, SUBTITULO, DESCRIPCION,
                   LUGAR, CIUDAD, FECHA_INICIO, FECHA_FIN, HORA_INICIO, HORA_FIN,
                   IMAGEN_PORTADA, ENLACE_EVENTO, TIPO_EVENTO, ORDEN
              FROM ADM_EVENTO_INSTITUCIONAL
             WHERE VIGENTE = 1
               AND TRUNC(FECHA_INICIO) <= V_FECHA
               AND TRUNC(FECHA_FIN)    >= V_FECHA
             ORDER BY ORDEN, FECHA_INICIO ASC, ID_EVENTO DESC;
    END P_GET_EVENTOS_ACTIVOS;


    PROCEDURE P_INS_EVENTO(
        P_UNIDAD            IN VARCHAR2,
        P_TITULO            IN VARCHAR2,
        P_SUBTITULO         IN VARCHAR2,
        P_DESCRIPCION       IN CLOB,
        P_LUGAR             IN VARCHAR2,
        P_CIUDAD            IN VARCHAR2,
        P_FECHA_INICIO      IN DATE,
        P_FECHA_FIN         IN DATE,
        P_HORA_INICIO       IN VARCHAR2,
        P_HORA_FIN          IN VARCHAR2,
        P_IMAGEN_PORTADA    IN VARCHAR2,
        P_ENLACE_EVENTO     IN VARCHAR2,
        P_TIPO_EVENTO       IN VARCHAR2,
        P_ORDEN             IN NUMBER,
        P_USUARIO_CREACION  IN VARCHAR2,
        P_MAQUINA_CREACION  IN VARCHAR2,
        P_ID_EVENTO         OUT NUMBER,
        P_RESULTADO         OUT NUMBER,
        P_MENSAJE           OUT VARCHAR2
    )
    AS
    BEGIN
        IF P_FECHA_FIN < P_FECHA_INICIO THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'La fecha fin no puede ser menor a la fecha inicio.';
            RETURN;
        END IF;

        INSERT INTO ADM_EVENTO_INSTITUCIONAL (
            ID_EVENTO, UNIDAD, TITULO, SUBTITULO, DESCRIPCION, LUGAR, CIUDAD,
            FECHA_INICIO, FECHA_FIN, HORA_INICIO, HORA_FIN,
            IMAGEN_PORTADA, ENLACE_EVENTO, TIPO_EVENTO, ORDEN, VIGENTE,
            FECHA_CREACION, USUARIO_CREACION, MAQUINA_CREACION
        ) VALUES (
            SQ_ADM_EVENTO_INSTITUCIONAL.NEXTVAL,
            SUBSTR(P_UNIDAD, 1, 100),
            SUBSTR(P_TITULO, 1, 300),
            SUBSTR(P_SUBTITULO, 1, 500),
            P_DESCRIPCION,
            SUBSTR(P_LUGAR, 1, 200),
            SUBSTR(P_CIUDAD, 1, 100),
            P_FECHA_INICIO,
            P_FECHA_FIN,
            SUBSTR(P_HORA_INICIO, 1, 5),
            SUBSTR(P_HORA_FIN, 1, 5),
            SUBSTR(P_IMAGEN_PORTADA, 1, 500),
            SUBSTR(P_ENLACE_EVENTO, 1, 500),
            SUBSTR(P_TIPO_EVENTO, 1, 50),
            NVL(P_ORDEN, 1),
            1,
            SYSDATE,
            SUBSTR(P_USUARIO_CREACION, 1, 100),
            SUBSTR(P_MAQUINA_CREACION, 1, 100)
        ) RETURNING ID_EVENTO INTO P_ID_EVENTO;

        P_RESULTADO := 1;
        P_MENSAJE := 'Evento creado correctamente.';

    EXCEPTION
        WHEN OTHERS THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'Error al crear evento: ' || SQLERRM;
    END P_INS_EVENTO;


    PROCEDURE P_UPD_EVENTO(
        P_ID_EVENTO          IN NUMBER,
        P_UNIDAD             IN VARCHAR2,
        P_TITULO             IN VARCHAR2,
        P_SUBTITULO          IN VARCHAR2,
        P_DESCRIPCION        IN CLOB,
        P_LUGAR              IN VARCHAR2,
        P_CIUDAD             IN VARCHAR2,
        P_FECHA_INICIO       IN DATE,
        P_FECHA_FIN          IN DATE,
        P_HORA_INICIO        IN VARCHAR2,
        P_HORA_FIN           IN VARCHAR2,
        P_IMAGEN_PORTADA     IN VARCHAR2,
        P_ENLACE_EVENTO      IN VARCHAR2,
        P_TIPO_EVENTO        IN VARCHAR2,
        P_ORDEN              IN NUMBER,
        P_VIGENTE            IN NUMBER,
        P_USUARIO_MODIFICA   IN VARCHAR2,
        P_MAQUINA_MODIFICA   IN VARCHAR2,
        P_RESULTADO          OUT NUMBER,
        P_MENSAJE            OUT VARCHAR2
    )
    AS
    BEGIN
        IF P_FECHA_FIN < P_FECHA_INICIO THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'La fecha fin no puede ser menor a la fecha inicio.';
            RETURN;
        END IF;

        UPDATE ADM_EVENTO_INSTITUCIONAL
           SET UNIDAD            = SUBSTR(P_UNIDAD, 1, 100),
               TITULO            = SUBSTR(P_TITULO, 1, 300),
               SUBTITULO         = SUBSTR(P_SUBTITULO, 1, 500),
               DESCRIPCION       = P_DESCRIPCION,
               LUGAR             = SUBSTR(P_LUGAR, 1, 200),
               CIUDAD            = SUBSTR(P_CIUDAD, 1, 100),
               FECHA_INICIO      = P_FECHA_INICIO,
               FECHA_FIN         = P_FECHA_FIN,
               HORA_INICIO       = SUBSTR(P_HORA_INICIO, 1, 5),
               HORA_FIN          = SUBSTR(P_HORA_FIN, 1, 5),
               IMAGEN_PORTADA    = SUBSTR(P_IMAGEN_PORTADA, 1, 500),
               ENLACE_EVENTO     = SUBSTR(P_ENLACE_EVENTO, 1, 500),
               TIPO_EVENTO       = SUBSTR(P_TIPO_EVENTO, 1, 50),
               ORDEN             = NVL(P_ORDEN, 1),
               VIGENTE           = CASE WHEN P_VIGENTE = 0 THEN 0 ELSE 1 END,
               FECHA_MODIFICA    = SYSDATE,
               USUARIO_MODIFICA  = SUBSTR(P_USUARIO_MODIFICA, 1, 100),
               MAQUINA_MODIFICA  = SUBSTR(P_MAQUINA_MODIFICA, 1, 100)
         WHERE ID_EVENTO = P_ID_EVENTO;

        IF SQL%ROWCOUNT = 0 THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'No existe el evento.';
            RETURN;
        END IF;

        P_RESULTADO := 1;
        P_MENSAJE := 'Evento actualizado correctamente.';

    EXCEPTION
        WHEN OTHERS THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'Error al actualizar evento: ' || SQLERRM;
    END P_UPD_EVENTO;


    PROCEDURE P_DEL_EVENTO(
        P_ID_EVENTO          IN NUMBER,
        P_USUARIO_MODIFICA   IN VARCHAR2,
        P_MAQUINA_MODIFICA   IN VARCHAR2,
        P_RESULTADO          OUT NUMBER,
        P_MENSAJE            OUT VARCHAR2
    )
    AS
    BEGIN
        UPDATE ADM_EVENTO_INSTITUCIONAL
           SET VIGENTE          = 0,
               FECHA_MODIFICA   = SYSDATE,
               USUARIO_MODIFICA = SUBSTR(P_USUARIO_MODIFICA, 1, 100),
               MAQUINA_MODIFICA = SUBSTR(P_MAQUINA_MODIFICA, 1, 100)
         WHERE ID_EVENTO = P_ID_EVENTO;

        IF SQL%ROWCOUNT = 0 THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'No existe el evento.';
            RETURN;
        END IF;

        P_RESULTADO := 1;
        P_MENSAJE := 'Evento eliminado correctamente (lógico).';

    EXCEPTION
        WHEN OTHERS THEN
            P_RESULTADO := 0;
            P_MENSAJE := 'Error al eliminar evento: ' || SQLERRM;
    END P_DEL_EVENTO;

END PK_CREATIVOS;
/
