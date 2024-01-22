using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        Task<bool> Send(NotificationSend notificationSend);
        Task InsertNotificationItem(NotificationItem notificationItem);
    }
}