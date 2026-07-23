-- Create table
create table CTR_MENU_ROLES
(
  id_menurol       NUMBER not null,
  id_rol           NUMBER not null,
  id_menu          NUMBER not null,
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
    next 64K
    minextents 1
    maxextents unlimited
  );
-- Add comments to the columns 
comment on column CTR_MENU_ROLES.id_menurol
  is 'Identificador Primario de la Tabla';
comment on column CTR_MENU_ROLES.id_rol
  is 'Identificador de CTR_ROLES';
comment on column CTR_MENU_ROLES.id_menu
  is 'Identificador de CTR_MENU';
comment on column CTR_MENU_ROLES.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_MENU_ROLES.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_MENU_ROLES.maquina_creacion
  is 'Máquina de Creación del regisro';
comment on column CTR_MENU_ROLES.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_MENU_ROLES.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_MENU_ROLES.maquina_modifica
  is 'Maquina Modifica el registro';
-- Create/Recreate indexes 
create index CTRMENROL_B1_I on CTR_MENU_ROLES (ID_MENU)
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
create index CTRMENROL_B2_I on CTR_MENU_ROLES (ID_ROL)
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
-- Create/Recreate primary, unique and foreign key constraints 
alter table CTR_MENU_ROLES
  add constraint CTRMENROL_A1_PK primary key (ID_MENUROL)
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
alter table CTR_MENU_ROLES
  add constraint CTRMENROL_A2_UK unique (ID_ROL, ID_MENU)
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
alter table CTR_MENU_ROLES
  add constraint CTRMENROL_A3_FK foreign key (ID_ROL)
  references CTR_ROLES (ID_ROL);
alter table CTR_MENU_ROLES
  add constraint CTRMENROL_A4_FK foreign key (ID_MENU)
  references CTR_MENU (ID_MENU);
