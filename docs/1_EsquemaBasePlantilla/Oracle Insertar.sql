-------  Insertar y Actualizar    ---------------------------------------------------------------------------------
Procedure P_InsUdpHechosRelato(P_IdPrimaria      In Number,
                               P_CampoA          In Varchar2,  
                               P_CampoB          In HECHOS_RELATOS.Descripcion%Type,
                               P_Usuario         In Number,
                               P_Maquina         In Varchar2,
                               SRV_Message       Out Varchar2,
                               P_Resultado       Out Number) As
  Begin
    P_Resultado := 0;
    If P_IdPrimaria = 0 Then
        Begin
          Insert Into Nombre_Tabla
            (            
              Id_Primaria, 
              Campo_A, 
              Campo_B, 
              usuario_creacion, 
              fecha_creacion, 
              maquina_creacion                                 
            )
          Values
            (Nombre_Tabla_SEQ.NEXTVAL,
             P_CampoA,             
             P_CampoB,                          
             P_Usuario,
             Sysdate,
             P_Maquina)
          Returning Id_Primaria Into P_Resultado;
          SRV_Message := 'El registro se grabó exitosamente con Número';
          Commit Work;
        Exception
          When Others Then
            P_Resultado:=0;
            SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;                    
            Goto ExitPaquete;
        End;
    Else     
      Begin
          Update Nombre_Tabla S
             Set  s.Campo_A           = P_CampoA
                 ,s.Campo_B           = P_CampoB  
                 ,s.usuario_modifica  = P_Usuario
                 ,s.fecha_modifica    = Sysdate
                 ,s.maquina_modifica  = P_Maquina
           Where s.Id_Primaria = P_IdPrimaria;
             If Sql%Rowcount > 0 Then
              P_Resultado := P_IdPrimaria;
              SRV_Message := 'Registro actualizado exitosamente';
             Else 
               P_Resultado := 0;         
               SRV_Message := 'No es posible actualizar, revise';
           End If;
          Commit Work;
        Exception
          When NO_DATA_FOUND Then
            P_Resultado := 0;
            SRV_Message := 'No es posible actualizar, revise';
            Goto ExitPaquete;
        End;
    End If;
    -- End Code -------------------------    
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
     Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsUdpHechosRelato;
---- Solo Insertar--------------------------------------------------------------------------------------
   Procedure P_InsHechosRelato(P_CampoA          In Varchar2,  
                               P_CampoB          In HECHOS_RELATOS.Descripcion%Type,
                               P_Usuario         In Number,
                               P_Maquina         In Varchar2,
                               SRV_Message       Out Varchar2,
                               P_Resultado       Out Number)
 As  
  Begin
  P_Resultado:=0;
    Begin    
      Insert Into Nombre_Tabla
        (Id_Primaria, 
         Campo_A, 
         Campo_B, 
         usuario_creacion, 
         fecha_creacion, 
         maquina_creacion     
         )
      Values
        (Nombre_Tabla_SEQ.NEXTVAL,
         P_CampoA,
         P_CampoB,
         P_Usuario,
         Sysdate,
         P_Maquina)
         Returning Id_Primaria Into P_Resultado; 
         SRV_Message := 'El registro se grabó exitosamente';
        Commit Work;    
    Exception
      When Others Then
        P_Resultado:=0;
        SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;        
        Goto ExitPaquete;
    End;  
    -- End Code -------------------------
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsHechosRelato;  
--- Insertar y Actualizar con Fecha
Procedure P_InsUdpBitacora (P_IdPrimaria      In Number,                           
                            P_Fecha           In Varchar2,                              
                            P_CampoA          In Varchar2,
                            P_CampoB          In BIBLIA_BITACORA.Descripcion%Type,
                            P_Usuario         In Number,
                            P_Maquina         In Varchar2,
                            SRV_Message       Out Varchar2,
                            P_Resultado       Out Number) As
  V_Fecha Date;
  Begin
    P_Resultado := 0;
    
    If P_Fecha Is Null Then
      V_Fecha := Null;
    Else
      V_Fecha := to_date(P_Fecha,'dd/mm/rrrr hh24:mi');      
    End If;
    
    If P_IdPrimaria = 0 Then
        Begin
          Insert Into Nombre_Tabla
            (            
             Id_Primaria, 
             Fecha,
             Campo_A, 
             Campo_B, 
             usuario_creacion, 
             fecha_creacion, 
             maquina_creacion
            )
          Values
            (Nombre_Tabla_SEQ.NEXTVAL,            
             V_Fecha,                          
             P_CampoA,       
             P_CampoB,
             P_Usuario,
             Sysdate,
             P_Maquina)
          Returning Id_Primaria Into P_Resultado;
          SRV_Message := 'El registro se grabó exitosamente con Número';
          Commit Work;
        Exception
          When Others Then
            P_Resultado := 0;
            SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;            
            Goto ExitPaquete;
        End;
    Else     
      Begin
          Update Nombre_Tabla S
             Set  s.Campo_A           = P_CampoA
                 ,s.Campo_B           = P_CampoA        
                 ,s.usuario_modifica  = P_Usuario
                 ,s.fecha_modifica    = Sysdate
                 ,s.maquina_modifica  = P_Maquina
           Where s.Id_Primaria = P_IdPrimaria;
             If Sql%Rowcount > 0 Then
              P_Resultado := P_IdPrimaria;
              SRV_Message := 'Registro actualizado exitosamente';
             Else 
               P_Resultado := 0;         
               SRV_Message := 'No es posible actualizar, revise';
           End If;
          Commit Work;
        Exception
          When NO_DATA_FOUND Then
            P_Resultado := 0;
            SRV_Message := 'No es posible actualizar, revise';
            Goto ExitPaquete;
        End;
    End If; 
    -- End Code -------------------------   
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsUdpBitacora;  
  --------Actualizar y si no existe inserta---------------------------------------
  Procedure Ins_RevSintSist(P_NumeroPaciente  In Number,                            
                            P_Descripcion     In Varchar2,
                            P_Usuario         In Number,
                            P_Maquina         In Varchar2,
                            SRV_Message       Out Varchar2,
                            P_Resultado       Out Number) As
  

  Begin    
    Begin
      Update REVISION_SINTOMAS_SIST g
         Set g.descripcion      = P_Descripcion,
             g.usuario_modifica = P_Usuario,
             g.fecha_modifica   = Sysdate,
             g.maquina_modifica = P_Maquina
       Where g.mtvcorrelativo   = P_mtvcorrelativo       
         And g.pac_pac_numero   = P_NumeroPaciente;
             P_Resultado:=1;
             SRV_Message := 'Registro actualizado exitosamente';  
      If Sql%Notfound Then
        Begin
          Insert Into REVISION_SINTOMAS_SIST
            (
              id_rev_sintomas_sist, 
              pac_pac_numero,             
              descripcion, 
              usuario_creacion, 
              fecha_creacion, 
              maquina_creacion 
            )
          Values
            (REVISION_SINTOMAS_SIST_SEQ.Nextval,
             P_NumeroPaciente,           
             P_Descripcion,             
             P_Usuario,
             Sysdate,
             P_Maquina)
             Returning id_rev_sintomas_sist Into P_Resultado;
             SRV_Message := 'El Registro se grabó exitosamente';                
        Exception         
              When Others Then
                P_Resultado:=0;
                SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
                Goto ExitPaquete;
        End;
      End If;    
    Exception      
      When Others Then
        P_Resultado:=0;      
        SRV_Message := 'Error durante la actualización del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
    End;  
    -- End Code -------------------------
    <<ExitPaquete>>  
    If P_Resultado > 0 Then
      Commit Work;
    Else
     Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;  
  End Ins_RevSintSist;
