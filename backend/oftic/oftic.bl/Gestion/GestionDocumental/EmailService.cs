using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using Comun.Dtos.GestionDocumental;
using Datos.Interfaz;
using Datos.Interfaz.GestionDocumental;
using Comun.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Negocio.Gestion.GestionDocumental
{
    public interface IEmailService
    {
        Task<DtoEnvioCorreoResult> EnviarCorreoAsync(DtoEnvioCorreoRequest request, string usuario, string maquina);
        List<DtoHistoricoCorreo> ConsultarHistorico();
        string GetSiglaSistema();
    }

    public class EmailService : IEmailService
    {
        private readonly IDbCuentaEmailRepository _cuentaRepo;
        private readonly IDbGestionCorreosRepository _gestionRepo;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IDbCuentaEmailRepository cuentaRepo, 
            IDbGestionCorreosRepository gestionRepo,
            ILogger<EmailService> logger)
        {
            _cuentaRepo = cuentaRepo;
            _gestionRepo = gestionRepo;
            _logger = logger;
        }

        public async Task<DtoEnvioCorreoResult> EnviarCorreoAsync(DtoEnvioCorreoRequest request, string usuario, string maquina)
        {
            try
            {
                // 0. Validar Clasificación Obligatoria
                if (!request.IdClasificacion.HasValue || request.IdClasificacion <= 0)
                {
                    return new DtoEnvioCorreoResult { Success = false, Message = "Debe seleccionar una clasificación de la información para enviar el correo." };
                }

                // 1. Obtener la configuración de la cuenta
                var cuenta = _cuentaRepo.ConsultarCuentas(request.IdCuentaEmail).FirstOrDefault();
                if (cuenta == null || cuenta.Vigente == 0)
                {
                    return new DtoEnvioCorreoResult { Success = false, Message = "La cuenta emisora no existe o no está vigente." };
                }

                // 2. Descifrar la contraseña
                string claveDescifrada = CryptoUtility.Decrypt(cuenta.ClaveSmtp);

                // 3. Generar Radicado Oficial
                string radicadoReal = _cuentaRepo.GetNextRadicado(request.IdCuentaEmail);

                // 4. Configurar el cliente SMTP
                using (var client = new SmtpClient(cuenta.ServidorSmtp, cuenta.Puerto))
                {
                    client.Credentials = new NetworkCredential(cuenta.UsuarioSmtp, claveDescifrada);
                    client.EnableSsl = cuenta.UsarSsl == 1;

                    // 5. Preparar el mensaje
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(cuenta.Email, cuenta.NombreCuenta),
                        Subject = request.Asunto,
                        IsBodyHtml = true
                    };

                    string cuerpoFinal = $@"<p style='font-family: Arial, sans-serif; font-size: 14px; margin-bottom: 20px;margin-top: 20px;'>
                                           Correo Electrónico  <b>No. {radicadoReal} </b> de fecha  <b>{DateTime.Now:dd/MM/yyyy}</b>
                                          </p>" + request.Cuerpo;

                    // Añadir Firma de la Cuenta (si existe)
                    if (!string.IsNullOrEmpty(cuenta.FirmaHtml))
                    {
                        cuerpoFinal += $"<br><br>{cuenta.FirmaHtml}";
                    }

                    // Añadir Clasificación al final absoluto (Centrada)
                    cuerpoFinal += $@"<br><p style='text-align: center; color: #000; font-family: Arial, sans-serif;'><b>{request.NombreClasificacion.ToUpper()}</b></p>";


                    // Obtener Sigla del Sistema Dinámica
                    string siglaSistema = _cuentaRepo.GetSiglaSistema();

                    // Pie de página de seguridad oficial
                    cuerpoFinal += $@"
                        <br>
                        <table style='width: 100%; border-top: 1px solid #e2e8f0; padding-top: 15px; font-family: Arial, sans-serif;'>
                            <tr>
                                <td style='font-size: 12px; color: #64748b;'>
                                    <p style='margin: 0;'><b>SEGURIDAD DE LA INFORMACIÓN:</b></p>
                                    <p style='margin: 5px 0;'>Este mensaje fue generado y enviado de forma segura a través del sistema {siglaSistema}.</p>
                                    <p style='margin: 0;'><b>ENVIADO POR:</b> {usuario.ToUpper()}</p>
                                    <p style='margin: 0;'><b>FECHA DE ENVÍO:</b> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                                </td>
                            </tr>
                        </table>";

                  
                    // --- PROCESAMIENTO DE IMÁGENES EMBEBIDAS (BASE64 TO CID) ---
                    var regex = new Regex(@"<img[^>]+src=""data:(image\/[^;]+);base64,([^""]+)""[^>]*>", RegexOptions.IgnoreCase);
                    var matches = regex.Matches(cuerpoFinal);
                    
                    if (matches.Count > 0)
                    {
                        int imgCount = 0;
                        var resView = AlternateView.CreateAlternateViewFromString(cuerpoFinal, null, MediaTypeNames.Text.Html);

                        foreach (Match match in matches)
                        {
                            try
                            {
                                string contentType = match.Groups[1].Value;
                                string base64Data = match.Groups[2].Value;
                                byte[] imageBytes = Convert.FromBase64String(base64Data);
                                
                                string contentId = $"img_{imgCount++}";
                                
                                // Crear recurso vinculado
                                var lr = new LinkedResource(new MemoryStream(imageBytes), contentType) 
                                { 
                                    ContentId = contentId 
                                };
                                resView.LinkedResources.Add(lr);
                                
                                // Reemplazar en el HTML para que apunte al CID
                                cuerpoFinal = cuerpoFinal.Replace(match.Value, $@"<img src=""cid:{contentId}"" style=""max-width: 100%;"" />");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error al procesar imagen embebida.");
                            }
                        }

                        // Re-creamos la vista con el HTML que ya tiene los CIDs y le pasamos los recursos
                        var finalView = AlternateView.CreateAlternateViewFromString(cuerpoFinal, null, MediaTypeNames.Text.Html);
                        foreach (var res in resView.LinkedResources)
                        {
                            finalView.LinkedResources.Add(res);
                        }
                        mailMessage.AlternateViews.Add(finalView);
                    }
                    else
                    {
                        mailMessage.Body = cuerpoFinal;
                        mailMessage.IsBodyHtml = true;
                    }

                    foreach (var dest in request.Para.Split(';', ','))
                    {
                        if (!string.IsNullOrWhiteSpace(dest))
                            mailMessage.To.Add(dest.Trim());
                    }

                    // 5. Gestionar Adjuntos
                    if (request.Adjuntos != null)
                    {
                        foreach (var file in request.Adjuntos)
                        {
                            if (file.Length > 0)
                            {
                                var attachment = new Attachment(file.OpenReadStream(), file.FileName);
                                mailMessage.Attachments.Add(attachment);
                            }
                        }
                    }

                    // 6. Enviar con Timeout Real y Logs
                    _logger.LogInformation("Intentando conectar con servidor SMTP: {Servidor}:{Puerto} (SSL: {SSL})", cuenta.ServidorSmtp, cuenta.Puerto, client.EnableSsl);
                    
                    // Forzar TLS 1.2 (Requerido por Office 365)
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                    // Timeout asíncrono real (60 segundos)
                    await client.SendMailAsync(mailMessage).WaitAsync(TimeSpan.FromSeconds(60));
                    
                    _logger.LogInformation("Conexión SMTP exitosa. Correo enviado.");
                }

                _logger.LogInformation("Correo enviado exitosamente desde {Email} para {Para}. Radicado: {Radicado}", cuenta.Email, request.Para, radicadoReal);
                
                // 7. Registrar en Base de Datos (Audit trail)
                var ok = _gestionRepo.RegistrarEnvio(request, radicadoReal, usuario, maquina);
                if (!ok)
                {
                    _logger.LogError("El correo se envió por SMTP pero falló el registro en la base de datos para el radicado {Radicado}", radicadoReal);
                    return new DtoEnvioCorreoResult 
                    { 
                        Success = false, 
                        Message = "El correo se envió, pero hubo un problema al guardarlo en el historial." 
                    };
                }

                return new DtoEnvioCorreoResult 
                { 
                    Success = true, 
                    Message = "Correo enviado y registrado correctamente.",
                    Radicado = radicadoReal 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo desde {Id}", request.IdCuentaEmail);
                return new DtoEnvioCorreoResult { Success = false, Message = $"Error SMTP: {ex.Message}" };
            }
        }

        public List<DtoHistoricoCorreo> ConsultarHistorico()
        {
            return _gestionRepo.ConsultarHistorico();
        }

        public string GetSiglaSistema()
        {
            return _cuentaRepo.GetSiglaSistema();
        }
    }
}
