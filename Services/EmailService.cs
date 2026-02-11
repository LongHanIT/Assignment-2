using System.Net;
using System.Net.Mail;

namespace HarvestHavenSecurePortal.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"] ?? throw new InvalidOperationException("SMTP username missing");
            var appPassword = _config["Smtp:AppPassword"] ?? throw new InvalidOperationException("SMTP app password missing");
            var fromName = _config["Smtp:FromName"] ?? "Harvest Haven Security";
            var fromEmail = _config["Smtp:FromEmail"] ?? username;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, appPassword)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}
