create or replace package pk_correo_235 is

PROCEDURE ENVIO_CORREO ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT NUMBER,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_procedimiento IN NUMBER                        
                        );

PROCEDURE ENVIO_CORREO_DESDE ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT NUMBER,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2                        
                        ); 

PROCEDURE ENVIO_CORREO_ASU_VAR ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2,
                         v_asunto_envia IN VARCHAR2,
                         v_variable IN VARCHAR2                        
                        );                                               

PROCEDURE graba_mensaje ( p_mensaje IN CLOB,
                          p_asunto IN VARCHAR2,
                          p_usuario IN VARCHAR2,
                          p_maquina IN VARCHAR2,
                          p_id_mensaje OUT NUMBER ,
                          p_id_mensaje_mod IN NUMBER,
                          p_id_resultado OUT NUMBER,
                          p_sigla IN VARCHAR2,
                          v_firma IN VARCHAR2,
                          v_cargo IN VARCHAR                 
                         );
PROCEDURE graba_files( 
                            p_archivo      IN BLOB,
                            p_filename     IN VARCHAR2,
                            p_contenttype  IN VARCHAR2,
                            p_usuario_creacion IN VARCHAR2,
                            p_maquina_creacion  IN VARCHAR2,
                            p_descripcion       IN VARCHAR2,                                                        
                            p_id_soporte    OUT NUMBER,
                            p_id_mensaje IN NUMBER                            
                            ); 
PROCEDURE ELIMINAR_DOCUMENTO(p_id_soporte IN NUMBER, P_IDRESULTADO OUT NUMBER);   
PROCEDURE MAIL_TEST;    

PROCEDURE ENVIO_CORREO_POLIS ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,                         
                         p_exito OUT number,
                         v_envia IN VARCHAR2
                        );
                          
PROCEDURE ENVIO_CORREO_ENVIA ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2,
                         v_asunto_envia IN VARCHAR2,
                         v_variable IN VARCHAR2                       
                        ); 
PROCEDURE ENVIO_MENSAJE ( v_id_adjunto IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        );   
PROCEDURE ENVIO_MENSAJE_SIUTH ( v_id_adjunto IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        );   
                        
 PROCEDURE ENVIO_MENSAJE_COVID ( v_id_adjunto IN NUMBER,
                         V_CORREO IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        );                         
                        
                                                                                                              
end pk_correo_235;
/
create or replace package body pk_correo_235 is

PROCEDURE ENVIO_CORREO ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_procedimiento IN NUMBER                        
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_MENSAJE      CLOB;
V_ASUNTO       VARCHAR2(2000);
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);
v_elaboro      VARCHAR2(255);
v_firma        VARCHAR2(255);
v_cargo        VARCHAR2(255);  
v_correoenv    VARCHAR2(255);
BEGIN  

