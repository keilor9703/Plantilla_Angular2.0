CREATE OR REPLACE Package PKS_ADMINISTRACION Is

  -- Author  : EDWIN.SUAREZ
  -- Created : 17/03/2021 11:26:01 a. m.
  -- Purpose : 

  Procedure P_DelRoles(P_IdUserRol       In Number,
                       P_Justificacion   In Varchar2,
                       P_Usuario         In Number,
                       P_Maquina         In Varchar2,
                       SRV_Message       Out Varchar2,
                       P_Resultado       Out Number); 
  Function F_GetMenu(P_IdRol In Number, P_Identificacion In Number)  Return Sys_Refcursor;
  Function F_GetRoles Return Sys_Refcursor;
  Function F_GetUserRoles(P_Identificacion In Number) Return Sys_Refcursor;  
  Procedure P_GetValidaUser(P_Usuario               In Varchar2,
                            P_Maquina               In Varchar2,
                            Out_Identificacion      Out Number,
                            Out_GradAlfabetico      Out Varchar2,
                            Out_NombreGrado         Out Varchar2,
                            Out_ApellidosNombres    Out Varchar2,
                            Out_Funcionario         Out Varchar2,
                            Out_EmplConsecutivo     Out Number,
                            Out_EmplUndeConsecutivo Out Number,
                            Out_EmplUndeFuerza      Out Number,
                            Out_IdCargo             Out Number,
                            Out_Cargo               Out Varchar2,
                            Out_IdUsuario           Out Number,
                            Out_Usuario             Out Varchar2,
                            Out_IdUndeLaborando     Out Number,
                            Out_Fisica              Out Varchar2,
                            Out_Dependencia         Out Varchar2,
                            Out_Correo              Out Varchar2,
                            Out_SituacionLaboral    Out Varchar2,
                            Out_Bloqueado           Out Number,
                            Out_Celular             Out Number,
                            CursorRoles             Out Sys_Refcursor,
                            P_Resultado             Out Number);
                            
  Procedure P_InsAuditoria(P_Evento          In Varchar2,  
                           P_Descripcion     In Varchar2,
                           P_Identificador   In Number,
                           P_Usuario         In Number,
                           P_Maquina         In Varchar2,
                           SRV_Message       Out Varchar2,
                           P_Resultado       Out Number);
                           
  Procedure P_InsErrorSystem(P_Procedimiento       In Varchar2,
                             P_Descripcion         In Varchar2,
                             P_IdDocumento         In Varchar2,
                             P_Usuario             In Number
                             );
                          
  Procedure P_InsRolesUser( P_IdUsuario     In Number,
                            P_IdRol         In Number,
                            P_Justificacion In Varchar2,
                            P_FechaFin      In Date,                                              
                            P_Usuario       In Number,
                            P_Maquina       In Varchar2,
                            SRV_Message     Out Varchar2,
                            P_Resultado     Out Number);
  
  Procedure P_InsUdpMenus(P_IdMenu          In Number,
                          P_Descripcion     In Varchar2, 
                          P_IdPadre         In Number, 
                          P_Posicion        In Number, 
                          P_Tipo            In Varchar2, 
                          P_Area            In Varchar2, 
                          P_Controlador     In Varchar2, 
                          P_Vista           In Varchar2, 
                          P_Icono           In Varchar2, 
                          P_Vigente         In Number, 
                          P_Detalle         In Varchar2,
                          P_Usuario         In Number,
                          P_Maquina         In Varchar2,
                          SRV_Message       Out Varchar2,
                          P_Resultado       Out Number);
                                                
  Procedure P_InsUdpRoles(P_IdRol           In Number,
                          P_Descripcion     In Varchar2,  
                          P_Vigente         In Number,
                          P_Usuario         In Number,
                          P_Maquina         In Varchar2,
                          SRV_Message       Out Varchar2,
                          P_Resultado       Out Number);
                          
  Procedure P_InsUdpRolesUserAudit(P_IdUserRole    In Number,
                                   P_IdUsuario     In Number,
                                   P_IdRol         In Number,
                                   P_Justificacion In Varchar2,
                                   P_Vigente       In Number,
                                   P_FechaFin      In Date,
                                   P_Usuario       In Number,
                                   P_Maquina       In Varchar2,
                                   P_UndeLabora    Number,
                                   P_CodigoCargo   Number,
                                   SRV_Message     Out Varchar2,
                                   P_Resultado     Out Number);
                          
  Procedure P_InsUdpUsuarios(P_Identificacion In ctr_usuarios.identificacion%Type,
                             P_Bloqueado      In ctr_usuarios.bloqueado%Type,
                             P_Usuario        In ctr_usuarios.usuario_creacion%Type,
                             P_Maquina        In ctr_usuarios.maquina_creacion%Type,
                             SRV_Message      Out Varchar2,
                             P_Resultado      Out Number);

  Procedure DeshabilitaUsuarios;
  Procedure DeshabilitaUsuariosCargo;
  Procedure DeshabilitaUsuariosFecha;        
  Procedure DeshabilitaUsuariosUnidad;

