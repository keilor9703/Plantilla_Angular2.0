CREATE OR REPLACE Package PKS_FUNCIONES Is

  -- Author  : EDWIN.SUAREZ
  -- Created : 22/01/2022 08:17:00 a. m.
  -- Purpose : 

Function F_GetDominioDetalle(P_IdDominio In Number) Return Varchar2;
Function F_GetLugaresDetalle(P_IdLugar In Number) Return Varchar2;

End PKS_FUNCIONES;
/
CREATE OR REPLACE Package Body PKS_FUNCIONES Is
  --Obtener Detalle Dominios
  Function F_GetDominioDetalle(P_IdDominio In Number) Return Varchar2 Is
    V_Resultado Varchar2(255);
  Begin
    Select t.Descripcion
      Into V_Resultado
      From ctr_dominios t
     Where t.id_dominio = P_IdDominio;
    Return(V_Resultado);
  Exception
    When Others Then
      Return('0');
  End F_GetDominioDetalle;
  --Obtener Lugares Geográficos Detalle
  Function F_GetLugaresDetalle(P_IdLugar In Number) Return Varchar2 Is
    V_Resultado Varchar2(255);
  Begin
    Select t.descripcion
      Into V_Resultado
      From vm_ctr_lugares_geograficos t
     Where t.codigo = P_IdLugar;
    Return(V_Resultado);
  Exception
    When Others Then
      Return('0');
  End F_GetLugaresDetalle;
 
End PKS_FUNCIONES;
/
