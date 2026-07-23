     Procedure P_DelRoles(P_IdPrimaria      In Number,
                          P_Justificacion   In Varchar2,
                          P_Usuario         In Number,
                          P_Maquina         In Varchar2,
                          SRV_Message       Out Varchar2,
                          P_Resultado       Out Number) Is
  Begin
    P_Resultado := 0;
    Begin
      Delete nombre_tabla ru Where ru.Id_Primaria = P_IdPrimaria;
      If (Sql%Rowcount > 0) Then                
          P_InsUdpRolesAuditoria(P_IdUserRol,
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
