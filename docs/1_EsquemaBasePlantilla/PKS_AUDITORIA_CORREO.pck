CREATE OR REPLACE Package PKS_AUDITORIA_CORREO Is

-- Author  : EDWIN.SUAREZ
-- Created : 24/05/2022 02:49:27 p. m.
-- Purpose :   
-- Public type declarations

Procedure Ins_DocumentPostEmail(P_IdDocumentPost In Number,
                                  P_Radicado       In Varchar2,
                                  P_Email          In Varchar2,
                                  P_Messageid      In Varchar2,                                  
                                  P_Usuario        In Number,
                                  P_Maquina        In Varchar2,
                                  SRV_Message      Out Varchar2,
                                  P_Resultado      Out Number);

Procedure Ins_DocumentPostEmailClicks(P_IdDocumentPost In Number,
                                        P_Messageid      In Varchar2,
                                        P_Instant        In Number,
                                        P_IP             In Varchar2,
                                        P_ISP            In Varchar2,
                                        P_City           In Varchar2,
                                        P_Coordinates    In Varchar2,
                                        P_Country        In Varchar2,
                                        P_Client         In Varchar2,
                                        P_Device         In Varchar2,
                                        P_Url            In Varchar2,
                                        P_Usuario        In Number,
                                        P_Maquina        In Varchar2,
                                        SRV_Message      Out Varchar2,
                                        P_Resultado      Out Number); 

Procedure Ins_DocumentPostEmailOpenin(P_IdDocumentPost In Number,
                                        P_Messageid      In Varchar2,
                                        P_Instant        In Number,
                                        P_IP             In Varchar2,
                                        P_ISP            In Varchar2,
                                        P_City           In Varchar2,
                                        P_Coordinates    In Varchar2,
                                        P_Country        In Varchar2,
                                        P_Client         In Varchar2,
                                        P_Device         In Varchar2,                                        
                                        P_Usuario        In Number,
                                        P_Maquina        In Varchar2,
                                        SRV_Message      Out Varchar2,
                                        P_Resultado      Out Number);                                                                         

Procedure Udp_DocumentPostEmailExitosa(P_IdDocumentPost    In Number,
                                         P_Emailstatus       In Varchar2,
                                         P_Sent              In Number,
                                         P_Emailtype         In Varchar2,
                                         P_DeliveryReception In Number,
                                         P_DeliveryResponse  In Varchar2,
                                         P_DeliveryIp        In Varchar2,
                                         P_FirstOpeningTime  In Number,
                                         P_OpeningsCount     In Number,
                                         P_FirstLocation     In Varchar2,
                                         P_FirstIp           In Varchar2,
                                         P_FirstClickTime    In Number,
                                         P_ClicksCount       In Number,
                                         P_Url               In Varchar2,
                                         P_Messageid         In Varchar2,                                       
                                         P_Usuario           In Number,
                                         P_Maquina           In Varchar2,
                                         SRV_Message         Out Varchar2,
                                         P_Resultado         Out Number);  

Procedure Udp_DocumentPostEmailRebote(P_IdDocumentPost    In Number,
                                        P_Emailstatus       In Varchar2,
                                        P_Sent              In Number,
                                        P_BounceReception   In Number,
                                        P_BounceType        In Varchar2,
                                        P_BounceSubtype     In Varchar2,
                                        P_BounceRecipients  In Varchar2,
                                        P_BounceIp          In Varchar2,
                                        P_Message           In Varchar2,
                                        P_Messageid         In Varchar2,                                        
                                        P_Usuario           In Number,
                                        P_Maquina           In Varchar2,
                                        SRV_Message         Out Varchar2,
                                        P_Resultado         Out Number);

Procedure F_GetDocumentPostEmailCerts(P_IdDocumentPost In Number,
                                      C_1 Out Sys_Refcursor,
                                      C_2 Out Sys_Refcursor,
                                      C_3 Out Sys_Refcursor);
                                                                       
