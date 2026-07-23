create or replace package Pk_Clientes is

  -- Author  : EDWIN.SUAREZ
  -- Created : 23/08/2024 9:22:09 a. m.
  -- Purpose : Paquete que contiene la información de los clientes
  
 Procedure P_InsUdpKardex (P_IdKardex        In Number,
                            P_Apellidos       In Varchar2, 
                            P_Nombres         In Varchar2, 
                            P_Identificacion  In Number, 
                            P_FechaNace       In Varchar2,
                            P_IdDto           In Number, 
                            P_IdLugar         In Number, 
                            P_Direccion       In Varchar2, 
                            P_IdGenero        In Number, 
                            P_Observaciones   In Varchar2,
                            P_Usuario         In Number,
                            P_Maquina         In Varchar2,
                            SRV_Message       Out Varchar2,
                            P_Resultado       Out Number);
 Function F_GetKardex(P_IdKardex In Number) Return Sys_Refcursor;
 Function F_GetKardexAll Return Sys_Refcursor ;
 Function F_GetDominios(P_Id In Number) Return Sys_Refcursor; 
 Procedure P_UdpKardex(P_IdKardex       In Number,
                        P_Apellidos      In Varchar2,
                        P_Nombres        In Varchar2,
                        P_Usuario        In Number,
                        P_Maquina        In Varchar2,
                        SRV_Message      Out Varchar2,
                        P_Resultado      Out Number);
 Function F_GetDepartamentos(P_Id In Number) Return Sys_Refcursor;
 Function F_GetMunicipios(P_Id In Number) Return Sys_Refcursor;   
 Procedure P_InsKardexCursos(P_IdKardex        In Number,
                              P_IdTipo          In Number,
                              P_Descripcion     In Varchar2,
                              P_FechaTermina    In Varchar2,
                              P_Usuario         In Number,
                              P_Maquina         In Varchar2,
                              SRV_Message       Out Varchar2,
                              P_Resultado       Out Number);

 Procedure P_DelKardex(P_IdKardex       In Number,
                       P_Vigente        In Number, 
                       P_Motivo         In Varchar2,                      
                       P_Usuario        In Number,
                       P_Maquina        In Varchar2,
                       SRV_Message      Out Varchar2,
                       P_Resultado      Out Number);                                                                            
                       
 Procedure EliminadoLogicoAutomatico;                       

