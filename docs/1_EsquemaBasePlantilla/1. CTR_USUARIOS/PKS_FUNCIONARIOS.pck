create or replace package PK_FUNCIONARIOS is

  -- Author  : EDWIN.SUAREZ
  -- Created : 15/12/2021 04:33:33 p. m.
  -- Purpose : 
  
 Function F_GetEmpleadoIntel(v_busqueda in varchar2) Return Sys_Refcursor;
 Function F_GetFuncionarioId (P_Identificacion in number) return Sys_Refcursor;
 Function F_GetFuncAntUnidad(P_UndeLaborando In Number) Return Sys_Refcursor;   

end PK_FUNCIONARIOS;
/
CREATE OR REPLACE Package Body PK_FUNCIONARIOS Is

  --Busqueda Inteligente Empleado
  Function F_GetEmpleadoIntel(V_Busqueda In Varchar2) Return Sys_Refcursor Is
    V_CONSULTA Sys_Refcursor;
  Begin
    Begin
      Open V_CONSULTA For
        Select TO_NUMBER(F.Identificacion)Identificacion, Trim(F.FUNCIONARIO) Funcionario
          From VM_CTR_EMPLEADOS_INDEX F
         Where catsearch(F.funcionario, V_Busqueda, '') > 0
           And rownum < 15;
      Return(V_CONSULTA);
      Rollback;
      Close V_CONSULTA;
    End;
  Exception
    When Others Then
      Null;
      Rollback;
  End F_GetEmpleadoIntel;
  --Obtener Funcionarios todos
  Function F_GetFuncionarioId(P_Identificacion In Number)
    Return Sys_Refcursor Is
    v_Consulta Sys_Refcursor;
  Begin
    Open v_Consulta For
      Select e.Activo,
             nvl(e.AntiguedadGrado, 0) AntiguedadGrado,
             e.Apellidos,
             e.CargoActual,
             e.descripcion Categoria,
             nvl(e.Celular, 0) Celular,
             nvl(e.codigocargo, 0) CodigoCargo,
             e.Consecutivo,
             e.Correo,
             e.Dependencia,
             e.DireccionReside,
             e.EstadoCivil,
             e.Fisica,
             e.Funcionario,
             e.GradAlfabetico,
             e.Identificacion,
             e.Nombres,
             e.NombreGrado,
             e.Numerico,
             nvl(e.NumeroCurso, 0) NumeroCurso,
             nvl(e.Placa, 0) Placa,
             e.Profesion,
             e.idsituacionlaboral IdSituacion,
             e.SituacionLaboral,
             nvl(e.Telefono, 0) Telefono,
             e.UndeConsecutivo,
             e.UndeLaborando,
             e.UndeFuerza,
             e.SiglaPapa || '-' || U.AREA || '-' || U.SIGLA Unidad,
             e.UsuarioInstitucional,
             e.DtoReside,
             e.CiudadReside,
             e.BarrioReside,
             e.CiudadNace,
             to_char(e.FechaNace, 'dd/mm/yyyy') FechaNace,
             e.TiempoPonal,
             e.TiempoUnidad,
             e.FactorRH,
             e.Aptitud,
             nvl(e.Categoria, 0) IdCategoria,
             e.SiglaPapa,
             e.sigla SiglaDependencia,
             e.EstadoEmpleado,
             to_char(e.FechaAscenso, 'dd/mm/yyyy') FechaAscenso,
             e.tiempoponalcompleto TiempoCargo,
             nvl(e.Edad, 0) Edad
        From vm_ctr_empleados_all e
        Join VM_CTR_UNIDADES u
          On (u.consecutivo = e.undelaborando)
       Where e.Identificacion = P_Identificacion;
    Return(v_Consulta);
    Rollback;
    Close V_Consulta;
  Exception
    When Others Then
      Null;
  End F_GetFuncionarioId;
  --Obtener el más antiguo de la unidad dependencia
  Function F_GetFuncAntUnidad(P_UndeLaborando In Number) Return Sys_Refcursor Is
    V_CONSULTA Sys_Refcursor;
  Begin
    Open V_CONSULTA For
      Select c.Funcionario, c.Identificacion, c.CargoActual, c.Celular
        From (Select Rownum Cons,
                     b.Funcionario,
                     b.Identificacion,
                     b.CargoActual,
                     b.Celular
                From (Select a.Funcionario,
                             a.Identificacion,
                             a.Cargoactual,
                             nvl(a.celular, 0) Celular
                        From VM_CTR_EMPLEADOS_ALL A
                       Where A.UNDELABORANDO = P_UndeLaborando
                         And A.ACTIVO = 'SI'
                         And a.idsituacionlaboral = 1
                       Order By A.NUMERICO, A.FECHAASCENSO, A.ANTIGUEDADGRADO) b) c
       Where c.Cons = 1;
    Return(V_CONSULTA);
    Rollback;
    Close V_CONSULTA;
  Exception
    When Others Then
      Null;
      Return(Null);
  End F_GetFuncAntUnidad;

End PK_FUNCIONARIOS;
/
