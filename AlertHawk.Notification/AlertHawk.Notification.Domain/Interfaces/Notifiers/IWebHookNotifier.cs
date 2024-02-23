namespace AlertHawk.Notification.Domain.Interfaces.Notifiers;

public interface IWebHookNotifier
{
    Task SendNotification(string message, string webHookUrl, string body, List<Tuple<string, string>>? headers);
}