-- Create table
create table CTR_ROLES_USER_ADMIN
(
  id_roluser_admin NUMBER not null,
  id_rol           NUMBER,
  id_usuario       NUMBER,
  justificacion    VARCHAR2(255) not null,
  fecha_fin        DATE not null,
  vigente          NUMBER not null,
  codigo_cargo     NUMBER not null,
  unde_labora      NUMBER not null,
  motivo_elimina   VARCHAR2(255),
  usuario_creacion NUMBER not null,
  fecha_creacion   DATE not null,
  maquina_creacion VARCHAR2(255) not null,
  usuario_modifica NUMBER,
  fecha_modifica   DATE,
  maquina_modifica VARCHAR2(255)
)
tablespace SISTEL_TEST_DATA
  pctfree 10
  initrans 1
  maxtrans 255
  storage
  (
    initial 64K
    next 1M
    minextents 1
    maxextents unlimited
  );
-- Add comments to the columns 
comment on column CTR_ROLES_USER_ADMIN.id_roluser_admin
  is 'Identificador primario de la tabla';
comment on column CTR_ROLES_USER_ADMIN.id_rol
  is 'Identificador de CTR_ROLES';
comment on column CTR_ROLES_USER_ADMIN.id_usuario
  is 'Identificador de CTR_USUARIOS';
comment on column CTR_ROLES_USER_ADMIN.justificacion
  is 'Justificación asgnación del rol al usuario';
comment on column CTR_ROLES_USER_ADMIN.fecha_fin
  is 'Fecha de caducidad del rol';
comment on column CTR_ROLES_USER_ADMIN.vigente
  is 'Estado del Rol';
comment on column CTR_ROLES_USER_ADMIN.codigo_cargo
  is 'Código de Cargo del funcionario con SIATH';
comment on column CTR_ROLES_USER_ADMIN.unde_labora
  is 'Codigo de la Unidad donde labora el funcionario con SIATH';
comment on column CTR_ROLES_USER_ADMIN.motivo_elimina
  is 'Justificación por el cual se elimina el rol en CTR_ROLES_USER';
comment on column CTR_ROLES_USER_ADMIN.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_ROLES_USER_ADMIN.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_ROLES_USER_ADMIN.maquina_creacion
  is 'Máquina de Creación del regisro';
comment on column CTR_ROLES_USER_ADMIN.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_ROLES_USER_ADMIN.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_ROLES_USER_ADMIN.maquina_modifica
  is 'Maquina Modifica el registro';
-- Create/Recreate indexes 
create index ROLEUSERADM_B1_I on CTR_ROLES_USER_ADMIN (ID_ROL)
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
create index ROLEUSERADM_B2_I on CTR_ROLES_USER_ADMIN (ID_USUARIO)
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
create index ROLEUSERADM_B3_I on CTR_ROLES_USER_ADMIN (FECHA_FIN)
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
create index ROLEUSERADM_B4_I on CTR_ROLES_USER_ADMIN (CODIGO_CARGO)
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
create index ROLEUSERADM_B5_I on CTR_ROLES_USER_ADMIN (UNDE_LABORA)
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
alter table CTR_ROLES_USER_ADMIN
  add constraint ROLEUSERADM_A1_PK primary key (ID_ROLUSER_ADMIN)
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
alter table CTR_ROLES_USER_ADMIN
  add constraint ROLEUSERADM_A2_FK foreign key (ID_ROL)
  references CTR_ROLES (ID_ROL);
alter table CTR_ROLES_USER_ADMIN
  add constraint ROLEUSERADM_A3_FK foreign key (ID_USUARIO)
  references CTR_USUARIOS (ID_USUARIO);
