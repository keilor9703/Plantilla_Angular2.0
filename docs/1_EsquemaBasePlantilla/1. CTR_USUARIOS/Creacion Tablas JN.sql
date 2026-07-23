CREATE OR REPLACE Trigger AUDIT_SERVICIOS_EXTRA_JN
  Before Insert Or Update Or Delete On SERVICIOS_EXTRA
  For Each Row
Declare
  V_MAQUINA Varchar2(255);
Begin

  Begin
    SELECT SYS_CONTEXT('USERENV', 'IP_ADDRESS') || '-' ||
             SYS_CONTEXT('USERENV', 'HOST')
        INTO V_MAQUINA
        FROM DUAL;
  End;

  -------------------------------------------------------------------------------
  -- Cuando se esta insertando un registro, se ejecuta el siguiente query.
  -------------------------------------------------------------------------------
  If inserting Then
    Insert Into SERVICIOS_EXTRA_JN
    Values
      (
       :new.id_serviextra,
       :new.evento,
       :new.fecha_servicio,
       :new.regi,
       :new.unidad,
       :new.id_dto,
       :new.id_lugar,
       :new.lugar_faccion,
       :new.id_estado,
       :new.identificacion,
       :new.celular,
       :new.ofi,
       :new.sub,
       :new.pat,
       :new.motos,
       :new.bus,
       :new.camion,
       :new.tanqueta,
       :new.indicativo,
       :new.bloq_vias,
       :new.lesionados,
       :new.ctr_disturbios,
       :new.capturas,
       :new.observaciones_fin,
       :new.latitud,
       :new.longitud,
       :new.observaciones,
       :new.fecha_creacion,
       :new.usuario_creacion,
       :new.imei,
       :new.fecha_modifica,
       :new.usuario_modifica,
       :new.imei_modifica,
       :new.id_grupo,
       :new.vehiculos,
       :new.observaciones_ini,
       :new.faccion_udp,
       :new.pin,
       :new.actividadid,
       :new.funcionario,
       :new.dron,
       :new.fecha_fin,
       :new.ruta_fc,
       :new.ruta_mp,
       :new.id_tienefc,
       :new.id_tienemp,
       :new.maquina_modifica,
       :new.id_tipo_minuta,
         'INS',
         Sysdate,
         V_MAQUINA
      );

    -------------------------------------------------------------------------------
    -- Cuando se esta modificando un registro, se ejecuta el siguiente query.
    -------------------------------------------------------------------------------
  Elsif updating Then
    Insert Into SERVICIOS_EXTRA_JN
    Values
      (:old.id_serviextra,
       :old.evento,
       :old.fecha_servicio,
       :old.regi,
       :old.unidad,
       :old.id_dto,
       :old.id_lugar,
       :old.lugar_faccion,
       :old.id_estado,
       :old.identificacion,
       :new.celular,
       :new.ofi,
       :new.sub,
       :new.pat,
       :new.motos,
       :new.bus,
       :new.camion,
       :new.tanqueta,
       :new.indicativo,
       :old.bloq_vias,
       :old.lesionados,
       :old.ctr_disturbios,
       :old.capturas,
       :old.observaciones_fin,
       :old.latitud,
       :old.longitud,
       :old.observaciones,
       :old.fecha_creacion,
       :old.usuario_creacion,
       :old.imei,
       :new.fecha_modifica,
       :new.usuario_modifica,
       :new.imei_modifica,
       :old.id_grupo,
       :new.vehiculos,
       :new.observaciones_ini,
       :old.faccion_udp,
       :old.pin,
       :old.actividadid,
       :old.funcionario,
       :old.dron,
       :old.fecha_fin,
       :old.ruta_fc,
       :old.ruta_mp,
       :old.id_tienefc,
       :old.id_tienemp,
       :new.maquina_modifica,
       :old.id_tipo_minuta,
         'UPD',
         Sysdate,
         V_MAQUINA);

    -------------------------------------------------------------------------------
    -- Cuando se esta eliminando un registro, se ejecuta el siguiente query.
    -------------------------------------------------------------------------------
  Elsif deleting Then
    Insert Into SERVICIOS_EXTRA_JN
    Values
      (:old.id_serviextra,
       :old.evento,
       :old.fecha_servicio,
       :old.regi,
       :old.unidad,
       :old.id_dto,
       :old.id_lugar,
       :old.lugar_faccion,
       :old.id_estado,
       :old.identificacion,
       :old.celular,
       :old.ofi,
       :old.sub,
       :old.pat,
       :old.motos,
       :old.bus,
       :old.camion,
       :old.tanqueta,
       :old.indicativo,
       :old.bloq_vias,
       :old.lesionados,
       :old.ctr_disturbios,
       :old.capturas,
       :old.observaciones_fin,
       :old.latitud,
       :old.longitud,
       :old.observaciones,
       :old.fecha_creacion,
       :old.usuario_creacion,
       :old.imei,
       :new.fecha_modifica,
       :new.usuario_modifica,
       :new.imei_modifica,
       :old.id_grupo,
       :old.vehiculos,
       :old.observaciones_ini,
       :old.faccion_udp,
       :old.pin,
       :old.actividadid,
       :old.funcionario,
       :old.dron,
       :old.fecha_fin,
       :old.ruta_fc,
       :old.ruta_mp,
       :old.id_tienefc,
       :old.id_tienemp,
       :new.maquina_modifica,
       :old.id_tipo_minuta,
         'DEL',
         Sysdate,
         V_MAQUINA);

  End If;

End AUDIT_SERVICIOS_EXTRA_JN;
