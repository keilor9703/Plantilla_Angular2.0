-- Create table
create table CTR_DOMINIOS
(
  id_dominio       NUMBER not null,
  descripcion      VARCHAR2(3000 CHAR),
  id_padre         NUMBER,
  vigente          NUMBER,
  abreviatura      VARCHAR2(255 CHAR),
  observacion      VARCHAR2(255 CHAR),
  usuario_creacion NUMBER,
  fecha_creacion   DATE,
  maquina_creacion VARCHAR2(255),
  usuario_modifica NUMBER,
  fecha_modifica   DATE,
  maquina_modifica VARCHAR2(255)
)
tablespace GESPO_DATA
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
-- Create/Recreate indexes 
create index CTR_DOM_B1_I on CTR_DOMINIOS (ABREVIATURA)
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
create index CTR_DOM_B2_I on CTR_DOMINIOS (ID_PADRE)
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
create index CTR_DOM_B3_I on CTR_DOMINIOS (VIGENTE)
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
-- Create/Recreate primary, unique and foreign key constraints 
alter table CTR_DOMINIOS
  add constraint CTR_DOM_A1_PK primary key (ID_DOMINIO)
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