End Pk_Clientes;
/
CREATE OR REPLACE Package Body pk_Clientes Is
--Procedimiento para crear kardex
  Procedure P_InsUdpKardex(P_IdKardex       In Number,
                           P_Apellidos      In Varchar2,
                           P_Nombres        In Varchar2,
                           P_Identificacion In Number,
                           P_FechaNace      In Varchar2,
                           P_IdDto          In Number,
                           P_IdLugar        In Number,
                           P_Direccion      In Varchar2,
                           P_IdGenero       In Number,
                           P_Observaciones  In Varchar2,
                           P_Usuario        In Number,
                           P_Maquina        In Varchar2,
                           SRV_Message      Out Varchar2,
                           P_Resultado      Out Number) As
    V_Fecha Date;
  Begin
    P_Resultado := 0;
  
    If P_FechaNace Is Null Then
      V_Fecha := Null;
    Else
      V_Fecha := to_date(P_FechaNace, 'dd/mm/rrrr hh24:mi');
    End If;
  
    If P_IdKardex = 0 Then
      Begin
        Insert Into KARDEX
          (id_kardex,
           apellidos,
           nombres,
           identificacion,
           fecha_nace,
           id_dto,
           id_lugar,
           direccion,
           id_genero,
           observaciones,
           usuario_creacion,
           fecha_creacion,
           maquina_creacion)
        Values
          (KARDEX_SEQ.NEXTVAL,
           upper(P_Apellidos),
           upper(P_Nombres),
           P_Identificacion,
           V_Fecha,
           P_IdDto,
           P_IdLugar,
           upper(P_Direccion),
           P_IdGenero,
           P_Observaciones,
           P_Usuario,
           Sysdate,
           P_Maquina)
        Returning id_kardex Into P_Resultado;
        SRV_Message := 'El registro se grabó exitosamente con Número: ' ||   P_Resultado;
        Commit Work;
      Exception
        When Others Then
          P_Resultado := 0;
          SRV_Message := 'Error durante la insercion del registro, reportado por la BD [' ||
                         TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
          Goto ExitPaquete;
      End;
    Else
      Begin
        Update KARDEX S
           Set s.Apellidos        = upper(P_Apellidos),
               s.Nombres          = upper(P_Nombres),
               s.Identificacion   = P_Identificacion,
               S.ID_DTO           = P_IdDto,
               s.Id_Lugar         = P_IdLugar,
               s.Fecha_Nace       = P_FechaNace,
               s.usuario_modifica = P_Usuario,
               s.fecha_modifica   = Sysdate,
               s.maquina_modifica = P_Maquina
         Where s.id_kardex = P_IdKardex;
        If Sql%Rowcount > 0 Then
          P_Resultado := P_IdKardex;
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
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'pk_Clientes.P_InsUdpKardex',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsUdpKardex;
--Obtener Información de la persona
  Function F_GetKardex(P_IdKardex In Number) Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
          Select p.id_kardex                                   IdKardex,
                 p.Apellidos,
                 p.Nombres,
                 p.Identificacion,                 
                 p.id_dto                                      IdDto,
                 PKS_FUNCIONES.F_GetLugaresDetalle(p.id_dto)   Dto,                 
                 p.id_lugar                                    IdLugar,
                 PKS_FUNCIONES.F_GetLugaresDetalle(p.id_lugar) Lugar,                 
                 p.Direccion,
                 p.id_genero                                   IdGenero,                 
                 PKS_FUNCIONES.F_GetDominioDetalle(p.id_genero)Genero,
                 p.Observaciones,
                 to_char(p.fecha_nace, 'dd/mm/yyyy')           FechaNace
            From kardex p
           Where p.id_kardex = P_IdKardex
           And   p.vigente=1;
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetKardex;
  --Obtener Información de todos los incritos
  Function F_GetKardexAll Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
          Select p.id_kardex                                      IdKardex,
                 p.Apellidos,
                 p.Nombres,
                 p.Identificacion,                 
                 p.id_dto                                         IdDto,
                 PKS_FUNCIONES.F_GetLugaresDetalle(p.id_dto)      Dto,                 
                 p.id_lugar                                       IdLugar,
                 PKS_FUNCIONES.F_GetLugaresDetalle(p.id_lugar)    Lugar,                 
                 p.Direccion,
                 p.id_genero                                      IdGenero,                 
                 PKS_FUNCIONES.F_GetDominioDetalle(p.id_genero)   Genero,
                 p.Observaciones,
                 to_char(p.fecha_nace, 'dd/mm/yyyy')              FechaNace,
                 to_char(p.fecha_creacion, 'dd/mm/yyyy hh24:mi')  FechaCreacion
            From kardex p
            Where p.vigente=1;           
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetKardexAll;
--Obtener Listado de Dominios
  Function F_GetDominios(P_Id In Number) Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
         Select p.Id_Dominio IdDominio, 
                p.Descripcion                
           From CTR_DOMINIOS p
          Where p.id_padre = P_Id
            And p.vigente=1;
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetDominios;
--Actualizar Ciudadano
  Procedure P_UdpKardex(P_IdKardex       In Number,
                        P_Apellidos      In Varchar2,
                        P_Nombres        In Varchar2,
                        P_Usuario        In Number,
                        P_Maquina        In Varchar2,
                        SRV_Message      Out Varchar2,
                        P_Resultado      Out Number) Is

  Begin
    P_Resultado := 0;
    Begin
      Update kardex S
         Set  s.Apellidos         = P_Apellidos
             ,s.nombres           = P_Nombres
             ,s.Usuario_Modifica  = P_Usuario
             ,s.Fecha_Modifica    = Sysdate
             ,s.Maquina_Modifica  = P_Maquina
       Where s.id_kardex   = P_IdKardex;
         If Sql%Rowcount > 0 Then
          P_Resultado := P_IdKardex;
          SRV_Message := 'Registro actualizado exitosamente';
          Goto ExitPaquete;
         Else     
           P_Resultado := 0;     
           SRV_Message := 'No es posible actualizar, revise';
           Goto ExitPaquete;
       End If;
      Commit Work;
    Exception
      When NO_DATA_FOUND Then
        P_Resultado := 0;
        SRV_Message := 'No es posible actualizar, revise';
        Goto ExitPaquete;
      When Others Then
          P_Resultado := 0;
          SRV_Message := 'Error durante la actualización del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
         Goto ExitPaquete;
    End;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'pk_Clientes.P_UdpKardex',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
    End If;
  End P_UdpKardex; 
