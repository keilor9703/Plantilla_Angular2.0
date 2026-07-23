-- Create table
create table CTR_ERRORES_SYSTEM
(
  id_error         NUMBER not null,
  procedimiento    VARCHAR2(3000),
  descripcion      VARCHAR2(3000),
  id_documento     VARCHAR2(255),
  usuario_creacion Number,
  fecha_creacion   DATE default Sysdate  
)
PARTITION BY RANGE (fecha_creacion)
   INTERVAL(numtoyminterval(1, 'MONTH'))
   (partition p1 values less than (to_date('01-01-2024','dd-mm-yyyy'))   
   );


-- Add comments to the columns 
comment on column CTR_ERRORES_SYSTEM.id_error
  is 'Identificador primario de la tabla';
comment on column CTR_ERRORES_SYSTEM.procedimiento
  is 'Descripción del procedimiento';
comment on column CTR_ERRORES_SYSTEM.descripcion
  is 'Descripción del Procedimiento';
comment on column CTR_ERRORES_SYSTEM.fecha_creacion
  is 'Fecha de Creación del Registro';
comment on column CTR_ERRORES_SYSTEM.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_ERRORES_SYSTEM.id_documento
  is 'Identificador del reigstro';
-- Create/Recreate indexes 
create index CTR_ERROR_B1_I on CTR_ERRORES_SYSTEM (ID_DOCUMENTO)
  tablespace SISTEL_TEST_INDEX
  pctfree 10
  initrans 2
  maxtrans 255
  storage
  (
    initial 64K
    next 1M
    minextents 1
    maxextents unlimited
  );
create index CTR_ERROR_B2_I on CTR_ERRORES_SYSTEM (USUARIO_CREACION)
  tablespace SISTEL_TEST_INDEX
  pctfree 10
  initrans 2
  maxtrans 255
  storage
  (
    initial 64K
    next 1M
    minextents 1
    maxextents unlimited
  );
create index CTR_ERROR_B3_I on CTR_ERRORES_SYSTEM (PROCEDIMIENTO)
  tablespace SISTEL_TEST_INDEX
  pctfree 10
  initrans 2
  maxtrans 255
  storage
  (
    initial 64K
    next 1M
    minextents 1
    maxextents unlimited
  );
-- Create/Recreate primary, unique and foreign key constraints 
alter table CTR_ERRORES_SYSTEM
  add constraint CTR_ERROR_A1_PK primary key (ID_ERROR)
  using index 
  tablespace SISTEL_TEST_INDEX
  pctfree 10
  initrans 2
  maxtrans 255
  storage
  (
    initial 64K
    next 1M
    minextents 1
    maxextents unlimited
  );
