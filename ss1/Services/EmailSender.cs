using System.Net;
using System.Net.Mail;
using ss1.Interfaces;

namespace ss1.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var fromAddress = new MailAddress("diplomka41ip@gmail.com", "Photo Service");
            var toAddress = new MailAddress(to);
            const string fromPassword = "vnbz usun rcwr etpy"; // ← встав сюди App Password

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                Timeout = 20000
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}
