using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using System.Net;
using System.Net.Mail;

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

            if (emailNotification.Subject != null)
            {
                emailNotification.Subject = emailNotification.Body != null && emailNotification.Body.Contains("Success")
                    ? "\u2705 " + emailNotification.Subject
                    : "\u274C " + emailNotification.Subject;
            }

            // Create and send email to multiple recipients
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(emailNotification.FromEmail);

            var emailRecipients = new List<string>();

            var emailTo = emailNotification.ToEmail?.Split(";").ToList();
            var emailCcList = emailNotification.ToCCEmail?.Split(";").ToList();
            var emailBccList = emailNotification.ToBCCEmail?.Split(";").ToList();

            if (!string.IsNullOrEmpty(emailNotification.ToEmail))
            {
                emailRecipients.AddRange(emailTo);
            }

            if (!string.IsNullOrEmpty(emailNotification.ToCCEmail))
            {
                emailRecipients.AddRange(emailCcList);
            }

            if (!string.IsNullOrEmpty(emailNotification.ToBCCEmail))
            {
                emailRecipients.AddRange(emailBccList);
            }

            foreach (var recipient in emailRecipients)
            {
                mailMessage.To.Add(recipient);
            }

            mailMessage.Subject = emailNotification.Subject;
            mailMessage.Body = emailNotification.Body;
            mailMessage.IsBodyHtml = emailNotification.IsHtmlBody;

            const int maxRetries = 3;
            const int retryIntervalSeconds = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                    return true; // Email sent successfully, exit method
                }
                catch (Exception)
                {
                    if (retryCount < maxRetries - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryIntervalSeconds));
                    }
                }

                retryCount++;
            }

            return false;
        }
    }
}