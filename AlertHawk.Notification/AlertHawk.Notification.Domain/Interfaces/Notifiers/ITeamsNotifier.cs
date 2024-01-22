namespace AlertHawk.Notification.Domain.Interfaces.Notifiers
{
    public interface ITeamsNotifier
    {
        Task SendNotification(string message, string webHookUrl);
    }
}