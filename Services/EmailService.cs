using System.Net;
using System.Net.Mail;

namespace SocialHelpDonation.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var user = _config["Smtp:Username"];
            var pass = _config["Smtp:Password"];
            var from = _config["Smtp:From"] ?? "noreply@donationsystem.com";

            if (string.IsNullOrEmpty(host))
            {
                // Development fallback: Log to console instead of sending
                _logger.LogInformation("--- DEVELOPMENT EMAIL LOG ---");
                _logger.LogInformation("To: {ToEmail}", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Message/Link: {Message}", message);
                _logger.LogInformation("-----------------------------");
                return;
            }

            if (!int.TryParse(portStr, out int port)) port = 587;

            using var client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(user, pass);
            client.EnableSsl = true;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(from),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
