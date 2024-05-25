using System.Net.Mail;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IEmailSender
{
    void Send(MailMessage message);
}