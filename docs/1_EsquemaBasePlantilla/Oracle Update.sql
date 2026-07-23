Procedure P_UdpCierre(P_IdPrimaria     In Number,
                      P_Cerrado        In Number,                       
                      P_Usuario        In Number,
                      P_Maquina        In Varchar2,
                      SRV_Message      Out Varchar2,
                      P_Resultado      Out Number) Is

  Begin
    P_Resultado := 0;
    Begin
      Update Nombre_Tabla S
         Set  s.Cerrado           = P_Cerrado
             ,s.Usuario_Modifica  = P_Usuario
             ,s.Fecha_Modifica    = Sysdate
             ,s.Maquina_Modifica  = P_Maquina
       Where s.Id_Primaria  = P_IdPrimaria;
         If Sql%Rowcount > 0 Then
          P_Resultado := P_IdPrimaria;
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
      Pks_Administracion.P_InsErrorsystem(P_Procedimiento => 'PKS_ADMINISTRACION.P_DelRoles',
                                          P_Descripcion   => SRV_Message,
                                          P_IdDocumento   => 0,
                                          P_Usuario       => P_Usuario);
    End If;
  End P_UdpCierre; 
  
  
  
  
 ----Eventos Oracle
  dbms_output.put_line('Student grade has been changed.');
  
  When DUP_VAL_ON_INDEX Then
      SRV_Message    := 'La informacion que intenta actualizar genera llave duplicada';
      P_Resultado:=0;
      Goto RegiClinic_Exit;      

-------------Update con Ciclo--
CREATE OR REPLACE ChangeGrade (p_sID enrolling.sID%TYPE,
                               p_classID enrolling.classID%TYPE)
AS
  CURSOR cEnrolling IS
         SELECT     *
         FROM       Enrolling
         WHERE      SID = p_sID
         AND        classID = p_classID;

  rEnrolling cEnrolling%ROWTYPE;
BEGIN
        OPEN cEnrolling;
        FETCH cEnrolling INTO rEnrolling;
              IF cEnrolling%FOUND THEN
                      -- Student record found.
                      UPDATE Enrolling
                      SET Grade = 'A'
                      WHERE SID = rEnrolling.sId;
                      DBMS_OUTPUT.PUT_LINE('Student grade has been changed.');
              ELSE
                      -- Student record not found. 
                      DBMS_OUTPUT.PUT_LINE('Student record does not exist.');
              END IF;
        CLOSE cEnrolling;
END;
----------------
