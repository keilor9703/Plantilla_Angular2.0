-- Create table
create table CTR_ROLES
(
  id_rol           NUMBER not null,
  descripcion      VARCHAR2(255) not null,
  vigente          NUMBER not null,
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
comment on column CTR_ROLES.id_rol
  is 'Identificador primario de la tabla';
comment on column CTR_ROLES.descripcion
  is 'Descripción del Rol';
comment on column CTR_ROLES.vigente
  is 'Estado del rol';
comment on column CTR_ROLES.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_ROLES.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_ROLES.maquina_creacion
  is 'Máquina de Creación del regisro';
comment on column CTR_ROLES.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_ROLES.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_ROLES.maquina_modifica
  is 'Maquina Modifica el registro';
-- Create/Recreate indexes 
create index CTR_ROLES_B2_I on CTR_ROLES (VIGENTE)
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
alter table CTR_ROLES
  add constraint CTR_ROLES_A1_PK primary key (ID_ROL)
  using index 
  tablespace SISTEL_TEST_INDEX
  pctfree 10
  initrans 2
  maxtrans 255
  storage
  (
    initial 64K
    next 64K
    minextents 1
    maxextents unlimited
  );
alter table CTR_ROLES
  add constraint CTR_ROLES_A2_UK unique (DESCRIPCION)
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
