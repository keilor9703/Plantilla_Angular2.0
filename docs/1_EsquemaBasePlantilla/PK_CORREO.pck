create or replace package PK_CORREO is

  -- Author  : EDWIN.SUAREZ
  -- Created : 9/09/2020 1:57:49 p. m.
  -- Purpose : 
  
 PROCEDURE ENVIO_CORREO_ASIGROL ( v_id_usuario IN NUMBER, UserRolID in number);
 PROCEDURE ENVIO_CORREO_BLOQUEO(v_id_usuario IN NUMBER, V_Detalle in Varchar2);
 PROCEDURE ENVIO_CORREO_RETIROROL(v_id_usuario IN NUMBER, UserRolID in number, P_Justificacion In Varchar2);
end PK_CORREO;
/
create or replace package body PK_CORREO is

--Envio de Correo Asignacion Rol
 PROCEDURE ENVIO_CORREO_ASIGROL(v_id_usuario IN NUMBER, UserRolID in number)
  
   IS
    L_HTML      CLOB;
    V_MENSAJE   CLOB;
    V_EXITO1    FLOAT;
    V_EXITO2    FLOAT;
    V_ASUNTO    VARCHAR2(2000);
    v_firma     VARCHAR2(255) := 'Sistema gespo';
    v_cargo     VARCHAR2(255) := 'Policia Nacional de Colombia';
    v_correoenv VARCHAR2(255) := 'gespo@policia.gov.co';
  
  Begin
    
    FOR I IN (SELECT T.ID_USUARIO,
                     E.Funcionario,
                     E.NombreGrado,
                     E.Correo,
                     E.APELLIDOS,
                     E.NOMBRES,
                     e.CargoActual Cargo,
                     E.fisica SiglaFisica,
                     E.Dependencia,
                     r.descripcion rol,
                     a.justificacion descripcion,
                     TO_CHAR(a.fecha_fin, 'dd/mm/yyyy') fecha_fin
                FROM ctr_usuarios          T,
                     VM_CTR_EMPLEADOS_ALL@DBL2TELEPOL4DBOPER E,
                     ctr_roles_user        ru,
                     ctr_roles             r,
                     CTR_ROLES_USER_ADMIN  a
               WHERE t.identificacion = e.identificacion
                 and t.id_usuario = v_id_usuario
                 and ru.id_user_rol = UserRolID
                 and ru.id_usuario = t.id_usuario
                 and r.id_rol = ru.id_rol
                 and a.id_roluser_admin = ru.id_user_rol) LOOP
    
      V_MENSAJE := 'De manera atenta me permito informar, que le ha sido asignado rol para acceso al Sistema Gestión del Servicio de Policía - GESPO, el cual debera ingresar con su usuario y clave institucional de PSI asi:';
      V_ASUNTO  := 'Asignacion Rol Sistema GESPO';
      BEGIN
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><img src="https://sinac.policia.gov.co:8443/herramientas/images/CabezoteCorreos.png" /></p>' ||
                  '<style type="text/css">.mitabla{border-collapse: collapse; width:75%; border-left: 1px solid #1e8cbe;border-right: 1px solid #1e8cbe;font-family:helvetica,arial,sans-serif;font-weight:normal;}.mitabla1 {background-color: #164e49;color:#fff;font-weight: normal;font-size:14px;padding: 8px;}.mitabla2 {margin-right:5px; margin-left:5px; valign:"middle"}.mitabla2 tr:nth-child(odd){background-color: #ffffff;} .mitabla2 tr:nth-child(even) {background-color: #eee;}</style>' ||
                  '<body>' || '<div>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,' || ' ' ||
                  TO_CHAR(SYSDATE,
                          'FMDay, DD "de" Month "de" YYYY',
                          'NLS_DATE_LANGUAGE=SPANISH') || '</div><br/>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Señor (a) ' ||INITCAP(I.NombreGrado) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' || I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.Cargo) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.SiglaFisica || ' - ' || I.Dependencia || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: ' ||V_ASUNTO || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>' ||
                 
                 ---Tabla
                  '<table border=1 bordercolor=1e8cbe class="mitabla">' ||
                    '<thead class="mitabla1">' ||
                      '<th style="text-align: center;">NOMBRE DEL ROL</th>' ||
                      '<th>DETALLE ASIGNACION</th>' ||
                      '<th>FECHA CADUCIDAD</th>' || 
                    '</thead>' ||
                   
                    '<tbody class="mitabla2">' || 
                      '<tr>' || 
                      '<td>' || i.Rol ||'</td>' || 
                      '<td>' || i.descripcion || '</td>' ||
                      '<td style="text-align: center;">' || i.fecha_fin ||'</td>' ||
                      '</tr>'|| 
                    '</tbody>' || 
                  '</table><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">URL de acceso: https://gespo.policia.gov.co:9105/ </div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"></div>' ||
                 
                  '<p></p>' ||                 
                  
                  '</body></html>';
      
        srvcorreo_235.envia_mail_html_new@DBL2TELEPOL4DBOPER(p_origen       => v_correoenv,
                                                             p_destino      => I.Correo,
                                                             p_asunto       => V_ASUNTO,
                                                             p_mensaje      => L_HTML,
                                                             p_confirmacion => V_EXITO1,
                                                             p_error        => V_EXITO2
                                                              );        
      
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;
    END LOOP;
    --p_exito := 1;
    COMMIT WORK;
    ROLLBACK;
  
  EXCEPTION
    WHEN OTHERS THEN
      NULL;
      --p_exito := 0;
  END ENVIO_CORREO_ASIGROL;
