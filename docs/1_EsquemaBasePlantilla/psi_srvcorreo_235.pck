CREATE OR REPLACE PACKAGE psi_srvcorreo_235 IS

	-- Author  : Subcomisario Ing HERNAN GARCIA BARRERA
	--           Funcionario Grupo Telem¿tica - OFITE
	-- Created : 05/04/2012 02:46:15 p.m.
	-- Purpose :

	-- PROCEDIMIENTO PARA ENVIAR CORREOS ELECTRONICOS
	PROCEDURE envia_mail(p_origen       IN VARCHAR2,
					 p_destino      IN VARCHAR2,
					 p_asunto       IN VARCHAR2,
					 p_mensaje      IN VARCHAR2,
					 p_confirmacion OUT NUMBER,
					 p_error        OUT VARCHAR2,
           p_copia        IN VARCHAR2 DEFAULT NULL);

	-- PROCEDIMIENTO PARA ENVIAR CORREOS ELECTRONICOS EN FORMATO HTML
	PROCEDURE envia_mail_html(p_origen       IN VARCHAR2,
					 p_destino      IN VARCHAR2,
					 p_asunto       IN VARCHAR2,
					 p_mensaje      IN VARCHAR2,
					 p_confirmacion OUT NUMBER,
					 p_error        OUT VARCHAR2,
           p_copia        IN VARCHAR2 DEFAULT NULL);


  -- PROCEDIMIENTO PARA ENVIAR CORREOS ELECTRONICOS EN FORMATO HTML
  PROCEDURE envia_mail_html_new(p_origen       IN VARCHAR2,
           p_destino      IN VARCHAR2,
           p_asunto       IN VARCHAR2,
           p_mensaje      IN VARCHAR2,
           p_confirmacion OUT NUMBER,
           p_error        OUT VARCHAR2,
           p_copia        IN VARCHAR2 DEFAULT NULL);

PROCEDURE send_mail                   (p_origen          IN VARCHAR2,
                                       p_destino        IN VARCHAR2,
                                       p_asunto     IN VARCHAR2,
                                       p_filename IN VARCHAR2 DEFAULT NULL,
                                       p_conten_type IN VARCHAR2 DEFAULT NULL,
                                       p_adjunto IN BLOB DEFAULT NULL,
                                       p_html  IN VARCHAR2 DEFAULT NULL                           
                                       );

PROCEDURE send_polis                  (p_origen          IN VARCHAR2,
                                       p_destino        IN VARCHAR2,
                                       p_asunto     IN VARCHAR2,
                                       p_filename IN VARCHAR2 DEFAULT NULL,
                                       p_conten_type IN VARCHAR2 DEFAULT NULL,
                                       p_adjunto IN BLOB DEFAULT NULL,
                                       p_html  IN VARCHAR2 DEFAULT NULL                                  
                                       );
                                                                              
PROCEDURE send_mail_hmtl             ( p_envia      IN VARCHAR2,
                                       p_destino    IN VARCHAR2,
                                       p_asunto   IN VARCHAR2,
                                       p_mensaje  IN VARCHAR2 DEFAULT NULL,
                                       p_html_msg  IN VARCHAR2 DEFAULT NULL)  ;

 

END psi_srvcorreo_235;
/
CREATE OR REPLACE PACKAGE BODY psi_srvcorreo_235 IS

