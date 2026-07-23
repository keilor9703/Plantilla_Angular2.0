-- Create table
create table CTR_MENU
(
  id_menu          NUMBER not null,
  descripcion      VARCHAR2(255) not null,
  idpadre          NUMBER not null,
  posicion         NUMBER not null,
  tipo             VARCHAR2(255) not null,
  area             VARCHAR2(255),
  controlador      VARCHAR2(255),
  vista            VARCHAR2(255),
  icono            VARCHAR2(255),
  vigente          NUMBER not null,
  detalle          VARCHAR2(255),
  usuario_creacion NUMBER not null,
  fecha_creacion   DATE not null,
  maquina_creacion VARCHAR2(255) not null,
  usuario_modifica NUMBER,
  fecha_modifica   DATE,
  maquina_modifica VARCHAR2(255)
)
tablespace SITIC_TEST_DATA
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
comment on column CTR_MENU.id_menu
  is 'Identificador Primario de la tabla';
comment on column CTR_MENU.descripcion
  is 'Descripción del Menú';
comment on column CTR_MENU.idpadre
  is 'Padre';
comment on column CTR_MENU.posicion
  is 'Posición del Menú en el padre';
comment on column CTR_MENU.tipo
  is 'Tipo de Menu= S: Sub Menú, frm: Formulario, url: hipervinculo a otra página; pdf: apertura archivos pdf';
comment on column CTR_MENU.area
  is 'Área en Visual Studio al cual pertenece el formulario';
comment on column CTR_MENU.controlador
  is 'Controlador de la vista del formulario';
comment on column CTR_MENU.vista
  is 'Vista del formulario en Visual Studio';
comment on column CTR_MENU.icono
  is 'Icono representativo del menú';
comment on column CTR_MENU.vigente
  is 'Estado del Menú';
comment on column CTR_MENU.detalle
  is 'Url donde realizará hipervinculos';
comment on column CTR_MENU.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_MENU.fecha_creacion
  is 'Fecha de creación del registro';
comment on column CTR_MENU.maquina_creacion
  is 'Máquina de Creación del regisro';
comment on column CTR_MENU.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_MENU.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_MENU.maquina_modifica
  is 'Maquina Modifica el registro';
-- Create/Recreate indexes 
create index CTR_MENU_B1_I on CTR_MENU (POSICION)
  tablespace SITIC_TEST_DATA
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
create index CTR_MENU_B2_I on CTR_MENU (TIPO)
  tablespace SITIC_TEST_DATA
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
create index CTR_MENU_B3_I on CTR_MENU (IDPADRE)
  tablespace SITIC_TEST_DATA
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
create index CTR_MENU_B4_I on CTR_MENU (VIGENTE)
  tablespace SITIC_TEST_DATA
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
alter table CTR_MENU
  add constraint CTR_MENU_A1_PK primary key (ID_MENU)
  using index 
  tablespace SITIC_TEST_DATA
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
alter table CTR_MENU
  add constraint CTR_MENU_A1_UK unique (DESCRIPCION, IDPADRE)
  using index 
  tablespace SITIC_TEST_DATA
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
