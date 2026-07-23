Create Or Replace Function F_GetDominios(P_IdDominio In Number)
  Return Varchar2 Is
  V_Resultado Varchar2(200);
Begin
  Select p.Descripcion
    Into V_Resultado
    From ctr_dominios P
   Where p.id_dominio = P_IdDominio;
  Return(V_Resultado);

Exception
  When Others Then
    Return('Sin Datos');
End F_GetDominios;
/
