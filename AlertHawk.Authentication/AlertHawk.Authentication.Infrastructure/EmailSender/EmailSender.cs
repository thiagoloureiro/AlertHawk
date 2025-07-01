using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mail;

namespace AlertHawk.Authentication.Infrastructure.EmailSender;

[ExcludeFromCodeCoverage]
public static class EmailSender
{
    public static void SendEmail(string to, string subject, string body)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("smtpPort"), out int port))
        {
            port = 587;
        }

        if (!bool.TryParse(Environment.GetEnvironmentVariable("enableSsl"), out bool useSsl))
        {
            useSsl = false;
        }

        // Initialize your static fields here if needed
        var smtpServer = Environment.GetEnvironmentVariable("smtpHost") ?? string.Empty;
        var username = Environment.GetEnvironmentVariable("smtpUsername") ?? string.Empty;
        var password = Environment.GetEnvironmentVariable("smtpPassword") ?? string.Empty;
        var smtpFrom = Environment.GetEnvironmentVariable("smtpFrom") ?? string.Empty;

        try
        {
            using MailMessage mail = new MailMessage();
            mail.From = new MailAddress(smtpFrom);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            using SmtpClient smtp = new SmtpClient(smtpServer, port);
            smtp.Credentials = new NetworkCredential(username, password);
            smtp.EnableSsl = useSsl;
            smtp.Send(mail);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}