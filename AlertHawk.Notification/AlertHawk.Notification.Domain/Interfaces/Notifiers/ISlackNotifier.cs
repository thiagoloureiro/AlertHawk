namespace AlertHawk.Notification.Domain.Interfaces.Notifiers
{
    public interface ISlackNotifier
    {
        Task SendNotification(string channel, string message, string webHookUrl);
    }
}