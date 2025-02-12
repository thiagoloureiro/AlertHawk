using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Notifiers;

public interface IWebHookNotifier
{
    Task SendNotification(NotificationSend notification, NotificationWebHook webHook);
}