using FenrisWebsite.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace FenrisWebsite.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(EmailForm emailForm)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
            var smtpUserEmail = _configuration["EmailSettings:SmtpUserEmail"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(smtpUserEmail!, "Fenris Block");
                mailMessage.Subject = emailForm.Subject;
                mailMessage.Body = emailForm.Message;
                mailMessage.IsBodyHtml = true;

                mailMessage.To.Add(smtpUserEmail!);
                if (!string.IsNullOrWhiteSpace(emailForm.UserEmail))
                {
                    mailMessage.CC.Add(emailForm.UserEmail); 
                }

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUserEmail, smtpPassword);
                    smtpClient.EnableSsl = true;

                    try
                    {
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to send email.", ex);
                    }
                }
            }
        }
    }
}