End PKS_AUDITORIA_CORREO;
/
CREATE OR REPLACE Package Body PKS_AUDITORIA_CORREO Is
--Insertar Auditoria correo certificado para particulares
  Procedure Ins_DocumentPostEmail(P_IdDocumentPost In Number,
                                  P_Radicado       In Varchar2,
                                  P_Email          In Varchar2,
                                  P_Messageid      In Varchar2,
                                  P_Usuario        In Number,
                                  P_Maquina        In Varchar2,
                                  SRV_Message      Out Varchar2,
                                  P_Resultado      Out Number)
  
   As
  Begin
    P_Resultado := 0;
    Begin
      Insert Into DOCUMENTOS_POST_EMAIL
        (id_email,
         id_document_post,
         Radicado,
         messageid,
         email,
         usuario_creacion,
         fecha_creacion,
         maquina_creacion)
      Values
        (DOCUMENTOS_POST_EMAIL_SEQ.NEXTVAL,
         P_IdDocumentPost,
         P_Radicado,
         P_Messageid,
         P_Email,
         P_Usuario,
         Sysdate,
         P_Maquina)
      Returning id_email Into P_Resultado;
      SRV_Message := 'El registro se grabó exitosamente';
      Commit Work;
    Exception
      When Others Then
        P_Resultado := 0;
        SRV_Message := 'Se presento un error durante la insercion del registro, ';
        SRV_Message := SRV_Message || 'Error reportado por la BD [' ||
                       TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
    End;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
    End If;
  End Ins_DocumentPostEmail;
--Evento para determinar la cantidad de descargas del documento adjunto  
  Procedure Ins_DocumentPostEmailClicks(P_IdDocumentPost In Number,
                                        P_Messageid      In Varchar2,
                                        P_Instant        In Number,
                                        P_IP             In Varchar2,
                                        P_ISP            In Varchar2,
                                        P_City           In Varchar2,
                                        P_Coordinates    In Varchar2,
                                        P_Country        In Varchar2,
                                        P_Client         In Varchar2,
                                        P_Device         In Varchar2,
                                        P_Url            In Varchar2,
                                        P_Usuario        In Number,
                                        P_Maquina        In Varchar2,
                                        SRV_Message      Out Varchar2,
                                        P_Resultado      Out Number)
  
   As
  Begin
    P_Resultado := 0;
    Begin
      Insert Into DOCUMENTOS_POST_EMAIL_clicks
        (        
        id_email_clicks, 
        id_document_post, 
        messageid, 
        instant, 
        ip, 
        isp, 
        city, 
        coordinates, 
        country, 
        client, 
        device,
        Usuario_Creacion, 
        fecha_creacion,
        Maquina_Creacion, 
        url        
        )
      Values
        (DOCUMENTOS_POST_EMAIL_clicks_SEQ.NEXTVAL,
         P_IdDocumentPost,
         P_Messageid,
         P_Instant,
         P_IP,
         P_ISP,
         P_City,
         P_Coordinates,
         P_Country,
         P_Client,
         P_Device,
         P_Usuario,
         Sysdate,
         P_Maquina,
         P_Url)
      Returning id_email_clicks Into P_Resultado;
      SRV_Message := 'El registro se grabó exitosamente';
      Commit Work;
    Exception
      When Others Then
        P_Resultado := 0;
        SRV_Message := 'Se presento un error durante la insercion del registro, ';
        SRV_Message := SRV_Message || 'Error reportado por la BD [' ||
                       TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
    End;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
    End If;
  End Ins_DocumentPostEmailClicks;
