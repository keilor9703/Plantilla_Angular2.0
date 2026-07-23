Procedure P_DelCierre(P_IdPrimaria     In Number,
                      P_Vigente        In Number, 
                      P_Motivo         In Varchar2,                      
                      P_Usuario        In Number,
                      P_Maquina        In Varchar2,
                      SRV_Message      Out Varchar2,
                      P_Resultado      Out Number) Is

  Begin
    P_Resultado := 0;
    Begin
      Update nombre_tabla S
         Set  s.Vigente           = P_Vigente
             ,s.Motivo            = P_Motivo
             ,s.Usuario_Modifica  = P_Usuario
             ,s.Fecha_Modifica    = Sysdate
             ,s.Maquina_Modifica  = P_Maquina
       Where s.Id_Primaria  = P_IdPrimaria;
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
  End P_DelCierre;
