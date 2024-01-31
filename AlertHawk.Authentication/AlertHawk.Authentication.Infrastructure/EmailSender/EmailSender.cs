using System.Net;
using System.Net.Mail;

namespace AlertHawk.Authentication.Infrastructure.EmailSender;

public static class EmailSender
{
    private static string? _smtpServer;
    private static int _port;
    private static string? _username;
    private static string? _password;
    private static bool _useSsl;
    
    public static void SendEmail(string to, string subject, string body)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("SmtpPort"), out _port))
        {
            _port = 587;
        }

        if (!bool.TryParse(Environment.GetEnvironmentVariable("enableSsl"), out _useSsl))
        {
            _useSsl = false;
        }

        // Initialize your static fields here if needed
        _smtpServer = Environment.GetEnvironmentVariable("smtpHost") ?? "smtp.office365.com";
        _username = Environment.GetEnvironmentVariable("username") ?? string.Empty;
        _password = Environment.GetEnvironmentVariable("password") ?? string.Empty;
        try
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(_username);
                mail.To.Add(to);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient(_smtpServer, _port))
                {
                    smtp.Credentials = new NetworkCredential(_username, _password);
                    smtp.EnableSsl = _useSsl;

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