--Evento para determinar la cantidad de aperturas del correo enviado
  Procedure Ins_DocumentPostEmailOpenin(P_IdDocumentPost In Number,
                                        P_Messageid      In Varchar2,
                                        P_Instant        In Number,
                                        P_IP             In Varchar2,
                                        P_ISP            In Varchar2,
                                        P_City           In Varchar2,
                                        P_Coordinates    In Varchar2,
                                        P_Country        In Varchar2,
                                        P_Client         In Varchar2,
                                        P_Device         In Varchar2,                                        
                                        P_Usuario        In Number,
                                        P_Maquina        In Varchar2,
                                        SRV_Message      Out Varchar2,
                                        P_Resultado      Out Number)
  
   As
  Begin
    P_Resultado := 0;
    Begin
      Insert Into DOCUMENTOS_POST_EMAIL_openings
        (        
        id_email_openings , 
        id_document_post, 
        messageid, 
        instant, 
        ip, 
        isp, 
        city, 
        coordinates, 
        country, 
        client, 
        device,
        Usuario_Creacion, 
        fecha_creacion,
        Maquina_Creacion              
        )
      Values
        (DOCUMENTOS_POST_EMAIL_openings_SEQ.NEXTVAL,
         P_IdDocumentPost,
         P_Messageid,
         P_Instant,
         P_IP,
         P_ISP,
         P_City,
         P_Coordinates,
         P_Country,
         P_Client,
         P_Device,
         P_Usuario,
         Sysdate,
         P_Maquina
         )
      Returning id_email_openings Into P_Resultado;
      SRV_Message := 'El registro se grabó exitosamente';
      Commit Work;
    Exception
      When Others Then
        P_Resultado := 0;
        SRV_Message := 'Se presento un error durante la insercion del registro, ';
        SRV_Message := SRV_Message || 'Error reportado por la BD [' ||
                       TO_CHAR(Sqlcode) || '] ' || Sqlerrm;
        Goto ExitPaquete;
    End;
    <<ExitPaquete>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
    End If;
  End Ins_DocumentPostEmailOpenin;  
  
  --Actualizar auditoria del correo certificado entrega exitosa DELIVERED Entregado
  Procedure Udp_DocumentPostEmailExitosa(P_IdDocumentPost    In Number,
                                         P_Emailstatus       In Varchar2,
                                         P_Sent              In Number,
                                         P_Emailtype         In Varchar2,
                                         P_DeliveryReception In Number,
                                         P_DeliveryResponse  In Varchar2,
                                         P_DeliveryIp        In Varchar2,
                                         P_FirstOpeningTime  In Number,
                                         P_OpeningsCount     In Number,
                                         P_FirstLocation     In Varchar2,
                                         P_FirstIp           In Varchar2,
                                         P_FirstClickTime    In Number,
                                         P_ClicksCount       In Number,
                                         P_Url               In Varchar2,
                                         P_Messageid         In Varchar2,
                                         P_Usuario           In Number,
                                         P_Maquina           In Varchar2,
                                         SRV_Message         Out Varchar2,
                                         P_Resultado         Out Number) Is
  
  Begin
    P_Resultado := 0;
    Begin
      Update DOCUMENTOS_POST_EMAIL S
         Set s.emailstatus        = P_Emailstatus,
             s.sent               = P_Sent,
             s.emailtype          = P_Emailtype,
             s.delivery_reception = P_DeliveryReception,
             s.delivery_response  = P_DeliveryResponse,
             s.delivery_ip        = P_DeliveryIp,
             s.firstopeningtime   = P_FirstOpeningTime,
             s.openingscount      = P_OpeningsCount,
             s.firstlocation      = P_FirstLocation,
             s.firstip            = P_FirstIp,
             s.firstclicktime     = P_FirstClickTime,
             s.clickscount        = P_ClicksCount,
             s.Usuario_Modifica   = P_Usuario,
             s.fecha_modifica     = Sysdate,
             s.maquina_modifica   = P_Maquina,
             s.URL                = P_Url
       Where s.id_document_post   = P_IdDocumentPost
         and s.messageid          = P_Messageid;
      If Sql%Rowcount > 0 Then
        P_Resultado := 1;
        SRV_Message := 'Registro actualizado exitosamente';
      Else
        P_Resultado := 0;
      End If;
      Commit Work;
    Exception
      When NO_DATA_FOUND Then
        Goto eper_Exit;
    End;
    ----------    
    <<eper_Exit>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
    End If;
    ------     
  End Udp_DocumentPostEmailExitosa;
--Actualizar auditoria del correo certificado en caso de rebote
  Procedure Udp_DocumentPostEmailRebote(P_IdDocumentPost    In Number,
                                        P_Emailstatus       In Varchar2,
                                        P_Sent              In Number,
                                        P_BounceReception   In Number,
                                        P_BounceType        In Varchar2,
                                        P_BounceSubtype     In Varchar2,
                                        P_BounceRecipients  In Varchar2,
                                        P_BounceIp          In Varchar2,
                                        P_Message           In Varchar2,
                                        P_Messageid         In Varchar2,                                        
                                        P_Usuario           In Number,
                                        P_Maquina           In Varchar2,
                                        SRV_Message         Out Varchar2,
                                        P_Resultado         Out Number) Is
  
  Begin
    P_Resultado := 0;
    Begin
      Update DOCUMENTOS_POST_EMAIL S
         Set s.emailstatus        = P_Emailstatus,
             s.sent               = P_Sent,
             s.bounce_reception   = P_BounceReception,
             s.bounce_type        = P_BounceType,
             s.bounce_subtype     = P_BounceSubtype,
             s.bounce_ip          = P_BounceRecipients,
             s.bounce_recipients  = P_BounceIp,
             s.message            = P_Message,             
             s.Usuario_Modifica   = P_Usuario,
             s.fecha_modifica     = Sysdate,
             s.maquina_modifica   = P_Maquina
       Where s.id_document_post   = P_IdDocumentPost
         and s.messageid          = P_Messageid;
      If Sql%Rowcount > 0 Then
        P_Resultado := 1;
        SRV_Message := 'Registro actualizado exitosamente';
      Else
        P_Resultado := 0;
      End If;
      Commit Work;
    Exception
      When NO_DATA_FOUND Then
        Goto eper_Exit;
    End;
    ----------    
    <<eper_Exit>>
    If P_Resultado > 0 Then
      Commit;
    Else
      Rollback;
    End If;
    ------     
  End Udp_DocumentPostEmailRebote;  