PROCEDURE envia_mail(p_origen       IN VARCHAR2,
					 p_destino      IN VARCHAR2,
					 p_asunto       IN VARCHAR2,
					 p_mensaje      IN VARCHAR2,
					 p_confirmacion OUT NUMBER,
					 p_error        OUT VARCHAR2,
           p_copia        IN VARCHAR2 DEFAULT NULL) IS

        l_mailhost  VARCHAR2(255) := '172.28.9.235';
   --        l_mailhost  VARCHAR2(255) := '172.28.9.205';
     --     l_mailhost  VARCHAR2(255) := '192.168.2.97';
		      l_mail_conn utl_smtp.connection;
           l_boundary    VARCHAR2(50) := '----=*#abc1234321cba#*=';         
      

	BEGIN


  l_mail_conn := UTL_SMTP.open_connection(l_mailhost, 25);
  
  UTL_SMTP.helo(l_mail_conn, l_mailhost);
  UTL_SMTP.mail(l_mail_conn, p_origen);
  UTL_SMTP.rcpt(l_mail_conn, p_destino);

  UTL_SMTP.open_data(l_mail_conn);
  UTL_SMTP.WRITE_RAW_DATA(l_mail_conn,UTL_RAW.CAST_TO_RAW('MIME-Version: 1.0' || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Date: ' || TO_CHAR(SYSDATE, 'DD-MON-YYYY HH24:MI:SS') || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'To: ' || p_destino || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'From: ' || p_origen || UTL_TCP.crlf);
  UTL_SMTP.write_raw_data(l_mail_conn, UTL_RAW.CAST_TO_RAW('Subject: ' || p_asunto || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Reply-To: ' || p_copia || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'MIME-Version: 1.0' || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'Content-Type: multipart/mixed; boundary="' || l_boundary || '"' || UTL_TCP.crlf || UTL_TCP.crlf);
 
 IF p_mensaje IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/html; charset=iso-8859-1' || UTL_TCP.crlf || UTL_TCP.crlf);

    UTL_SMTP.WRITE_RAW_DATA(l_mail_conn, UTL_RAW.CAST_TO_RAW(p_mensaje));
    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;

     	 
  
		IF p_copia IS NOT NULL THEN
       utl_smtp.rcpt(l_mail_conn, trim(p_copia));
    END IF;
    
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || '--' || UTL_TCP.crlf);
    UTL_SMTP.close_data(l_mail_conn);
    UTL_SMTP.quit(l_mail_conn);

		p_confirmacion := 1;
		p_error        := '';
    COMMIT;

	EXCEPTION
		WHEN OTHERS THEN
			p_confirmacion := 0;
			p_error        := 'ERROR AL TRATAR DE ENVIAR EL MENSAJE EL CODIGO DEL ERROR ES: ' ||
						            SQLERRM;
	END envia_mail;


	PROCEDURE envia_mail_html(p_origen       IN VARCHAR2,
					 p_destino      IN VARCHAR2,
					 p_asunto       IN VARCHAR2,
					 p_mensaje      IN VARCHAR2,
					 p_confirmacion OUT NUMBER,
					 p_error        OUT VARCHAR2,
           p_copia        IN VARCHAR2 DEFAULT NULL) IS

          l_mailhost  VARCHAR2(255) := '172.28.9.235';

		l_mail_conn utl_smtp.connection;

		msg  CLOB;
		crlf VARCHAR2(2) := chr(13) || chr(10);    
	BEGIN
		l_mail_conn := utl_smtp.open_connection(l_mailhost, 25);

	  	msg := 'Date: ' || to_char(SYSDATE, 'dd/mm/yyyy hh24:mi:ss') || crlf ||
			  'From: <' || p_origen || '>' || crlf ||
        'Subject: ' ||p_asunto || crlf ||
        'To: ' || p_destino || crlf ||
        'Cc: '|| p_copia || utl_tcp.CRLF ||      
        'Content-Type: text/html; charset=iso-8859-1'|| utl_tcp.CRLF ||       
			  p_mensaje;

    utl_smtp.helo(l_mail_conn, l_mailhost);
		utl_smtp.mail(l_mail_conn, p_origen);
		utl_smtp.rcpt(l_mail_conn, p_destino);
		utl_smtp.open_data(l_mail_conn);
		utl_smtp.write_data(l_mail_conn, msg);
		utl_smtp.close_data(l_mail_conn);
		utl_smtp.quit(l_mail_conn);

		p_confirmacion := 1;
		p_error        := '';
    COMMIT;

	EXCEPTION
		WHEN OTHERS THEN
			p_confirmacion := 0;
			p_error        := 'ERROR AL TRATAR DE ENVIAR EL MENSAJE EL CODIGO DEL ERROR ES: ' ||
						   SQLERRM;
	END envia_mail_html;


  PROCEDURE envia_mail_html_new(p_origen       IN VARCHAR2,
                                p_destino      IN VARCHAR2,
                                p_asunto       IN VARCHAR2,
                                p_mensaje      IN VARCHAR2,
                                p_confirmacion OUT NUMBER,
                                p_error        OUT VARCHAR2,
                                p_copia        IN VARCHAR2 DEFAULT NULL) IS

      l_mailhost    VARCHAR2(255) := '172.28.9.235';

      l_mail_conn   utl_smtp.connection;

      l_boundary    VARCHAR2(50)  := '----=*#abc1234321cba#*=';

      msg_header    VARCHAR2(500) := '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">' ||
                                     '<html xmlns="http://www.w3.org/1999/xhtml">' ||
                                     '<head><meta http-equiv="content-type" content="text/html;charset=iso-8859-1" /></head>' ||
                                     '<html xmlns="http://www.w3.org/1999/xhtml">'||
                                     '<body>';

      msg_footer    VARCHAR2(20)  := ' </body>'||
                                     ' </html>';
      v_mensaje     CLOB;

  BEGIN
    l_mail_conn := utl_smtp.open_connection(l_mailhost, 25);


    v_mensaje := msg_header||
                 p_mensaje||
                 msg_footer;


    utl_smtp.helo(l_mail_conn, l_mailhost);
    utl_smtp.mail(l_mail_conn, p_origen);
    utl_smtp.rcpt(l_mail_conn, p_destino);
    IF p_copia IS NOT NULL THEN
       utl_smtp.rcpt(l_mail_conn, trim(p_copia));
    END IF;

    utl_smtp.open_data(l_mail_conn);


    UTL_SMTP.write_data(l_mail_conn, 'Date: ' || TO_CHAR(SYSDATE, 'DD-MON-YYYY HH24:MI:SS') || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'To: ' || p_destino || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'From: ' || p_origen || UTL_TCP.crlf);    
    UTL_SMTP.write_raw_data(l_mail_conn, UTL_RAW.CAST_TO_RAW('Subject: ' || p_asunto || UTL_TCP.crlf));    
    UTL_SMTP.write_data(l_mail_conn, 'Reply-To: ' || p_destino || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'MIME-Version: 1.0' || UTL_TCP.crlf);


    IF TRIM(p_copia) IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, 'CC: ' || REPLACE(p_copia, ',', ';') || UTL_TCP.crlf);
  END IF;


    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: multipart/alternative; boundary="' || l_boundary || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/html; charset=iso-8859-1"' || UTL_TCP.crlf || UTL_TCP.crlf);

--    UTL_SMTP.write_data(l_mail_conn, v_mensaje); -- En el parametro p_mensaje se recibe el codigo html a enviar
    
    UTL_SMTP.WRITE_RAW_DATA(l_mail_conn, UTL_RAW.CAST_TO_RAW(v_mensaje));



    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);


    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || '--' || UTL_TCP.crlf);
    UTL_SMTP.close_data(l_mail_conn);

    UTL_SMTP.quit(l_mail_conn);

    p_confirmacion := 1;
    p_error        := '';
    COMMIT;

  EXCEPTION
    WHEN OTHERS THEN
      p_confirmacion := 0;
      p_error        := 'ERROR AL TRATAR DE ENVIAR EL MENSAJE EL CODIGO DEL ERROR ES: ' ||SQLERRM;
  END envia_mail_html_new;

