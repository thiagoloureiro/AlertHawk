using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mail;

namespace AlertHawk.Authentication.Infrastructure.EmailSender;
[ExcludeFromCodeCoverage]
public static class EmailSender
{
    public static void SendEmail(string to, string subject, string body)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("SmtpPort"), out int port))
        {
            port = 587;
        }

        if (!bool.TryParse(Environment.GetEnvironmentVariable("enableSsl"), out bool useSsl))
        {
            useSsl = false;
        }

        // Initialize your static fields here if needed
        var smtpServer = Environment.GetEnvironmentVariable("smtpHost") ?? "smtp.office365.com";
        var username = Environment.GetEnvironmentVariable("username") ?? string.Empty;
        var password = Environment.GetEnvironmentVariable("password") ?? string.Empty;
        
        try
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(username);
                mail.To.Add(to);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient(smtpServer, port))
                {
                    smtp.Credentials = new NetworkCredential(username, password);
                    smtp.EnableSsl = useSsl;
                    smtp.Send(mail);
                }
            }

            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }
}