IF v_categoria > 0 THEN
FOR I IN (
        SELECT *
             FROM usr_rehu.VM_PERSONAL_AGRUPADO A 
             WHERE a.categoria = v_categoria  AND a.correo_electronico IS NOT NULL   
              --MODIFICACION REALIZADA ORDEN CR HOOVER LAVERDE COMENTARIO GECOP S-2018-040572-DIRAN
             AND A.IDENTIFICACION NOT IN (93375319)                       
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    BEGIN
            
 /*SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
              
    END;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
--                  '<p><img src="https://srvpqrs.policia.gov.co/descargas/images/pie_pagina_correos.png" /></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Señor (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;  
p_exito := 1;
COMMIT WORK;
ROLLBACK;

END IF;  

IF v_fisica = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.fisica = v_fisica AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
/*  SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
    SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
        
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_sigla_papa = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.Sigla_Papa = v_sigla_papa AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
 /* SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/

    SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
        
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_dependencia > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
/*    SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
    SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
        
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_dependencia_papa > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia_papa  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
/*    SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/

SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
        
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    
    BEGIN
      
/*   SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
    
    SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, 'psi_eva@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
                   
    
      BEGIN     
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                                    
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      

        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

IF v_procedimiento = 0 THEN
  NULL;
  ELSE
FOR I IN (
  ----No sirve no esta saliendo
select p.* from encuestas_empleados t, vm_personal_agrupado p
where t.empl_identificacion=p.identificacion and t.encuesta_consecutivo=541
and t.completa=0

      
/*select * from vm_personal_agrupado a
where a.categoria in (1,2,4,5,6) and a.sigla_papa not in ('COEST', 'OFITE') AND A.FISICA NOT IN ('COEST', 'OFITE') AND A.GRAD_ALFABETICO NOT IN ('GR', 'MG', 'BG')*/
      
/*SELECT e.* FROM empleados_encuestas en, vm_personal_agrupado e WHERE en.encuesta_consecutivo=441 AND       
e.consecutivo=en.empl_consecutivo AND e.unde_consecutivo=en.empl_unde_consecutivo AND e.unde_fuerza = en.empl_unde_fuerza AND e.categoria IN (5,6)*/
 ---1 Oficiales, 
 ---2 Suboficiales, 
 ---4 Agentes Patrulleros, 
 ---5 NU, 
 ---6 ALumnos, 
 ---7 Auxiliares de Policia     
           
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    
    BEGIN
      
 /*  SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
     SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, 'psi_eva@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
                   
    
      BEGIN     
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                                    
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      

        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 


COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_CORREO;


PROCEDURE ENVIO_CORREO_DESDE (  v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2                      
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_MENSAJE      CLOB;
V_ASUNTO       VARCHAR2(2000);
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);
v_firma        VARCHAR2(255);
v_cargo        VARCHAR2(255);  
BEGIN  

IF v_categoria > 0 THEN
FOR I IN (
        SELECT *
             FROM usr_rehu.VM_PERSONAL_AGRUPADO A 
             WHERE a.categoria = v_categoria  AND a.correo_electronico IS NOT NULL                  
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    BEGIN
            
 SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo 
   FROM PORTAL_MSJ_CORREOS U--, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
    --AND E.Identificacion = i.identificacion;
              
    END;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;  
p_exito := 1;
COMMIT WORK;
ROLLBACK;

END IF;  

IF v_fisica = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.fisica = v_fisica AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
  SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo 
   FROM PORTAL_MSJ_CORREOS U--, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
   -- AND E.Identificacion = i.identificacion;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_sigla_papa = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.Sigla_Papa = v_sigla_papa AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
  SELECT U.MENSAJE, U.ASUNTO,u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo
   FROM PORTAL_MSJ_CORREOS U---, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
   -- AND E.Identificacion = i.identificacion;

    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_dependencia > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO,  v_firma, v_cargo 
   FROM PORTAL_MSJ_CORREOS U--, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
    --AND E.Identificacion = i.identificacion;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_dependencia_papa > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia_papa  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
    SELECT U.MENSAJE, U.ASUNTO,  u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO,  v_firma, v_cargo 
   FROM PORTAL_MSJ_CORREOS U--, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
   -- AND E.Identificacion = i.identificacion;

    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    
    BEGIN
   SELECT U.MENSAJE, U.ASUNTO,  u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo
   FROM PORTAL_MSJ_CORREOS U--, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje;
   -- AND E.Identificacion = i.identificacion;
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO,  u.firma, u.cargo
   INTO V_MENSAJE, V_ASUNTO,  v_firma, v_cargo 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
    
    
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE ||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correo_envia,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_CORREO_DESDE;


PROCEDURE ENVIO_CORREO_ASU_VAR ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2,
                         v_asunto_envia IN VARCHAR2,
                         v_variable IN VARCHAR2                       
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_MENSAJE      CLOB;
V_ASUNTO       VARCHAR2(2000);
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);
v_elaboro      VARCHAR2(255);
v_firma        VARCHAR2(255);
v_cargo        VARCHAR2(255);  
v_correoenv    VARCHAR2(255);
BEGIN  

IF v_categoria > 0 THEN
FOR I IN (
        SELECT *
             FROM usr_rehu.VM_PERSONAL_AGRUPADO A 
             WHERE a.categoria = v_categoria  AND a.correo_electronico IS NOT NULL                  
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    BEGIN
            
/* SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
              
     SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
    END;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;  
p_exito := 1;
COMMIT WORK;
ROLLBACK;

END IF;  

IF v_fisica = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.fisica = v_fisica AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
/*  SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
      SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_sigla_papa = 'NO' THEN
  NULL; 
  ELSE    
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A   
             WHERE a.Sigla_Papa = v_sigla_papa AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
/*  SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
      SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                   
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;  

IF v_dependencia > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
   /* SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
          SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.NOMBRE_GRADO) || ' ' || I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_dependencia_papa > 0 THEN
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE A.UNDE_CONSECUTIVO_LABORANDO = v_dependencia_papa  AND a.correo_electronico IS NOT NULL                      
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;

    END;

    
/*    SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
          SELECT U.MENSAJE, U.ASUNTO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;

    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">' ||V_MENSAJE || '</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;

      END;    
  END LOOP;

p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF;

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    
    BEGIN
      
/*   SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
          SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, 'psi_eva@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
    
    
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||v_asunto_envia || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE ||' ' ||v_variable||'</div><br/>'||

                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">Cordial Saludo,'||' '||'</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_envia|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correo_envia,
                                    I.CORREO_ELECTRONICO,
                                    v_asunto_envia,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_CORREO_ASU_VAR;


PROCEDURE graba_mensaje ( p_mensaje IN CLOB,
                          p_asunto IN VARCHAR2,
                          p_usuario IN VARCHAR2,
                          p_maquina IN VARCHAR2,
                          p_id_mensaje OUT NUMBER,
                          p_id_mensaje_mod IN NUMBER,
                          p_id_resultado OUT NUMBER,
                          p_sigla IN VARCHAR2,
                          v_firma IN VARCHAR2,
                          v_cargo IN VARCHAR                  
                         )
IS
v_consecutivo   NUMBER :=0;                         
v_existe        NUMBER :=0;
BEGIN

BEGIN
  SELECT COUNT(*) INTO v_existe FROM portal_msj_correos c
  WHERE c.id_mensaje=p_id_mensaje_mod;
  END;
  
  IF v_existe > 0 THEN
    
  BEGIN
    UPDATE portal_msj_correos h SET
    h.mensaje = p_mensaje,
    h.asunto = p_asunto,
    h.usuario_modifica = p_usuario,
    h.maquina_modifica = p_maquina,
    h.fecha_modifica = SYSDATE,
    h.sigla = p_sigla,
    H.FIRMA = V_FIRMA,
    H.CARGO = V_CARGO
    WHERE h.id_mensaje=p_id_mensaje_mod;
    COMMIT WORK;
    p_id_resultado := 1;  
    p_id_mensaje := p_id_mensaje_mod;  
    END;
  ELSE
  BEGIN
    SELECT portal_correos_seq.nextval INTO v_consecutivo FROM dual;

  INSERT INTO portal_msj_correos
  (
  id_mensaje,
  mensaje,
  asunto,
  fecha_creacion,
  usuario_creacion,
  maquina_creacion,
  sigla,
  FIRMA,
  CARGO
  )
  VALUES
  (
  v_consecutivo,
  p_mensaje,
  p_asunto,
  SYSDATE,
  p_usuario,
  p_maquina,
  p_sigla,
  V_FIRMA,
  V_CARGO
  );
  COMMIT WORK;
  p_id_mensaje := v_consecutivo; 
  p_id_resultado := 1;                         
EXCEPTION
  WHEN OTHERS THEN
    p_id_mensaje := 0;
    p_id_resultado := 0;
    END;    
    END IF; 
    END graba_mensaje; 

PROCEDURE graba_files( 
                            p_archivo      IN BLOB,
                            p_filename     IN VARCHAR2,
                            p_contenttype  IN VARCHAR2,
                            p_usuario_creacion IN VARCHAR2,
                            p_maquina_creacion  IN VARCHAR2,
                            p_descripcion       IN VARCHAR2,                                                        
                            p_id_soporte    OUT NUMBER,
                            p_id_mensaje IN NUMBER                         
                            )
                            
IS
v_consecutivo   NUMBER :=0;                         
                         
                         
                             
BEGIN
  
SELECT portal_soportes_seq.nextval INTO v_consecutivo FROM dual;  
INSERT INTO 
            portal_soportes (
            id_soporte ,           
            archivo ,
            filename,
            contenttype,
            usuario_creacion,
            fecha_creacion,
            maquina_creacion,
            descripcion,
            id_mensaje                    
            )
VALUES
            (
            v_consecutivo,           
            p_archivo,
            p_filename,
            p_contenttype,
            p_usuario_creacion,
            SYSDATE,
            p_maquina_creacion ,
            upper(p_descripcion),
            p_id_mensaje
            ); 
            p_id_soporte:=v_consecutivo;
COMMIT WORK;
EXCEPTION
  WHEN OTHERS  THEN
            p_id_soporte:=0 ;               
END graba_files; 

PROCEDURE ELIMINAR_DOCUMENTO(p_id_soporte IN NUMBER, P_IDRESULTADO OUT NUMBER) IS
BEGIN
  DELETE portal_soportes D WHERE D.ID_SOPORTE = p_id_soporte; 
  COMMIT WORK;
  P_IDRESULTADO := 1;
EXCEPTION
  WHEN OTHERS THEN
    P_IDRESULTADO := 0;
END ELIMINAR_DOCUMENTO;

PROCEDURE MAIL_TEST IS
  lv_server      VARCHAR2(255) := '172.28.9.235';
  lv_rcpt        VARCHAR2(255) := 'edwin.suarez@policia.gov.co';
  lv_from        VARCHAR2(255) := 'edwin.suarez@policia.gov.co';
  lv_subject     VARCHAR2(255) := 'Le chien paresseux a sauté';
  lv_message     VARCHAR2(255) := 'Voix ambiguë d''un coeur qui au zéphyr préfère les jattes de kiwis';
  lv_conn        UTL_SMTP.CONNECTION;
BEGIN
  lv_Conn := UTL_SMTP.Open_Connection(lv_server,25);
  UTL_SMTP.Helo(lv_conn, lv_server);
  UTL_SMTP.Mail(lv_conn, lv_from);
  UTL_SMTP.Rcpt(lv_conn, lv_rcpt);
  UTL_SMTP.OPEN_DATA(lv_conn);
  UTL_SMTP.WRITE_DATA(lv_conn, 'Subject: =?UTF-8?Q?' ||
                                UTL_RAW.CAST_TO_VARCHAR2(UTL_ENCODE.QUOTED_PRINTABLE_ENCODE(UTL_RAW.CAST_TO_RAW(lv_subject))) ||
                                '?=' || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'MIME-version: 1.0' || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'Content-Type: text/html;charset=utf-8' || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'Content-Transfer-Encoding: quoted-printable '|| UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'Date: ' || TO_CHAR(SYSDATE, 'dd Mon yy hh24:mi:ss' ) ||' -0800 (GMT)' || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'From: ' || lv_from || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn, 'To: ' || lv_rcpt || UTL_TCP.CRLF);
  UTL_SMTP.WRITE_DATA(lv_conn,  UTL_TCP.CRLF);
  UTL_SMTP.WRITE_RAW_DATA(lv_conn, UTL_ENCODE.QUOTED_PRINTABLE_ENCODE(UTL_RAW.CAST_TO_RAW(lv_message)));
  UTL_SMTP.WRITE_DATA(lv_conn, UTL_TCP.CRLF);
  UTL_SMTP.CLOSE_DATA(lv_conn);
  UTL_SMTP.QUIT(lv_conn);
END MAIL_TEST;

PROCEDURE ENVIO_CORREO_POLIS ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,                         
                         p_exito OUT number,
                         v_envia IN VARCHAR2
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_MENSAJE      CLOB;
V_ASUNTO       VARCHAR2(2000);
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);
v_elaboro      VARCHAR2(255);
v_firma        VARCHAR2(255);
v_cargo        VARCHAR2(255);  
v_correoenv    VARCHAR2(255);
BEGIN  

FOR I IN (        
       -- select * from APP_REGIS_POLIS@dblsivicc2 j where j.identificacion in (80144332,79955967)
       select * from vm_personal_agrupado j where j.identificacion in (80144332,79955967)
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    BEGIN
   SELECT U.MENSAJE, U.ASUNTO, 'APP POLIS', 'Direcci&oacute;n Geneal', 'Polic&iacute;a Nacional de Colombia', 'soporte.polis@correo.policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;   
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO, 'POLIS', 'Dirección General', 'Policía Nacional de Colombia', 'soporte.polis@correo.policia.gov.co'
        INTO   V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
        FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
        
    
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) : </div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES|| ' ' ||' '||i.nombres||' '||I.APELLIDOS||' '||i.apellidos|| '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.DIRECCION || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;"> Usuario APP POLIS </div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE ||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  --'<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.send_polis(v_correoenv,
                                    I.Correo_Electronico,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;

EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_CORREO_POLIS;

PROCEDURE ENVIO_CORREO_ENVIA ( v_id_adjunto IN NUMBER,
                         v_id_mensaje IN NUMBER,
                         v_categoria IN NUMBER,
                         v_fisica IN VARCHAR2,
                         v_dependencia IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         v_sigla_papa IN VARCHAR2,
                         v_dependencia_papa IN NUMBER,
                         v_correo_envia IN VARCHAR2,
                         v_asunto_envia IN VARCHAR2,
                         v_variable IN VARCHAR2                       
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_MENSAJE      CLOB;
V_ASUNTO       VARCHAR2(2000);
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);
v_elaboro      VARCHAR2(255);
v_firma        VARCHAR2(255);
v_cargo        VARCHAR2(255);  
v_correoenv    VARCHAR2(255);
BEGIN  

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
              --MODIFICACION REALIZADA ORDEN CR HOOVER LAVERDE COMENTARIO GECOP S-2018-040572-DIRAN
             AND A.IDENTIFICACION NOT IN (93375319)        
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
    
    
    BEGIN
  /* SELECT U.MENSAJE, U.ASUNTO, E.FISICA || '-' ||e.grad_alfabetico||' '||initcap(e.nombres)||' '||initcap(e.apellidos) CREADO, u.firma, u.cargo, lower(e.fisica)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U, vm_personal_agrupado_psi E
  WHERE U.ID_MENSAJE = v_id_mensaje
    AND E.Identificacion = i.identificacion;*/
    
       SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, lower(u.sigla)||'@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo, v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;
    
    EXCEPTION
      WHEN NO_DATA_FOUND THEN
        SELECT U.MENSAJE, U.ASUNTO, 'PSI' CREADO, u.firma, u.cargo, 'psi_eva@policia.gov.co'
   INTO V_MENSAJE, V_ASUNTO, V_ELABORO, v_firma, v_cargo,v_correoenv 
   FROM PORTAL_MSJ_CORREOS U
  WHERE U.ID_MENSAJE = v_id_mensaje;    
    END;
       
      BEGIN              
        L_HTML := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=utf-8" /></head>' ||
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<body>' || 
                  '<div>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.CARGO_ACTUAL || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||v_asunto_envia || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE ||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_envia || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||INITCAP(v_variable) || '</div><br/>'||                 
                  
       
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: PSI-EVA </div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado por: PSI POLICIA NACIONAL DE COLOMBIA </div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correo_envia,
                                    I.CORREO_ELECTRONICO,
                                    v_asunto_envia,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_CORREO_ENVIA;

PROCEDURE ENVIO_MENSAJE ( v_id_adjunto IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);  
BEGIN  

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM usr_rehu.VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
              --MODIFICACION REALIZADA ORDEN CR HOOVER LAVERDE COMENTARIO GECOP S-2018-040572-DIRAN
             AND A.IDENTIFICACION NOT IN (93375319)        
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
         
                   
    
      BEGIN     


        L_HTML := '<!DOCTYPE html>' ||
                  
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=iso-8859-1" /></head>' ||
                  '<body>' || 
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<div>' ||
                                    
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Atentamente,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_MENSAJE;

PROCEDURE ENVIO_MENSAJE_SIUTH ( v_id_adjunto IN NUMBER,
                         v_personaliza IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);  
BEGIN  

IF v_personaliza = 'NO' THEN
  NULL;
  ELSE
FOR I IN (
        SELECT *
             FROM usr_rehu.VM_PERSONAL_AGRUPADO A
             WHERE a.identificacion IN (v_personaliza) AND a.correo_electronico IS NOT NULL  
              --MODIFICACION REALIZADA ORDEN CR HOOVER LAVERDE COMENTARIO GECOP S-2018-040572-DIRAN
             AND A.IDENTIFICACION NOT IN (93375319)        
      ) LOOP
  
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
         
                   
    
      BEGIN     


        L_HTML := '<!DOCTYPE html>' ||
                  
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=iso-8859-1" /></head>' ||
                  '<body>' || 
                  '<p><center><img src="https://srvpsi.policia.gov.co/Descargas/descargas/images/siuth.png" /></center></p>' ||
                  '<div>' ||
                                    
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">Se&ntilde;or (a) '||INITCAP(I.NOMBRE_GRADO)||'</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.NOMBRES || ' ' ||I.APELLIDOS || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||INITCAP(I.CARGO_ACTUAL) || '</div>' ||
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">' ||I.FISICA || ' - ' || I.DESCRIPCION_DEPENDENCIA ||'</div><br/>' ||
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Cordial Saludo,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    I.CORREO_ELECTRONICO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    
  END LOOP;
p_exito := 1;
COMMIT WORK;
ROLLBACK;
END IF; 

COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_MENSAJE_SIUTH;

PROCEDURE ENVIO_MENSAJE_COVID ( v_id_adjunto IN NUMBER,
                         V_CORREO IN VARCHAR2,
                         p_exito OUT number,
                         v_envia IN VARCHAR2,
                         V_MENSAJE CLOB,
                         V_ASUNTO IN VARCHAR2,
                         v_correoenv in varchar2,
                         v_firma in varchar2,
                         v_cargo in varchar2,
                         v_elaboro in varchar2                         
                        )
  
IS
V_ADJUNTO1     BLOB;
L_HTML         CLOB;
V_ADJUNTO      BLOB;
V_FILENAME     VARCHAR2(255);
V_CONTEN_TYPE  VARCHAR2(255);  
BEGIN  

 
    BEGIN
       SELECT J.ARCHIVO, J.FILENAME, J.CONTENTTYPE
        INTO V_ADJUNTO1, V_FILENAME, V_CONTEN_TYPE
        FROM PORTAL_SOPORTES J
       WHERE J.ID_SOPORTE = v_id_adjunto;
    END;
         
                   
    
      BEGIN     


        L_HTML := '<!DOCTYPE html>' ||
                  
                  '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                  '<head><meta http-equiv="content-type" content="text/html;charset=iso-8859-1" /></head>' ||
                  '<body>' || 
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/inicio-ofite.png" /></center></p>' ||
                  '<div>' ||
                                    
                  '<div style="font-size:15px; font-family:Arial; font-style:italic; color:Black; font-weight:bold;">BOGOTA D.C.,'||' '||TO_CHAR (SYSDATE,'FMDay, DD "de" Month "de" YYYY', 'NLS_DATE_LANGUAGE=SPANISH')||'</div><br/>' ||
                  
                
                 
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">Asunto: '||V_ASUNTO || '</div><br/>'||
        
                  '<div style="font-size:16px; font-family:Arial; color:Black; text-align:justify; font-weight:normal;">'||V_MENSAJE||'</div><br/>'||
                  
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||'Atentamente,' || '</div><br/>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_firma || '</div>'||
                  '<div style="font-size:16px; font-family:Arial; color:Black; font-weight:normal;">' ||v_cargo || '</div><br/>'||                 
                  
       
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Elaboro: '||v_elaboro|| '</div>' ||
                  '<div style="font-size:7px; font-family:Arial; font-style:italic; color:Black; font-weight:normal;"> Enviado: '||v_envia|| '</div>' ||         
                  '<p><center><img src="https://psi.policia.gov.co/Descargas/descargas/images/final-ofite.png" /></center></p>' ||
                  '</body></html>';
      
        V_ADJUNTO := V_ADJUNTO1;
        psi_srvcorreo_235.SEND_MAIL(v_correoenv,
                                    V_CORREO,
                                    V_ASUNTO,                                    
                                    V_FILENAME,
                                    V_CONTEN_TYPE,
                                    V_ADJUNTO,
                                    L_HTML);
      EXCEPTION
        WHEN OTHERS THEN
          NULL;
      END;    

p_exito := 1;
COMMIT WORK;
ROLLBACK;


COMMIT WORK;
ROLLBACK;
EXCEPTION
  WHEN OTHERS THEN
   p_exito := 0;
END ENVIO_MENSAJE_COVID;

end pk_correo_235;
/
