using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Notifiers
{
    public interface IMailNotifier
    {
        Task<bool> Send(NotificationEmail emailNotification);
    }
}