End PKS_ADMINISTRACION;
/
CREATE OR REPLACE Package Body PKS_ADMINISTRACION Is
  --Eliminar roles Usuarios
  Procedure P_DelRoles(P_IdUserRol       In Number,
                       P_Justificacion   In Varchar2,
                       P_Usuario         In Number,
                       P_Maquina         In Varchar2,
                       SRV_Message       Out Varchar2,
                       P_Resultado       Out Number) Is
  Begin
    P_Resultado := 0;
    Begin
      Delete ctr_roles_user ru Where ru.id_user_rol = P_IdUserRol;
      If (Sql%Rowcount > 0) Then                
          P_InsUdpRolesUserAudit(P_IdUserRol,
                                 0,
                                 0,
                                 P_Justificacion,
                                 0,
                                 Sysdate,
                                 P_Usuario,
                                 P_Maquina,
                                 0,
                                 0,
                                 SRV_Message,
                                 P_Resultado);  
      P_Resultado := 1;                                 
      SRV_Message := 'Registro eliminado exitosamente';                                              
      Else
        P_Resultado := 0;
        SRV_Message := 'No es posible eliminar, revise';
      End If;
      Commit Work;
    Exception
      When Others Then
        P_Resultado := 0;
        SRV_Message := 'Ha ocurrido un error! ' || TO_CHAR(Sqlcode) || '] ' ||Sqlerrm;
        Goto ExitPaquete;
    End;
    -------Finalizar Procedimiento
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
  End P_DelRoles;
  --Obtiene Menus del sistema
  Function F_GetMenu(P_IdRol In Number, P_Identificacion In Number)
    Return Sys_Refcursor Is
    V_CONSULTA Sys_Refcursor;
  Begin
    Begin
      If P_IdRol = 1 Then
        Begin
          Open V_CONSULTA For
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.IdPadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m, CTR_MENU_ROLES mr
             Where mr.id_rol = 1
               And mr.id_menu = m.ID_MENU
               And m.Vigente = 1
            Union
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.Idpadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m
            Connect By Prior m.IdPadre = m.ID_MENU
             Start With m.ID_MENU In (Select me.id_menu
                                        From ctr_menu me
                                       Where me.vigente = 1)
             Order By IdPadre, POSICION;
          Return(V_CONSULTA);
          Rollback;
        End;
      Else
        Begin
          Open V_CONSULTA For
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.IdPadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m, CTR_MENU_ROLES mr
             Where mr.id_rol = 1
               And mr.id_menu = m.ID_MENU
               And m.Vigente = 1
            Union
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.IdPadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m
            Connect By Prior m.IdPadre = m.ID_MENU
             Start With m.ID_MENU In
                        (Select rm.id_menu
                           From CTR_MENU_ROLES rm,
                                CTR_USUARIOS   u,
                                ctr_roles_user ru,
                                ctr_menu       me
                          Where u.id_usuario = ru.id_usuario
                            And ru.id_rol = rm.id_rol
                            And me.id_menu = rm.id_menu
                            And me.vigente = 1
                            And u.identificacion = P_Identificacion
                            And u.bloqueado = 0)
            Union
            --Rol
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.IdPadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m
             Where m.vigente = 1
            Connect By Prior m.IdPadre = m.ID_MENU
             Start With m.ID_MENU In (Select P.id_menu
                                        From CTR_MENU_ROLES P, ctr_menu me
                                       Where me.id_menu = p.id_menu
                                         And me.vigente = 1
                                         And P.ID_ROL In (Select to_number(r.Rol) Rol
                                                            From (With CTE As (Select P_IdRol temp
                                                                                 From dual t)
                                                                   Select Trim(REGEXP_SUBSTR(temp,
                                                                                             '[^,]+',
                                                                                             1,
                                                                                             Level)) Rol
                                                                     From CTE
                                                                   Connect By Level <=
                                                                              REGEXP_COUNT(temp,
                                                                                           '[^,]+')) r
                                                          ))
            
            Union
            Select m.ID_MENU IdMenu,
                   m.Descripcion,
                   m.IdPadre,
                   m.Posicion,
                   m.Icono,
                   m.Detalle,
                   m.Tipo,
                   M.Controlador,
                   M.Vista,
                   m.Area
              From CTR_MENU m
            Connect By Prior m.IdPadre = m.ID_MENU
             Start With m.ID_MENU In
                        (Select g.id_menu
                           From ctr_rol_cargo                           t,
                                VM_CTR_EMPLEADOS_ALL e,
                                CTR_MENU_ROLES                          g,
                                ctr_menu                                me
                          Where e.CodigoCargo = t.id_cargo
                            And g.id_rol = t.id_rol
                            And me.id_menu = g.id_menu
                            And e.identificacion = P_Identificacion
                            And me.vigente = 1
                            And t.vigente = 1)
            
             Order By IdPadre, POSICION;
          Return(v_consulta);
          Rollback;
          Close v_consulta;
        End;
      End If;
    End;
  End F_GetMenu;
  --Obtener Roles
  Function F_GetRoles Return Sys_Refcursor Is
    V_CONSULTA Sys_Refcursor;
  Begin
    Open V_CONSULTA For
      Select r.id_rol         IdRol,
             r.Descripcion,
             r.fecha_creacion FechaCreacion,
             r.Vigente
        From ctr_roles r;
    Return(v_consulta);
    Rollback;
    Close v_consulta;
  End F_GetRoles;
  --Obtener Roles Usuario
  Function F_GetUserRoles(P_Identificacion In Number) Return Sys_Refcursor Is
    V_CONSULTA Sys_Refcursor;
  Begin
  
    Open V_CONSULTA For
      Select to_number(nvl(R.ID_ROL, 0)) IdRol,
             to_number(nvl(ru.id_user_rol, 0)) IdUserRol,
             to_number(nvl(us.id_usuario, 0)) IdUsuario,
             nvl(r.Descripcion, 'NR') Descripcion,
             nvl(to_char(ru.fecha_creacion, 'dd/mm/yyyy hh24:mi'), 'NR') FechaCreacion,
             NVL(em.Funcionario, 'NR') FuncionarioCreacion,
             NVL(to_char(ad.fecha_fin, 'dd/mm/yyyy hh24:mi'), 'NR') FechaFin,
             NVL(ad.Justificacion, 'NR') Justificacion,
             to_number(nvl(us.Bloqueado, 0)) Bloqueado
        From CTR_ROLES_USER ru
       Right Join ctr_usuarios us
          On (us.id_usuario = ru.id_usuario)
        Left Join ctr_roles r
          On (r.id_rol = ru.id_rol)
        Left Join VM_CTR_EMPLEADOS_ALL em
          On (em.identificacion = ru.usuario_creacion)
        Left Join ctr_roles_user_admin ad
          On (ad.id_roluser_admin = ru.id_user_rol)
       Where us.identificacion = P_Identificacion;
    Return(v_consulta);
    Rollback;
    Close v_consulta;
  End F_GetUserRoles;
  --Valida Usuario
  Procedure P_GetValidaUser(P_Usuario               In Varchar2,
                            P_Maquina               In Varchar2,
                            Out_Identificacion      Out Number,
                            Out_GradAlfabetico      Out Varchar2,
                            Out_NombreGrado         Out Varchar2,
                            Out_ApellidosNombres    Out Varchar2,
                            Out_Funcionario         Out Varchar2,
                            Out_EmplConsecutivo     Out Number,
                            Out_EmplUndeConsecutivo Out Number,
                            Out_EmplUndeFuerza      Out Number,
                            Out_IdCargo             Out Number,
                            Out_Cargo               Out Varchar2,
                            Out_IdUsuario           Out Number,
                            Out_Usuario             Out Varchar2,
                            Out_IdUndeLaborando     Out Number,
                            Out_Fisica              Out Varchar2,
                            Out_Dependencia         Out Varchar2,
                            Out_Correo              Out Varchar2,
                            Out_SituacionLaboral    Out Varchar2,
                            Out_Bloqueado           Out Number,
                            Out_Celular             Out Number,
                            CursorRoles             Out Sys_Refcursor,
                            P_Resultado             Out Number) Is
    
  SRV_Message Varchar2(2555);
  Begin
    Out_Identificacion      := 0;
    Out_GradAlfabetico      := '0';
    Out_NombreGrado         := '0';
    Out_ApellidosNombres    := '0';
    Out_Funcionario         := '0';
    Out_EmplConsecutivo     := 0;
    Out_EmplUndeConsecutivo := 0;
    Out_EmplUndeFuerza      := 0;
    Out_IdCargo             := 0;
    Out_Cargo               := '0';
    Out_IdUsuario           := 0;
    Out_Usuario             := '0';
    Out_IdUndeLaborando     := 0;
    Out_Fisica              := '0';
    Out_Dependencia         := '0';
    Out_Correo              := '0';
    Out_SituacionLaboral    := '0';
    Out_Bloqueado           := '0';
    Out_Celular             := 0;
    P_Resultado             := 0;
    --Consulta datos del funcionario 
    Begin
      Select e.Identificacion,
             e.GradAlfabetico,
             e.Nombregrado,
             e.Apellidos || ' ' || e.Nombres,
             e.Funcionario,
             e.Consecutivo,
             e.UndeConsecutivo,
             e.Undefuerza,
             e.CodigoCargo,
             e.CargoActual,
             nvl(us.id_usuario, 0),
             e.Usuarioinstitucional,
             e.Undelaborando,
             e.Fisica,
             e.Dependencia,
             e.Correo,
             nvl(us.Bloqueado, 1),
             nvl(e.Celular, 0) As celular,
             e.SituacionLaboral
        Into Out_Identificacion,
             Out_GradAlfabetico,
             Out_NombreGrado,
             Out_ApellidosNombres,
             Out_Funcionario,
             Out_EmplConsecutivo,
             Out_EmplUndeConsecutivo,
             Out_EmplUndeFuerza,
             Out_IdCargo,
             Out_Cargo,
             Out_IdUsuario,
             Out_Usuario,
             Out_IdUndeLaborando,
             Out_Fisica,
             Out_Dependencia,
             Out_Correo,
             Out_Bloqueado,
             Out_Celular,
             Out_SituacionLaboral
        From ctr_usuarios us
        Join VM_CTR_EMPLEADOS_ALL e
          On (us.identificacion = e.identificacion)
       Where e.UsuarioInstitucional = lower(P_Usuario);
      P_Resultado := 1;
      --Actualizar Datos del Usuario
      Begin
        Update ctr_usuarios us
           Set us.Codigocargo      = Out_IdCargo,
               US.GRAD_ALFABETICO  = Out_GradAlfabetico,
               us.funcionario      = Out_Funcionario,
               us.unde_laborando   = Out_IdUndeLaborando,
               US.USUARIO_MODIFICA = Out_Identificacion,
               US.FECHA_MODIFICA   = Sysdate,
               US.MAQUINA_MODIFICA = P_Maquina
         Where us.identificacion = Out_Identificacion;
        Commit Work;
      Exception
        When Others Then
          Null;
      End;    
    Exception
      When NO_DATA_FOUND Then
        P_Resultado := 0;
        SRV_Message := 'Error reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
      When TOO_MANY_ROWS Then
        P_Resultado := 0;
        SRV_Message := 'Error reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
      When Others Then
        P_Resultado := 0;
        SRV_Message := 'Error reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
    End;
    --Cursor Roles asignados
    Begin
      Open CursorRoles For
        Select ru.Id_Rol IdRol, r.Descripcion
          From CTR_ROLES_USER ru
          Join ctr_roles r
            On (r.id_rol = ru.id_rol)
         Where ru.Id_Usuario = Out_IdUsuario;
      Rollback;
    Exception
      When Others Then
        SRV_Message := 'Error reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
       Goto ExitPaquete;
    End;
    ----Finaliza Paqueta
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit Work;
    Else
      P_Resultado := 0;       
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.F_GetValidaUser',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_GetValidaUser;
  --Insertar Auditoria
  Procedure P_InsAuditoria(P_Evento          In Varchar2,  
                           P_Descripcion     In Varchar2,
                           P_Identificador   In Number,
                           P_Usuario         In Number,
                           P_Maquina         In Varchar2,
                           SRV_Message       Out Varchar2,
                           P_Resultado       Out Number)
 As  
  Begin
  P_Resultado:=0;
    Begin    
      Insert Into ctr_auditoria
        (
        id_auditoria, 
        evento, 
        descripcion, 
        ano, 
        identificador, 
        usuario_creacion, 
        fecha_creacion, 
        maquina_creacion
        )
      Values
        (ctr_auditoria_SEQ.NEXTVAL,
         P_Evento,
         P_Descripcion,
         P_Identificador,
         TO_NUMBER(TO_CHAR(Sysdate, 'YYYY')),
         P_Usuario,
         Sysdate,
         P_Maquina)
         Returning id_auditoria Into P_Resultado; 
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
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsAuditoria',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsAuditoria;  
  --Insertar Errores del Sistema Space
  Procedure P_InsErrorSystem(P_Procedimiento       In Varchar2,
                             P_Descripcion         In Varchar2,
                             P_IdDocumento         In Varchar2,
                             P_Usuario             In Number
                             ) Is
  
  Begin
    Insert Into CTR_ERRORES_SYSTEM
      (      
        id_error, 
        procedimiento, 
        descripcion, 
        id_documento, 
        usuario_creacion, 
        fecha_creacion
      )
    Values
      (CTR_ERRORES_SYSTEM_SEQ.NEXTVAL,
       p_procedimiento,
       p_descripcion,
       P_IdDocumento,       
       P_Usuario,
       Sysdate
       );
    Commit Work;
  Exception
    When Others Then
      Rollback;
      Null;
  End P_InsErrorSystem;     
  --Insertar Roles a los usuarios
  Procedure P_InsRolesUser(P_IdUsuario     In Number,
                           P_IdRol         In Number,
                           P_Justificacion In Varchar2,
                           P_FechaFin      In Date,
                           P_Usuario       In Number,
                           P_Maquina       In Varchar2,
                           SRV_Message     Out Varchar2,
                           P_Resultado     Out Number) Is
  
    V_Existe        Number;
    V_NombreRol     Varchar2(255);
    V_IdUserRol     Number;
    V_IdCargo       Number;
    V_UndeLaborando Number;
  
  Begin
    If P_FechaFin = to_date('01/01/0001', 'dd/mm/rrrr') Then
      P_Resultado := 0;
      SRV_Message := 'Debe registrar fecha';
      Goto ExitPaquete;
    Else
      --Verificar si tiene rol asignado 
      Begin
        Select e.id_user_rol
          Into V_IdUserRol
          From CTR_ROLES_USER e
         Where e.id_usuario = P_IdUsuario
           And e.id_rol = P_IdRol
           And RowNum = 1;
        V_Existe := 1;
      Exception
        When NO_DATA_FOUND Then
          V_Existe := 0;
      End;
    
      If V_Existe > 0 Then
        P_Resultado := 0;
        SRV_Message := 'El usuario ya tiene el rol asignado';
        Goto ExitPaquete;
      Else
        --Buscar Información del Funcionario
        Begin
          Select em.CodigoCargo, em.UndeLaborando
            Into V_IdCargo, V_UndeLaborando
            From VM_CTR_EMPLEADOS_ALL em
            Join ctr_usuarios us
              On (us.identificacion = em.Identificacion)
           Where us.id_usuario = P_IdUsuario;
        End;
      
        Begin
          Select r.descripcion
            Into V_NombreRol
            From ctr_roles r
           Where r.id_rol = P_IdRol;
        End;
       --Insertar el Rol
        Begin
          Insert Into CTR_ROLES_USER
            (id_user_rol,
             id_usuario,
             id_rol,
             fecha_creacion,
             usuario_creacion,
             Maquina_creacion)
          Values
            (CTR_ROLES_USER_SEQ.NEXTVAL,
             to_number(P_IdUsuario),
             to_number(P_IdRol),
             Sysdate,
             P_Usuario,
             P_Maquina)
          Returning id_user_rol Into P_Resultado;
          SRV_Message := 'El registro se grabó exitosamente';
          Commit Work;
          --Insertar la Auditoria del Rol
          P_InsUdpRolesUserAudit(P_Resultado,
                                 P_IdUsuario,
                                 P_IdRol,
                                 P_Justificacion,
                                 1,
                                 P_FechaFin,
                                 P_Usuario,
                                 P_Maquina,
                                 V_UndeLaborando,
                                 V_IdCargo,
                                 SRV_Message,
                                 P_Resultado);
          -- PK_CORREO.ENVIO_CORREO_ASIGROL(P_IdUsuario, v_consecutivo);        
          --Actualizar ultima modificación del usuario
          Begin
            Update ctr_usuarios us
               Set us.fecha_modifica   = Sysdate,
                   us.usuario_modifica = P_Usuario
             Where us.id_usuario       = P_IdUsuario;
            Commit Work;
          End;
        Exception
          When Others Then
            SRV_Message := 'Error durante la actualización del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
            Goto ExitPaquete;
        End;
      End If;
    End If;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;                                          
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => P_IdRol,
                                          P_Usuario       => P_Usuario);            
    End If;
  End P_InsRolesUser;
  --Insertar Menús
  Procedure P_InsUdpMenus(P_IdMenu          In Number,
                          P_Descripcion     In Varchar2, 
                          P_IdPadre         In Number, 
                          P_Posicion        In Number, 
                          P_Tipo            In Varchar2, 
                          P_Area            In Varchar2, 
                          P_Controlador     In Varchar2, 
                          P_Vista           In Varchar2, 
                          P_Icono           In Varchar2, 
                          P_Vigente         In Number, 
                          P_Detalle         In Varchar2,
                          P_Usuario         In Number,
                          P_Maquina         In Varchar2,
                          SRV_Message       Out Varchar2,
                          P_Resultado       Out Number) As
  Begin
    P_Resultado := 0;
    If P_IdMenu = 0 Then
        Begin
          Insert Into ctr_menu
            (            
              id_menu, 
              descripcion, 
              idpadre, 
              posicion, 
              tipo, 
              area, 
              controlador, 
              vista, 
              icono, 
              vigente, 
              detalle, 
              usuario_creacion, 
              fecha_creacion, 
              maquina_creacion       
            )
          Values
            (ctr_menu_SEQ.NEXTVAL,
             P_Descripcion, 
             P_IdPadre, 
             P_Posicion, 
             P_Tipo, 
             P_Area, 
             P_Controlador, 
             P_Vista, 
             P_Icono, 
             P_Vigente, 
             P_Detalle,                
             P_Usuario,
             Sysdate,
             P_Maquina)
          Returning id_menu Into P_Resultado;
          SRV_Message := 'El registro se grabó exitosamente';
          Commit Work;
        Exception
          When Others Then
            P_Resultado:=0;
            SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;                    
            Goto ExitPaquete;
        End;
    Else     
      Begin
          Update ctr_menu S
             Set  s.area              = P_Area
                 ,s.controlador       = P_Controlador
                 ,s.vista             = P_Vista
                 ,s.Icono             = P_Icono
                 ,s.usuario_modifica  = P_Usuario
                 ,s.fecha_modifica    = Sysdate
                 ,s.maquina_modifica  = P_Maquina
           Where s.id_menu = P_IdMenu;
             If Sql%Rowcount > 0 Then
              P_Resultado := P_IdMenu;
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
     Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsUdpMenus',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
      Rollback;
    End If;
  End P_InsUdpMenus; 
  --Insertar Actualizar Roles
  Procedure P_InsUdpRoles(P_IdRol           In Number,
                          P_Descripcion     In Varchar2,  
                          P_Vigente         In Number,
                          P_Usuario         In Number,
                          P_Maquina         In Varchar2,
                          SRV_Message       Out Varchar2,
                          P_Resultado       Out Number) As
  Begin
    P_Resultado := 0;
    If P_IdRol = 0 Then
        Begin
          Insert Into ctr_roles
            (    
              id_rol, 
              descripcion, 
              vigente, 
              usuario_creacion, 
              fecha_creacion, 
              maquina_creacion                                                  
            )
          Values
            (ctr_roles_SEQ.NEXTVAL,
             P_Descripcion,             
             P_Vigente,                          
             P_Usuario,
             Sysdate,
             P_Maquina)
          Returning id_rol Into P_Resultado;
          SRV_Message := 'El registro se grabó exitosamente';
          Commit Work;
        Exception
          When Others Then
            P_Resultado:=0;
            SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;                    
            Goto ExitPaquete;
        End;
    Else     
      Begin
          Update ctr_roles S
             Set  s.Descripcion       = P_Descripcion
                 ,s.Vigente           = P_Vigente  
                 ,s.usuario_modifica  = P_Usuario
                 ,s.fecha_modifica    = Sysdate
                 ,s.maquina_modifica  = P_Maquina
           Where s.id_rol = P_IdRol;
             If Sql%Rowcount > 0 Then
              P_Resultado := P_IdRol;
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
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsUdpRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);  
      Rollback;
    End If;
  End P_InsUdpRoles;
  --Insertar Roles Auditoria  
  Procedure P_InsUdpRolesUserAudit(P_IdUserRole    In Number,
                                   P_IdUsuario     In Number,
                                   P_IdRol         In Number,
                                   P_Justificacion In Varchar2,
                                   P_Vigente       In Number,
                                   P_FechaFin      In Date,
                                   P_Usuario       In Number,
                                   P_Maquina       In Varchar2,
                                   P_UndeLabora    Number,
                                   P_CodigoCargo   Number,
                                   SRV_Message     Out Varchar2,
                                   P_Resultado     Out Number) Is
  
  Begin    
    Begin
      Update CTR_ROLES_USER_ADMIN S
         Set s.vigente          = P_Vigente,
             s.justificacion = Case When P_Vigente = 1 THEN P_Justificacion Else justificacion End, 
             s.motivo_elimina = Case When P_Vigente = 0 THEN P_Justificacion Else motivo_elimina End,
             s.usuario_modifica = P_Usuario,
             s.fecha_modifica   = Sysdate,
             s.maquina_modifica = P_Maquina
       Where s.id_roluser_admin = P_IdUserRole;
       If Sql%Rowcount > 0 Then
          P_Resultado := P_IdUserRole;
          SRV_Message := 'El rol se actualizó exitosamente';
        Else
          Begin
              Insert Into CTR_ROLES_USER_ADMIN
                (id_roluser_admin,
                 id_rol,
                 id_usuario,
                 justificacion,
                 fecha_fin,
                 usuario_creacion,
                 fecha_creacion,
                 vigente,
                 maquina_creacion,
                 UNDE_LABORA,
                 CODIGO_CARGO)
              Values
                (P_IdUserRole,
                 P_IdRol,
                 P_IdUsuario,
                 P_Justificacion,
                 P_FechaFin,
                 P_Usuario,
                 Sysdate,
                 1,
                 P_Maquina,
                 P_UndeLabora,
                 P_CodigoCargo);
              Commit Work;
              P_Resultado := P_IdUserRole;
              SRV_Message := 'El rol se creó exitosamente';
          Exception
            When Others Then
             P_Resultado:=0;
             SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
             Goto ExitPaquete; 
          End;
        End If;
      Commit Work;
    Exception
      When NO_DATA_FOUND Then
        Begin
          Insert Into CTR_ROLES_USER_ADMIN
            (id_roluser_admin,
             id_rol,
             id_usuario,
             justificacion,
             fecha_fin,
             usuario_creacion,
             fecha_creacion,
             vigente,
             maquina_creacion,
             UNDE_LABORA,
             CODIGO_CARGO)
          Values
            (P_IdUserRole,
             P_IdRol,
             P_IdUsuario,
             P_Justificacion,
             P_FechaFin,
             P_Usuario,
             Sysdate,
             1,
             P_Maquina,
             P_UndeLabora,
             P_CodigoCargo);
          Commit Work;
          P_Resultado := P_IdUserRole;
          SRV_Message := 'El rol se creó exitosamente';
        Exception
          When Others Then
            P_Resultado:=0;
            SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
            Goto ExitPaquete;
        End;
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
       Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsUdpRolesAuditoria',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);     
      Rollback;
    End If;   
  End P_InsUdpRolesUserAudit;  
  --Insertar Actualizar Usuarios
  Procedure P_InsUdpUsuarios(P_Identificacion In ctr_usuarios.identificacion%Type,
                             P_Bloqueado      In ctr_usuarios.bloqueado%Type,
                             P_Usuario        In ctr_usuarios.usuario_creacion%Type,
                             P_Maquina        In ctr_usuarios.maquina_creacion%Type,
                             SRV_Message      Out Varchar2,
                             P_Resultado      Out Number) Is
  
    V_Existe         Number;
    v_usuario        Varchar2(255);
    v_apellidos      Varchar2(255);
    v_nombres        Varchar2(255);
    v_IdCargo        Number;
    V_GradAlfabetico Varchar2(255);
    V_UndeLaborando  Number;
    V_Funcionario    Varchar2(255);
    V_IdUsuario      Number;
  Begin
    --Validar si el usuario existe
    Begin
      Select us.id_usuario
        Into V_IdUsuario
        From ctr_usuarios us
       Where us.identificacion = P_Identificacion
         And RowNum = 1;
      V_Existe := 1;
    Exception
      When NO_DATA_FOUND Then
        V_Existe := 0;
    End;
    --Buscar Información del Funcionario
    Begin
      Select em.UsuarioInstitucional,
             em.nombres,
             em.Apellidos,
             em.CodigoCargo,
             em.GradAlfabetico,
             em.UndeLaborando,
             em.Funcionario
        Into v_usuario,
             v_nombres,
             v_apellidos,
             v_IdCargo,
             V_GradAlfabetico,
             V_UndeLaborando,
             V_Funcionario
        From VM_CTR_EMPLEADOS_ALL em
       Where em.identificacion = P_Identificacion;
    End;
    If V_Existe > 0 Then
      Begin
        Update ctr_usuarios us
           Set us.Codigocargo      = v_IdCargo,
               US.GRAD_ALFABETICO  = V_GradAlfabetico,
               us.funcionario      = V_Funcionario,
               us.unde_laborando   = V_UndeLaborando,
               US.USUARIO_MODIFICA = P_Usuario,
               US.FECHA_MODIFICA   = Sysdate,
               US.MAQUINA_MODIFICA = P_Maquina,
               us.bloqueado        = P_Bloqueado
         Where us.identificacion = P_Identificacion;
          If Sql%Rowcount > 0 Then
            P_Resultado := V_IdUsuario;
            SRV_Message := 'Registro actualizado exitosamente';
          Else
            P_Resultado := 0;
            SRV_Message := 'No es posible actualizar, revise';
          End If;
          Commit Work;
      Exception
        When Others Then
          P_Resultado := 0;
          SRV_Message := 'No es posible actualizar, revise';
          Goto ExitPaquete;
      End;
    Else
      Begin
        Insert Into ctr_usuarios
          (id_usuario,
           bloqueado,
           username,
           identificacion,
           Grad_Alfabetico,
           apellidos,
           nombres,
           Funcionario,
           Unde_Laborando,
           codigocargo,
           fecha_creacion,
           usuario_creacion,
           maquina_creacion)
        Values
          (CTR_USUARIOS_SEQ.NEXTVAL,
           P_Bloqueado,
           v_usuario,
           P_Identificacion,
           V_GradAlfabetico,
           v_apellidos,
           v_nombres,
           V_Funcionario,
           V_UndeLaborando,
           v_IdCargo,
           Sysdate,
           P_Usuario,
           P_Maquina)
        Returning id_usuario Into P_Resultado;
        SRV_Message := 'El usuario se creó exitosamente';
        Commit Work;
      Exception
        When Others Then
          P_Resultado := 0;
          SRV_Message := 'Error durante la insercion del registro, reportado por la BD ['||TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
          Goto ExitPaquete;
      End;
    End If;
    ----------------Finalización del Procedimiento--------
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
       Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_InsUdpUsuarios',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
    End If;
  End P_InsUdpUsuarios;
 --Evento para Deshabilitar Roles por reglas
 Procedure DeshabilitaUsuarios Is
  Begin
      DeshabilitaUsuariosCargo;
      DeshabilitaUsuariosUnidad;
      DeshabilitaUsuariosFecha;
    Null;
  End DeshabilitaUsuarios;
 
 Procedure DeshabilitaUsuariosCargo Is
  Begin
    For I In (Select rua.id_roluser_admin,
                     rua.fecha_fin,
                     rua.fecha_creacion,
                     em.activo,
                      rua.codigo_cargo,
                      em.codigocargo
                From CTR_ROLES_USER_ADMIN  rua,
                     ctr_usuarios          us,
                     ctr_roles_user        ru,
                     VM_CTR_EMPLEADOS_ALL em
               Where us.id_usuario = rua.id_usuario
                 And em.identificacion = us.identificacion
                 And ru.id_user_rol = rua.id_roluser_admin
                 And rua.codigo_cargo <> em.codigocargo
                 And rua.vigente = 1 ) Loop
      Begin
        Update CTR_ROLES_USER_ADMIN ruad
           Set ruad.vigente          = 0,
               RUAD.MOTIVO_ELIMINA   = 'Rol deshabilitado por Cambio de cargo',
               ruad.USUARIO_MODIFICA = 0,
               RUAD.FECHA_MODIFICA   = Sysdate,
               RUAD.MAQUINA_MODIFICA = 'SERVER'
         Where RUAD.ID_ROLUSER_ADMIN = i.id_roluser_admin;
        Commit Work;
      Exception
        When Others Then
          Rollback;
          Null;
      End;
      Begin
        Delete CTR_ROLES_USER ru Where ru.id_user_rol = i.id_roluser_admin;
        Commit Work;
      End;
    End Loop;
    DeshabilitaUsuariosUnidad;
  End DeshabilitaUsuariosCargo;
  --Deshabilitados por Unidad
  Procedure DeshabilitaUsuariosUnidad Is
  Begin
    For I In (Select rua.id_roluser_admin,
                     rua.fecha_fin,
                     rua.fecha_creacion,
                     em.activo,
                      rua.codigo_cargo,
                      em.codigocargo
                From CTR_ROLES_USER_ADMIN  rua,
                     ctr_usuarios          us,
                     ctr_roles_user        ru,
                     VM_CTR_EMPLEADOS_ALL em
               Where us.id_usuario = rua.id_usuario
                 And em.identificacion = us.identificacion
                 And ru.id_user_rol = rua.id_roluser_admin
                 And rua.unde_labora <> em.undelaborando
                 And rua.vigente = 1                  ) Loop
      Begin
        Update CTR_ROLES_USER_ADMIN ruad
           Set ruad.vigente          = 0,
               RUAD.MOTIVO_ELIMINA   = 'Rol deshabilitado por Cambio de Unidad',
               ruad.USUARIO_MODIFICA = 0,
               RUAD.FECHA_MODIFICA   = Sysdate,
               RUAD.MAQUINA_MODIFICA = 'SERVER'
         Where RUAD.ID_ROLUSER_ADMIN = i.id_roluser_admin;
        Commit Work;
      Exception
        When Others Then
          Rollback;
          Null;
      End;
      Begin
        Delete CTR_ROLES_USER ru Where ru.id_user_rol = i.id_roluser_admin;
        Commit Work;
      End;
    End Loop;
  End DeshabilitaUsuariosUnidad;
 --Deshabilitar rol por fecha caducidad
 Procedure DeshabilitaUsuariosFecha Is
  Begin
    For I In (Select rua.id_roluser_admin,
                     rua.fecha_fin,
                     rua.fecha_creacion,
                     em.activo
                From CTR_ROLES_USER_ADMIN  rua,
                     ctr_usuarios          us,
                     ctr_roles_user        ru,
                     VM_CTR_EMPLEADOS_ALL em
               Where us.id_usuario = rua.id_usuario
                 And em.identificacion = us.identificacion
                 And ru.id_user_rol = rua.id_roluser_admin                
                 And rua.vigente = 1
                 And rua.fecha_fin <= Sysdate) Loop
      Begin
        Update CTR_ROLES_USER_ADMIN ruad
           Set ruad.vigente          = 0,
               RUAD.MOTIVO_ELIMINA   = 'Rol deshabilitado por fecha',
               ruad.USUARIO_MODIFICA = 0,
               RUAD.FECHA_MODIFICA   = Sysdate,
               RUAD.MAQUINA_MODIFICA = 'SERVER'
         Where RUAD.ID_ROLUSER_ADMIN = i.id_roluser_admin;
        Commit Work;
      Exception
        When Others Then
          Rollback;
          Null;
      End;
      Begin
        Delete CTR_ROLES_USER ru Where ru.id_user_rol = i.id_roluser_admin;
        Commit Work;
      End;
    End Loop;
  End DeshabilitaUsuariosFecha;
 
End PKS_ADMINISTRACION;
/
