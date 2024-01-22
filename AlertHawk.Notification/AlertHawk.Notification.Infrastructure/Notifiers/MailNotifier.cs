using System.Net;
using System.Net.Mail;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class MailNotifier : IMailNotifier
    {
        public async Task<bool> Send(NotificationEmail emailNotification)
        {
            // Set up SMTP client
            var smtpClient = new SmtpClient(emailNotification.Hostname);
            smtpClient.Port = 587;
            smtpClient.Credentials = new NetworkCredential(emailNotification.Username, emailNotification.Password);
            smtpClient.EnableSsl = true;

            // Create and send email to multiple recipients
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(emailNotification.FromEmail);

            var emailRecipients = new List<string>();

            var emailTo = emailNotification.ToEmail.Split(";").ToList();
            var emailCCList = emailNotification.ToCCEmail.Split(";").ToList();
            var emailBCCList = emailNotification.ToBCCEmail.Split(";").ToList();

            emailRecipients.AddRange(emailTo);
            emailRecipients.AddRange(emailCCList);
            emailRecipients.AddRange(emailBCCList);

            foreach (var recipient in emailRecipients)
            {
                mailMessage.To.Add(recipient);
            }

            mailMessage.Subject = emailNotification.Subject;
            mailMessage.Body = emailNotification.Body;
            mailMessage.IsBodyHtml = emailNotification.IsHtmlBody;

            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
    }
}