--Obtener reporte general Auditoria entrega de correos a particulares
Procedure F_GetDocumentPostEmailCerts(P_IdDocumentPost In Number,
                                      C_1              Out Sys_Refcursor,
                                      C_2              Out Sys_Refcursor,
                                      C_3              Out Sys_Refcursor) As
Begin
  Begin
    Open C_1 For
      Select t.id_email IdEmail,
             t.id_document_post IdDocumentPost,
             t.Messageid,
             t.usuario_creacion UsuarioCreacion,
             to_char(t.fecha_creacion, 'dd/mm/yyyy hh24:mi') FechaCreacion,
             t.maquina_creacion MaquinaCreacion,
             t.Email,
             t.Radicado,
             decode(t.Emailstatus, 'DELIVERED', 'ENTREGADO', 'BOUNCED', 'REBOTE', 'COMPLAINT', 'QUEJA', 'OTTO', 'OTÓN', 'OPEN', 'ABIERTO' )Emailstatus,
             to_char(TO_DATE('01/01/1970', 'dd-mm-yy') + t.Sent / 86400,
                     'dd/MM/yyyy HH24:MI:SS') FechaEnvio,
             t.Emailtype,
             to_char(TO_DATE('01/01/1970', 'dd-mm-yy') +
                     t.delivery_reception / 86400,
                     'dd/MM/yyyy HH24:MI:SS') FechaEntrega,
             t.delivery_response DeliveryResponse,
             t.delivery_ip DeliveryIp,
             to_char(TO_DATE('01/01/1970', 'dd-mm-yy') +
                     t.firstopeningtime / 86400,
                     'dd/MM/yyyy HH24:MI:SS') FechaLectura,
             nvl(t.openingscount, 0) OpeningsCount,
             t.FirstLocation,
             t.FirstIp,
             to_char(TO_DATE('01/01/1970', 'dd-mm-yy') +
                     t.FirstClickTime / 86400,
                     'dd/MM/yyyy HH24:MI:SS') FechaDescarga,
             nvl(t.ClicksCount, 0) ClicksCount,
             to_char(TO_DATE('01/01/1970', 'dd-mm-yy') +
                     t.bounce_reception / 86400,
                     'dd/MM/yyyy HH24:MI:SS') FechaRebote,
             t.bounce_type BounceType,
             t.bounce_subtype BounceSubtype,
             t.bounce_recipients BounceRecipients,
             t.bounce_ip BounceIp,
             t.Message,
             to_char(t.fecha_modifica, 'dd/mm/yyyy hh24:mi') FechaSincroniza,
             t.usuario_modifica UsuarioSincroniza,
             t.maquina_modifica MaquinaSincroniza,
             nvl(t.Url, '0')Url
        From DOCUMENTOS_POST_EMAIL t
       Where t.id_document_post = P_IdDocumentPost;
    Rollback;
  Exception
    When Others Then
      Null;
      Rollback;
  End;
  Begin
    Open C_2 For
      Select to_char(TO_DATE('01/01/1970', 'dd-mm-yy') + t.instant / 86400,'dd/MM/yyyy HH24:MI:SS') FechaLectura,
             t.IP,
             t.ISP,
             t.City,
             t.Coordinates,
             t.Country,
             t.Client,
             t.Device,
             to_char(t.fecha_creacion, 'dd/mm/yyyy')FechaReporte
        From DOCUMENTOS_POST_EMAIL_openings t
       Where t.id_document_post = P_IdDocumentPost Order By t.instant Desc ;
    Rollback;
  Exception
    When Others Then
      Null;
      Rollback;
  End;
  Begin
    Open C_3 For
      Select to_char(TO_DATE('01/01/1970', 'dd-mm-yy') + t.instant / 86400, 'dd/MM/yyyy HH24:MI:SS') FechaDescarga,
             t.IP,
             t.ISP,
             t.City,
             t.Coordinates,
             t.Country,
             t.Client,
             t.Device,
             to_char(t.fecha_creacion, 'dd/mm/yyyy')FechaReporte
        From DOCUMENTOS_POST_EMAIL_clicks t
       Where t.id_document_post = P_IdDocumentPost Order By t.instant Desc ;
    Rollback;
  Exception
    When Others Then
      Null;
      Rollback;
  End;
End F_GetDocumentPostEmailCerts;

End PKS_AUDITORIA_CORREO;
/
