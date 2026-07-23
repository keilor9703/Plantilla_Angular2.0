-- Create table
create table CTR_ROL_CARGO
(
  id_rol_cargo NUMBER not null,
  id_rol       NUMBER,
  id_cargo     NUMBER,
  vigente      NUMBER(1)
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
comment on column CTR_ROL_CARGO.vigente
  is 'Activo 1, Desactivo 0';
-- Create/Recreate indexes 
create index CTRROLCARG_B1_I on CTR_ROL_CARGO (ID_CARGO)
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
create index CTRROLCARG_B2_I on CTR_ROL_CARGO (ID_CARGO, VIGENTE)
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
alter table CTR_ROL_CARGO
  add constraint CTRROLCARG_A1_PK primary key (ID_ROL_CARGO)
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
alter table CTR_ROL_CARGO
  add constraint CTRROLCARG_A2_UK unique (ID_ROL, ID_CARGO)
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
alter table CTR_ROL_CARGO
  add constraint CTRROLCARG_A3_FK foreign key (ID_ROL)
  references CTR_ROLES (ID_ROL);
