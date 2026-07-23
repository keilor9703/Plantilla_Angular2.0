-- Create table
create table CTR_AUDITORIA
(
 id_auditoria     NUMBER not null,
  evento           VARCHAR2(255),
  descripcion      VARCHAR2(3000),
  ano              NUMBER,
  identificador  Varchar2(3000),
  usuario_creacion NUMBER not null,
  fecha_creacion   DATE not null,
  maquina_creacion VARCHAR2(255) not null,
  usuario_modifica NUMBER,
  fecha_modifica   DATE,
  maquina_modifica VARCHAR2(255)
)
PARTITION BY RANGE (fecha_creacion)
   INTERVAL(numtoyminterval(1, 'MONTH'))
   (partition p1 values less than (to_date('01-01-2024','dd-mm-yyyy'))   
   );

-- Add comments to the columns 
comment on column CTR_AUDITORIA.id_auditoria
  is 'Identificador primario de la tabla';
comment on column CTR_AUDITORIA.evento
  is 'Evento o procedimiento que ejecuta';
comment on column CTR_AUDITORIA.descripcion
  is 'Descripción';
comment on column CTR_AUDITORIA.ano
  is 'Año de grabación del registro';
comment on column CTR_AUDITORIA.identificador
  is 'Identificador';
comment on column CTR_AUDITORIA.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_AUDITORIA.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_AUDITORIA.maquina_creacion
  is 'Máquina de Creación del regisro';
comment on column CTR_AUDITORIA.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_AUDITORIA.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_AUDITORIA.maquina_modifica
  is 'Maquina Modifica el registro';
-- Create/Recreate indexes 
create index CTR_AUDIT_B1_I on CTR_AUDITORIA (IDENTIFICADOR)
  tablespace GESPO_DATA
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
create index CTR_AUDIT_B2_I on CTR_AUDITORIA (EVENTO)
  tablespace GESPO_DATA
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
create index CTR_AUDIT_B3_I on CTR_AUDITORIA (ANIO)
  tablespace GESPO_DATA
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
create index CTR_AUDIT_B4_I on CTR_AUDITORIA (USUARIO_CREACION)
  tablespace GESPO_DATA
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
create index CTR_AUDIT_B5_I on CTR_AUDITORIA (FECHA_CREACION)
  tablespace GESPO_DATA
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
alter table CTR_AUDITORIA
  add constraint CTR_AUDIT_A1_PK primary key (ID_AUDITORIA)
  using index 
  tablespace GESPO_INDEX
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
