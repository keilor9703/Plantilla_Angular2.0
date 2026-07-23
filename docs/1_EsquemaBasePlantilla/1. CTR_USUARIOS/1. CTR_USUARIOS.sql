-- Create table
create table CTR_USUARIOS
(
  id_usuario       NUMBER not null,
  bloqueado        NUMBER(1) not null,
  username         VARCHAR2(255) not null,
  identificacion   NUMBER not null,
  grad_alfabetico  VARCHAR2(255) not null,
  apellidos        VARCHAR2(255) not null,
  nombres          VARCHAR2(255) not null,
  funcionario      VARCHAR2(255) not null,
  unde_laborando   NUMBER not null,
  codigocargo      NUMBER not null,
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
comment on column CTR_USUARIOS.id_usuario
  is 'Identificador primario de la tabla';
comment on column CTR_USUARIOS.bloqueado
  is 'Bloqueo del usuario = 0: no bloqueado; 1: Bloqueado';
comment on column CTR_USUARIOS.username
  is 'Nombre del Usuario';
comment on column CTR_USUARIOS.identificacion
  is 'N�mero de Identificaci�n del Usuario';
comment on column CTR_USUARIOS.grad_alfabetico
  is 'Grado Alfab�tico del Funcionario';
comment on column CTR_USUARIOS.apellidos
  is 'Apellidos del Funcionario';
comment on column CTR_USUARIOS.nombres
  is 'Nombre del Funcionario';
comment on column CTR_USUARIOS.funcionario
  is 'Nombre, grado completo del funcionario';
comment on column CTR_USUARIOS.unde_laborando
  is 'Identificador de la unidad donde labora el funcionario';
comment on column CTR_USUARIOS.codigocargo
  is 'Identificador del Cargo donde labora el funcionario';
comment on column CTR_USUARIOS.usuario_creacion
  is 'Usuario creador del registro';
comment on column CTR_USUARIOS.fecha_creacion
  is 'Fecha de creaci�n del registro';
comment on column CTR_USUARIOS.maquina_creacion
  is 'M�quina de Creaci�n del regisro';
comment on column CTR_USUARIOS.usuario_modifica
  is 'Usuario modifica el registro';
comment on column CTR_USUARIOS.fecha_modifica
  is 'Fecha Modifica el registro';
comment on column CTR_USUARIOS.maquina_modifica
  is 'Maquina Modifica el registro';

-- Create/Recreate indexes 
create index CTR_USUARIOS_B1_I on CTR_USUARIOS (UNDE_LABORANDO)
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

create index CTR_USUARIOS_B2_I on CTR_USUARIOS (CODIGOCARGO)
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

alter table CTR_USUARIOS
  add constraint CTR_USUARIOS_A1_PK primary key (ID_USUARIO)
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

alter table CTR_USUARIOS
  add constraint CTR_USUARIOS_A2_UK unique (USERNAME)
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
  
alter table CTR_USUARIOS
  add constraint CTR_USUARIOS_A3_UK unique (IDENTIFICACION)
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
