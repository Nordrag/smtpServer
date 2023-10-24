using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;
using EmailData;
using MailKit.Security;

namespace smtpServer.Controllers
{
    [ApiController]
    [Route("Email")]
    public class EmailController : ControllerBase
    {
        readonly string? senderMail;
        readonly string? senderPw;
        readonly string? smtp;
        readonly string? senderName;
        readonly string? secret;
        readonly int port;

        public EmailController(IConfiguration config)
        {
            senderMail = config["senderMail"];
            senderPw = config["senderPw"];
            smtp = config["smtp"];
            port = int.Parse(config["port"]);
            senderName = config["senderName"];
            secret = config["secret"];
        }

        [HttpPost]
        public async Task<MailServerResponse> SendMail([FromBody] Email email)
        {
            MailServerResponse response = new();
            await Task.Run(() =>
            {
                try
                {
                    //get the auth header
                    var headers = HttpContext.Request.Headers.ToList();
                    var auth = headers.FirstOrDefault(x => x.Key == "Authorization");
                    var value = auth.Value[0];

                    //check for secret
                    if (!value.Contains(secret))
                    {
                        response.Success = false;
                        response.Response = "Authorization error";
                        return;
                    }

                    MimeMessage msg = new();
                    msg.Subject = email.Subject;
                    msg.From.Add(new MailboxAddress(senderName, senderMail));
                    msg.To.Add(new MailboxAddress(email.RecipientName, email.RecipientMail));

                    var body = new TextPart(email.ContentType)
                    {
                        Text = email.Content
                    };
                    var multi = new Multipart("mixed");
                    multi.Add(body);

                    if (email.Attachments != null)
                        foreach (var attachment in email.Attachments)
                        {

                            var part = new MimePart("application/octet-stream")
                            {
                                Content = new MimeContent(new MemoryStream(attachment.Item2)),
                                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                FileName = attachment.Item1
                            };
                            multi.Add(part);

                        }
                    msg.Body = multi;

                    using var client = new SmtpClient();
                    //Connect to the SMTP server
                    client.Connect(smtp, port, SecureSocketOptions.StartTls);

                    // Authenticate with the SMTP server using your Gmail credentials
                    client.Authenticate(senderMail, senderPw);

                    // Send the email
                    var responseTxt = client.Send(msg);

                    // Disconnect from the SMTP server
                    client.Disconnect(true);

                    response.Success = responseTxt.Contains("OK");
                    response.Response = responseTxt;
                }
                catch (Exception e)
                {
                    response.Success = false;
                    response.Response = e.Message;
                }


            });
            return response;
        }      

        [HttpPost("multi")]
        public async Task SendMails([FromBody] List<Email> emails)
        {

            await Task.Run(() =>
            {
                using var client = new SmtpClient();
                //Connect to the SMTP server
                client.Connect(smtp, port, SecureSocketOptions.StartTls);

                // Authenticate with the SMTP server using your Gmail credentials
                client.Authenticate(senderMail, senderPw);
                try
                {
                    //get the auth header
                    var headers = HttpContext.Request.Headers.ToList();
                    var auth = headers.FirstOrDefault(x => x.Key == "Authorization");
                    var value = auth.Value[0];

                    //check for secret
                    if (!value.Contains(secret))
                    {
                        return;
                    }

                    foreach (var email in emails)
                    {
                        MimeMessage msg = new();
                        msg.Subject = email.Subject;
                        msg.From.Add(new MailboxAddress(senderName, senderMail));
                        msg.To.Add(new MailboxAddress(email.RecipientName, email.RecipientMail));

                        var body = new TextPart(email.ContentType)
                        {
                            Text = email.Content
                        };
                        var multi = new Multipart("mixed");
                        multi.Add(body);

                        if (email.Attachments != null)
                            foreach (var attachment in email.Attachments)
                            {

                                var part = new MimePart("application/octet-stream")
                                {
                                    Content = new MimeContent(new MemoryStream(attachment.Item2)),
                                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                    FileName = attachment.Item1
                                };
                                multi.Add(part);

                            }
                        msg.Body = multi;

                        
                        // Send the email
                        client.Send(msg);                                                                
                    }
                   
                }
                catch (Exception e)
                {                  
                }
                client.Disconnect(true);
            });
        }
    }
}