PROCEDURE send_mail                   (p_origen          IN VARCHAR2,
                                       p_destino        IN VARCHAR2,
                                       p_asunto     IN VARCHAR2,
                                       p_filename IN VARCHAR2 DEFAULT NULL,
                                       p_conten_type IN VARCHAR2 DEFAULT NULL,
                                       p_adjunto IN BLOB DEFAULT NULL,
                                       p_html  IN VARCHAR2 DEFAULT NULL
                                  --     p_confirmacion OUT NUMBER,
                                  --     p_error        OUT VARCHAR2
                                       )
IS
  l_mail_conn   UTL_SMTP.connection;
  p_smtp_host  VARCHAR2(255) := '172.28.9.235';
--  p_smtp_host  VARCHAR2(255) := '192.168.2.97';
  p_smtp_port  VARCHAR2(255) := '25';
  l_boundary    VARCHAR2(50) := '----=*#abc1234321cba#*=';
  l_step        PLS_INTEGER  := 12000; -- make sure you set a multiple of 3 not higher than 24573
BEGIN
  l_mail_conn := UTL_SMTP.open_connection(p_smtp_host, p_smtp_port);
  UTL_SMTP.helo(l_mail_conn, p_smtp_host);
  UTL_SMTP.mail(l_mail_conn, p_origen);
  UTL_SMTP.rcpt(l_mail_conn, p_destino);

  UTL_SMTP.open_data(l_mail_conn);
  UTL_SMTP.WRITE_RAW_DATA(l_mail_conn,UTL_RAW.CAST_TO_RAW('MIME-Version: 1.0' || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Date: ' || TO_CHAR(SYSDATE, 'DD-MON-YYYY HH24:MI:SS') || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'To: ' || p_destino || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'From: ' || p_origen || UTL_TCP.crlf);
  UTL_SMTP.write_raw_data(l_mail_conn, UTL_RAW.CAST_TO_RAW('Subject: ' || p_asunto || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Reply-To: ' || p_origen || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'MIME-Version: 1.0' || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'Content-Type: multipart/mixed; boundary="' || l_boundary || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

  IF p_html IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/html; charset=iso-8859-1' || UTL_TCP.crlf || UTL_TCP.crlf);

    UTL_SMTP.WRITE_RAW_DATA(l_mail_conn, UTL_RAW.CAST_TO_RAW(p_html));
    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;


  IF p_filename IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: ' || p_conten_type || '; name="' || p_filename || '"' || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Transfer-Encoding: base64' || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Disposition: attachment; filename="' || p_filename || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

    FOR i IN 0 .. TRUNC((DBMS_LOB.getlength(p_adjunto) - 1 )/l_step) LOOP
      UTL_SMTP.write_data(l_mail_conn, UTL_RAW.cast_to_varchar2(UTL_ENCODE.base64_encode(DBMS_LOB.substr(p_adjunto, l_step, i * l_step + 1))));
    END LOOP;

    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;

  UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || '--' || UTL_TCP.crlf);
  UTL_SMTP.close_data(l_mail_conn);

  UTL_SMTP.quit(l_mail_conn);
  COMMIT;

	EXCEPTION
		WHEN OTHERS THEN
		/*	p_confirmacion := 0;
			p_error        := 'ERROR AL TRATAR DE ENVIAR EL MENSAJE EL CODIGO DEL ERROR ES: ' ||
						            SQLERRM;*/
  NULL;
END;
----


PROCEDURE send_polis                   (p_origen          IN VARCHAR2,
                                       p_destino        IN VARCHAR2,
                                       p_asunto     IN VARCHAR2,
                                       p_filename IN VARCHAR2 DEFAULT NULL,
                                       p_conten_type IN VARCHAR2 DEFAULT NULL,
                                       p_adjunto IN BLOB DEFAULT NULL,
                                       p_html  IN VARCHAR2 DEFAULT NULL
                                  --     p_confirmacion OUT NUMBER,
                                  --     p_error        OUT VARCHAR2
                                       )
IS
  l_mail_conn   UTL_SMTP.connection;
--  p_smtp_host  VARCHAR2(255) := '172.28.9.235';
  p_smtp_host  VARCHAR2(255) := '192.168.2.97';
  p_smtp_port  VARCHAR2(255) := '25';
  l_boundary    VARCHAR2(50) := '----=*#abc1234321cba#*=';
  l_step        PLS_INTEGER  := 12000; -- make sure you set a multiple of 3 not higher than 24573
BEGIN
  l_mail_conn := UTL_SMTP.open_connection(p_smtp_host, p_smtp_port);
  UTL_SMTP.helo(l_mail_conn, p_smtp_host);
  UTL_SMTP.mail(l_mail_conn, p_origen);
  UTL_SMTP.rcpt(l_mail_conn, p_destino);

  UTL_SMTP.open_data(l_mail_conn);
       UTL_SMTP.WRITE_RAW_DATA(l_mail_conn,UTL_RAW.CAST_TO_RAW('MIME-Version: 1.0' || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Date: ' || TO_CHAR(SYSDATE, 'DD-MON-YYYY HH24:MI:SS') || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'To: ' || p_destino || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'From: ' || p_origen || UTL_TCP.crlf);
  UTL_SMTP.write_raw_data(l_mail_conn, UTL_RAW.CAST_TO_RAW('Subject: ' || p_asunto || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Reply-To: ' || p_origen || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'MIME-Version: 1.0' || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'Content-Type: multipart/mixed; boundary="' || l_boundary || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

  IF p_html IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/html; charset=iso-8859-1' || UTL_TCP.crlf || UTL_TCP.crlf);

    UTL_SMTP.WRITE_RAW_DATA(l_mail_conn, UTL_RAW.CAST_TO_RAW(p_html));
    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;


  IF p_filename IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: ' || p_conten_type || '; name="' || p_filename || '"' || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Transfer-Encoding: base64' || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Disposition: attachment; filename="' || p_filename || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

    FOR i IN 0 .. TRUNC((DBMS_LOB.getlength(p_adjunto) - 1 )/l_step) LOOP
      UTL_SMTP.write_data(l_mail_conn, UTL_RAW.cast_to_varchar2(UTL_ENCODE.base64_encode(DBMS_LOB.substr(p_adjunto, l_step, i * l_step + 1))));
    END LOOP;

    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;

  UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || '--' || UTL_TCP.crlf);
  UTL_SMTP.close_data(l_mail_conn);

  UTL_SMTP.quit(l_mail_conn);
  COMMIT;

  EXCEPTION
    WHEN OTHERS THEN
    /*  p_confirmacion := 0;
      p_error        := 'ERROR AL TRATAR DE ENVIAR EL MENSAJE EL CODIGO DEL ERROR ES: ' ||
                        SQLERRM;*/
  NULL;
END send_polis;


PROCEDURE send_mail_hmtl             ( p_envia      IN VARCHAR2,
                                       p_destino    IN VARCHAR2,
                                       p_asunto   IN VARCHAR2,
                                       p_mensaje  IN VARCHAR2 DEFAULT NULL,
                                       p_html_msg  IN VARCHAR2 DEFAULT NULL)

IS
  l_mail_conn   UTL_SMTP.connection;
  l_boundary    VARCHAR2(50) := '----=*#abc1234321cba#*=';
  p_smtp_host  VARCHAR2(255) := '172.28.9.235';
  p_smtp_port  VARCHAR2(255) := '25';


BEGIN
  l_mail_conn := UTL_SMTP.open_connection(p_smtp_host, p_smtp_port);
  UTL_SMTP.helo(l_mail_conn, p_smtp_host);
  UTL_SMTP.mail(l_mail_conn, p_envia);
  UTL_SMTP.rcpt(l_mail_conn, p_destino);

  UTL_SMTP.open_data(l_mail_conn);

  UTL_SMTP.write_data(l_mail_conn, 'Date: ' || TO_CHAR(SYSDATE, 'DD-MON-YYYY HH24:MI:SS') || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'To: ' || p_destino || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'From: ' || p_envia || UTL_TCP.crlf);  
  UTL_SMTP.write_raw_data(l_mail_conn, UTL_RAW.CAST_TO_RAW('Subject: ' || p_asunto || UTL_TCP.crlf));
  UTL_SMTP.write_data(l_mail_conn, 'Reply-To: ' || p_envia || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'MIME-Version: 1.0' || UTL_TCP.crlf);
  UTL_SMTP.write_data(l_mail_conn, 'Content-Type: multipart/alternative; boundary="' || l_boundary || '"' || UTL_TCP.crlf || UTL_TCP.crlf);

  IF p_mensaje IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/plain; charset="utf-8"' || UTL_TCP.crlf || UTL_TCP.crlf);

    UTL_SMTP.write_data(l_mail_conn, p_mensaje);
    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;

  IF p_html_msg IS NOT NULL THEN
    UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || UTL_TCP.crlf);
    UTL_SMTP.write_data(l_mail_conn, 'Content-Type: text/html; charset=iso-8859-1' || UTL_TCP.crlf || UTL_TCP.crlf);
    UTL_SMTP.WRITE_RAW_DATA(l_mail_conn, UTL_RAW.CAST_TO_RAW(p_html_msg));    
    
    UTL_SMTP.write_data(l_mail_conn, UTL_TCP.crlf || UTL_TCP.crlf);
  END IF;

  UTL_SMTP.write_data(l_mail_conn, '--' || l_boundary || '--' || UTL_TCP.crlf);
  UTL_SMTP.close_data(l_mail_conn);

  UTL_SMTP.quit(l_mail_conn);

END send_mail_hmtl;


END psi_srvcorreo_235;
/
