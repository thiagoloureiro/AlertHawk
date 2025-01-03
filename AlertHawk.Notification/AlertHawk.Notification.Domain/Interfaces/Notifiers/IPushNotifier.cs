using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Notifiers;

public interface IPushNotifier
{
    Task SendNotification(string message, NotificationPush notificationPush);
}