-----------------------------------------------------------------
Procedure Ins_ComBandeja(                       
                            P_Origen        In Number,                                                   
                            P_Destino       In Number,
                            P_Usuario       In Number, 
                            P_Maquina       In Varchar2,
                            SRV_Message      Out Varchar2,
                            P_Resultado     Out Number) Is
 Exi Float;
 Begin
   P_Resultado:=0;
   Begin
     Insert Into SERVICIOS_EXTRA_BITACORA
       (
         id_bandeja_comp, 
         origen, 
         destino, 
         usuario_creacion, 
         fecha_creacion, 
         maquina_creacion 
       )
     Values
       (DOCUMENTOS_POST_BANCOMP_SEQ.Nextval,        
        P_Origen,
        P_Destino,
        P_Usuario, 
        Sysdate, 
        P_Maquina        
        )
     Returning id_bandeja_comp Into P_Resultado;    
     SRV_Message := 'Registro grabado exitosamente'; 
     Commit Work;
      Pks_Administracion.Ins_Auditoria(P_Identificacion => P_Usuario,
                                       P_Evento         => 'Ins_ComBandeja',
                                       P_Descripcion    => 'Comparte Bandeja como Origen con '||P_Destino,
                                       P_Maquina        => P_Maquina,
                                       P_Documento      => P_Origen,
                                       p_resultado      => Exi);
   Exception
     When Others Then
       P_Resultado := 0;
       SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;       
       Goto ExitPaquete;                                        
   End; 
    -- End Code -------------------------
   <<ExitPaquete>>  
  If P_Resultado > 0 Then
    Commit;
  Else
    Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
    Rollback;
  End If;
 End Ins_ComBandeja;
 
-----------------
   

--Actualiza la tabla de consulta de eventos SvcAtnUpdEncuentroC_Pkg
        DECLARE
            V_Resultdo   NUMBER;
            V_Mensaje    VARCHAR2 (255);
        BEGIN
            NULL;
            PKS_ATENCIONMEDICA.Udp_EncuentroC (P_NumeroPaciente,
                                               Out_MtvCorrelativo,
                                               Out_TabEncuentro,
                                               0,
                                               P_Usuario,
                                               P_Maquina,
                                               V_Mensaje,
                                               V_Resultdo);
        END;
        
---------------Otros Eventos de Oracle        
        When DUP_VAL_ON_INDEX Then
        P_Resultado:=0;
        SRV_Message := 'La información que intenta actualizar genera llave duplicada!.';
        Goto ExitPaquete;
        
        When NO_DATA_FOUND Then
        P_Resultado := 0;
        Goto ExitPaquete;
        When TOO_MANY_ROWS Then
          P_Resultado := 0;
          Goto ExitPaquete;
        When Others Then
          Goto ExitPaquete;
                
        Returning id_rev_sintomas_sist Into P_Resultado;
             SRV_Message := 'El Registro se grabó exitosamente';       
          If Sql%Notfound Then
            P_Resultado:=0;
            SRV_Message := 'No se insertó la información, revise';
            Goto ExitPaquete;
          End If; 