--Obtener Departamentos
  Function F_GetDepartamentos(P_Id In Number) Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
        Select T.Codigo,
               t.Descripcion
          From VM_CTR_LUGARES_GEOGRAFICOS t
        Where T.TIPO = 'DE' 
        And T.LUGE_CODIGO=P_Id;
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetDepartamentos; 
---Obtener Municipios
  Function F_GetMunicipios(P_Id In Number) Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
         Select T.Codigo,
                t.Descripcion
          From  VM_CTR_LUGARES_GEOGRAFICOS t
         Where  T.LUGE_CODIGO = P_Id;
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetMunicipios;
--Insertar Kardex Cursos
  Procedure P_InsKardexCursos(P_IdKardex        In Number,
                              P_IdTipo          In Number,
                              P_Descripcion     In Varchar2,
                              P_FechaTermina    In Varchar2,
                              P_Usuario         In Number,
                              P_Maquina         In Varchar2,
                              SRV_Message       Out Varchar2,
                              P_Resultado       Out Number)
  As
  V_FechaFinal Date;
  V_Anio       Number;
   
  
  Begin
  P_Resultado:=0;
  
  If P_FechaTermina Is Null Then
      V_FechaFinal := Null;
  Else
      V_FechaFinal := to_date(P_FechaTermina, 'dd/mm/rrrr hh24:mi');
  End If;
  
  V_Anio:= EXTRACT(YEAR FROM V_FechaFinal);
  
    Begin    
      Insert Into KARDEX_CURSOS
        (id_cursos, 
         id_kardex, 
         id_tipo, 
         descripcion, 
         anio, 
         fecha_final, 
         usuario_creacion, 
         fecha_creacion, 
         maquina_creacion         
         )
      Values
        (KARDEX_CURSOS_SEQ.NEXTVAL,
         P_IdKardex,
         P_IdTipo,
         P_Descripcion,
         V_Anio,
         V_FechaFinal,        
         P_Usuario,
         Sysdate,
         P_Maquina)
         Returning id_cursos Into P_Resultado; 
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
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'pk_Clientes.P_InsKardexCursos',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsKardexCursos;  
--Eliminado lógico del registro
  Procedure P_DelKardex(P_IdKardex       In Number,
                        P_Vigente        In Number, 
                        P_Motivo         In Varchar2,                      
                        P_Usuario        In Number,
                        P_Maquina        In Varchar2,
                        SRV_Message      Out Varchar2,
                        P_Resultado      Out Number) Is

  Begin
    P_Resultado := 0;
    Begin
      Update kardex S
         Set  s.Vigente           = P_Vigente
             ,s.Motivo_elimina    = P_Motivo
             ,s.Usuario_Modifica  = P_Usuario
             ,s.Fecha_Modifica    = Sysdate
             ,s.Maquina_Modifica  = P_Maquina
       Where s.id_kardex  = P_IdKardex;
         If Sql%Rowcount > 0 Then
          P_Resultado := 1;
          SRV_Message := 'Registro eliminado exitosamente';
          Goto ExitPaquete;
         Else     
           P_Resultado := 0;     
           SRV_Message := 'No es posible eliminar, revise';
           Goto ExitPaquete;
       End If;
      Commit Work;
    Exception
      When NO_DATA_FOUND Then
        P_Resultado := 0;
        SRV_Message := 'No es posible eliminar, revise';
        Goto ExitPaquete;
      When Others Then
          P_Resultado := 0;
          SRV_Message := 'Error durante la eliminar del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
         Goto ExitPaquete;
    End;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
    End If;
  End P_DelKardex;
--Método para deshabilitar

  Procedure EliminadoLogicoAutomatico Is
    
  SRV_Message Varchar2(255);
  P_Resultado Float;
  Begin
    For I In (
              Select t.id_kardex,
               t.apellidos,
               t.nombres,
               t.identificacion,
               t.id_dto,
               t.id_lugar,
               t.direccion,
               t.id_genero,
               t.fecha_nace,
               t.vigente,
               t.observaciones,
               t.usuario_creacion,
               t.fecha_creacion,
               t.maquina_creacion,
               t.usuario_modifica,
               t.fecha_modifica,
               t.maquina_modifica,
               t.motivo_elimina,
               t.fecha_inicio,
               t.fecha_fin
        from KARDEX t
        Where Sysdate >= t.fecha_fin
      ) Loop
     
  P_DelKardex(I.ID_KARDEX ,
              0 ,
              'Eliminado por el Sistema' ,
              79955968 ,
              '172.28.3.26' ,
              SRV_Message ,
              P_Resultado);
    
    
    End Loop;
  End EliminadoLogicoAutomatico;
  

End pk_Clientes;
/
