
SELECT SUBSTR(LPAD(' ', (LEVEL - 1) * 3)||u.descripcion,1,240)Descripcion, u.id_menu IdMenu, u.posicion, u.tipo, u.Area, u.controlador, u.Vista, u.usuario_creacion, u.padreid, u.vigente, SUBSTR(u.descripcion,1,240) Nombre
  FROM CTR_MENU u 
  --WHERE u.vigente='SI' 
  CONNECT BY PRIOR u.id_menu =u.padreid  
 START WITH id_menu=0
 order siblings by posicion Asc --for update
