using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        Task<bool> Send(NotificationSend notificationSend);
        Task InsertNotificationItem(NotificationItem notificationItem);
        
        Task<IEnumerable<NotificationItem>> SelectNotificationItemList();
        Task<NotificationItem?> SelectNotificationItemById(int id);
    }
}