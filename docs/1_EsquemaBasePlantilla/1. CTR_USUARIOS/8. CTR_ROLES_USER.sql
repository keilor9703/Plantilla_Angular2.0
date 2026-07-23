-- Create table
create table CTR_ROLES_USER
(
  id_user_rol      NUMBER,
  id_usuario       NUMBER not null,
  id_rol           NUMBER not null,
  usuario_creacion NUMBER not null,
  fecha_creacion   DATE not null,
  maquina_creacion VARCHAR2(255) not null
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
comment on column CTR_ROLES_USER.id_user_rol
  is 'Identificador Primario de la tabla';
comment on column CTR_ROLES_USER.id_usuario
  is 'Identificador de CTR_USUARIOS';
comment on column CTR_ROLES_USER.id_rol
  is 'Identificador de CTR_ROLES';
comment on column CTR_ROLES_USER.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_ROLES_USER.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_ROLES_USER.maquina_creacion
  is 'Máquina de Creación del regisro';
-- Create/Recreate indexes 
create index CTR_ROLES_USER_B1_I on CTR_ROLES_USER (ID_USUARIO)
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
create index CTR_ROLES_USER_B2_I on CTR_ROLES_USER (ID_ROL)
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
alter table CTR_ROLES_USER
  add constraint CTR_ROLES_USER_A1_PK primary key (ID_USUARIO, ID_ROL)
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
alter table CTR_ROLES_USER
  add constraint CTR_ROLES_USER_A4_FK unique (ID_USER_ROL)
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
alter table CTR_ROLES_USER
  add constraint CTR_ROLES_USER_A2_FK foreign key (ID_ROL)
  references CTR_ROLES (ID_ROL);
alter table CTR_ROLES_USER
  add constraint CTR_ROLES_USER_A3_FK foreign key (ID_USUARIO)
  references CTR_USUARIOS (ID_USUARIO);
