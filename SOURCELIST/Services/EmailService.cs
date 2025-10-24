using Microsoft.Extensions.Options;
using sourcelist.Models; 
using System.Net;
using System.Net.Mail;

namespace sourcelist.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            try
            {
                var client = new SmtpClient(_settings.Server, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.FromAddress, _settings.Password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.FromAddress, "Sourcelist System"),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}