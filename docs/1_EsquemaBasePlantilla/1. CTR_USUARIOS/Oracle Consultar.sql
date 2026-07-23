--Obtener 
  Function F_GetRespuestaTarea(P_Id In Number) Return Sys_Refcursor Is
     V_CONSULTA Sys_Refcursor;
     Begin
       Open V_CONSULTA For
         Select R.ID_RESPUESTA,         
                R.NUMERO_GEPOL,         
                R.FECHA_FIRMA_GEPOL,
                R.ASUNTO_GEPOL,
                R.USUARIO_FIRMA_GEPOL,
                R.ID_UNIDAD,
                ST.NOMBRE,
                R.ID_TAREAS,
                R.OBSERVACIONES,
                R.ID_ESTADO_ACCION,
                R.FECHA_REGISTRO,
                R.USUARIO_REGISTRO,
                R.IDENTIFICACION_USUARIO_REGISTRO
           From SIPAZ_RESPUESTA_TAREA R
           INNER JOIN SIPAZ_TAREAS_X_ACCION ST ON R.ID_TAREAS = ST.ID
          Where R.ID_RESPUESTA = 4
       Return(V_CONSULTA);
       Rollback;
       Close V_CONSULTA;
     Exception
       When Others Then
         Null;
         Return(Null);
  End F_GetDominios;
--------------


