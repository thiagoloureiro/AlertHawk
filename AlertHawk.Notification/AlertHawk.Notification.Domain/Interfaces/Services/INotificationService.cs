using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        Task<bool> Send(NotificationSend notificationSend);
        Task InsertNotificationItem(NotificationItem notificationItem);
        Task UpdateNotificationItem(NotificationItem notificationItem);
        Task DeleteNotificationItem(int id);
        Task<IEnumerable<NotificationItem>> SelectNotificationItemList();
        Task<NotificationItem?> SelectNotificationItemById(int id);
    }
}