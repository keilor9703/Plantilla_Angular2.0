--Validar si existe inmueble registrsdo
    Begin
      Select l.consecutivo_casa
        Into V_Id
        From APP_CC_INMUEBLE l
       Where l.direccion_completa = P_DireccionCompleta
         And l.luge_ciudad = P_LugeCiudad
         And l.cuadrante = P_Cuadrante
         And RowNum = 1;
      V_Existe := 1;
    Exception
      When NO_DATA_FOUND Then
        V_Existe := 0;
    End;
  
    If V_Existe > 0 Then
      P_Resultado := 0;
      SRV_Message := 'El inmueble ya se encuentra registrado';
      Goto ExitPaquete;
    End If;
    