--Envio Correo Asignado como Destinatario
 PROCEDURE ENVIO_CORREO_BLOQUEO(v_id_usuario IN NUMBER, V_Detalle in Varchar2)  
   IS
    L_HTML      CLOB;
    V_MENSAJE   CLOB;
    V_EXITO1    FLOAT;
    V_EXITO2    FLOAT;
    V_ASUNTO    VARCHAR2(2000);
    v_firma     VARCHAR2(255) := 'Sistema GESPO';
    v_cargo     VARCHAR2(255) := 'Policia Nacional de Colombia';
    v_correoenv VARCHAR2(255) := 'gespo@policia.gov.co';
  
  BEGIN
    FOR I IN (Select t.Id_Usuario,
       e.Funcionario,
       e.Nombregrado,
       e.Correo,
       e.Apellidos,
       e.Nombres,
       e.CargoActual Cargo,
       e.Fisica Siglafisica,
       e.Dependencia      
  From Ctr_Usuarios t, VM_CTR_EMPLEADOS_ALL@DBL2TELEPOL4DBOPER e
 Where t.Identificacion = e.Identificacion
   And t.Id_Usuario = v_id_usuario) LOOP
    
      V_MENSAJE := 'De manera atenta me permito informar, que su usuario ha cambiado el estado como '||V_Detalle||' en el Sistema Gestión del Servicio de Policía - GESPO:';
      V_ASUNTO  := 'Estado Sistema GESPO';
      BEGIN
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><img src="https://sinac.policia.gov.co:8443/herramientas/images/CabezoteCorreos.png" /></p>' ||
                  '<style type="text/css">.mitabla{border-collapse: collapse; width:75%; border-left: 1px solid #1e8cbe;border-right: 1px solid #1e8cbe;font-family:helvetica,arial,sans-serif;font-weight:normal;}.mitabla1 {background-color: #164e49;color:#fff;font-weight: normal;font-size:14px;padding: 8px;}.mitabla2 {margin-right:5px; margin-left:5px; valign:"middle"}.mitabla2 tr:nth-child(odd){background-color: #ffffff;} .mitabla2 tr:nth-child(even) {background-color: #eee;}</style>' ||
                  '<body>' || '<div>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,' || ' ' ||
                  TO_CHAR(SYSDATE,
                          'FMDay, DD "de" Month "de" YYYY',
                          'NLS_DATE_LANGUAGE=SPANISH') || '</div><br/>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Señor (a) ' ||INITCAP(I.NombreGrado) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' || I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.Cargo) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.SiglaFisica || ' - ' || I.Dependencia || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: ' ||V_ASUNTO || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Acceso: https://gespo.policia.gov.co:9105' || '</div><br/>' ||
                                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"></div>' ||
                  
                  '<p></p>' ||                 
                                   

                  '</body></html>';
      
        srvcorreo_235.envia_mail_html_new@DBL2TELEPOL4DBOPER(p_origen       => v_correoenv,
        p_destino      => I.Correo,
        p_asunto       => V_ASUNTO,
        p_mensaje      => L_HTML,
        p_confirmacion => V_EXITO1,
        p_error        => V_EXITO2
        );    
            
      
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;
    END LOOP;
    --p_exito := 1;
    COMMIT WORK;
    ROLLBACK;
  
  EXCEPTION
    WHEN OTHERS THEN
      NULL;
  END ENVIO_CORREO_BLOQUEO;
--Envio Correo retiro rol
 PROCEDURE ENVIO_CORREO_RETIROROL(v_id_usuario IN NUMBER, UserRolID in number, P_Justificacion In Varchar2)
  
   IS
    L_HTML      CLOB;
    V_MENSAJE   CLOB;
    V_EXITO1    FLOAT;
    V_EXITO2    FLOAT;
    V_ASUNTO    VARCHAR2(2000);
    v_firma     VARCHAR2(255) := 'Sistema GESPO';
    v_cargo     VARCHAR2(255) := 'Policia Nacional de Colombia';
    v_correoenv VARCHAR2(255) := 'gespo@policia.gov.co';
  
  BEGIN
    FOR I IN (SELECT T.ID_USUARIO,
                     E.Funcionario,
                     E.NombreGrado,
                     E.Correo,
                     E.APELLIDOS,
                     E.NOMBRES,
                     e.CargoActual Cargo,
                     E.Fisica SiglaFisica,
                     E.Dependencia,
                     r.Descripcion rol,
                     a.justificacion descripcion,
                     TO_CHAR(a.fecha_fin, 'dd/mm/yyyy') fecha_fin
                FROM ctr_usuarios          T,
                     VM_CTR_EMPLEADOS_ALL@DBL2TELEPOL4DBOPER E,                 
                     ctr_roles             r,
                     CTR_ROLES_USER_ADMIN  a
               WHERE t.identificacion = e.identificacion
                 and t.id_usuario = v_id_usuario
                 and a.id_roluser_admin = UserRolID
                 and a.id_usuario = t.id_usuario
                 and r.id_rol = a.id_rol) LOOP
    
      V_MENSAJE := 'De manera atenta me permito informar, que le ha sido retirado el rol que se relaciona a continuacion del Sistema Gestión del Servicio de Policía - GESPO:';
      V_ASUNTO  := 'Retiro Rol Sistema GESPO';
      BEGIN
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><img src="https://sinac.policia.gov.co:8443/herramientas/images/CabezoteCorreos.png" /></p>' ||
                  '<style type="text/css">.mitabla{border-collapse: collapse; width:75%; border-left: 1px solid #1e8cbe;border-right: 1px solid #1e8cbe;font-family:helvetica,arial,sans-serif;font-weight:normal;}.mitabla1 {background-color: #164e49;color:#fff;font-weight: normal;font-size:14px;padding: 8px;}.mitabla2 {margin-right:5px; margin-left:5px; valign:"middle"}.mitabla2 tr:nth-child(odd){background-color: #ffffff;} .mitabla2 tr:nth-child(even) {background-color: #eee;}</style>' ||
                  '<body>' || '<div>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,' || ' ' ||TO_CHAR(SYSDATE,'FMDay, DD "de" Month "de" YYYY','NLS_DATE_LANGUAGE=SPANISH') || '</div><br/>' ||
                 
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Señor (a) ' ||INITCAP(I.NombreGrado) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' || I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.Cargo) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.SiglaFisica || ' - ' || I.Dependencia || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: ' ||V_ASUNTO || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>' ||
                 
                 ---Tabla
                  '<table border=1 bordercolor=1e8cbe class="mitabla">' ||
                    '<thead class="mitabla1">' ||
                      '<th style="text-align: center;">NOMBRE DEL ROL</th>' ||
                      '<th>DETALLE ELIMINACION</th>' ||
                      '<th>FECHA CADUCIDAD</th>' || 
                    '</thead>' ||
                   
                    '<tbody class="mitabla2">' || 
                      '<tr>' || 
                        '<td>' || i.Rol ||'</td>' || 
                        '<td>' || P_Justificacion || '</td>' ||
                        '<td style="text-align: center;">' || i.fecha_fin ||'</td>' ||
                      '</tr>' || 
                    '</tbody>' || 
                  '</table><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>' ||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"></div>' ||
                 
                  '<p></p>' ||                                   
                  '</body></html>';
      
                    srvcorreo_235.envia_mail_html_new@DBL2TELEPOL4DBOPER(p_origen       => v_correoenv,
                                                                          p_destino      => I.Correo,
                                                                          p_asunto       => V_ASUNTO,
                                                                          p_mensaje      => L_HTML,
                                                                          p_confirmacion => V_EXITO1,
                                                                          p_error        => V_EXITO2
                                                                          );   
                                                                               
      
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;
    END LOOP;
    --p_exito := 1;
    COMMIT WORK;
    ROLLBACK;
  
  EXCEPTION
    WHEN OTHERS THEN
      NULL;
      --p_exito := 0;
  END ENVIO_CORREO_RETIROROL;

End PK_CORREO;
/
