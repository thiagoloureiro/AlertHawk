using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        Task<bool> Send(NotificationSend notificationSend);
        Task InsertNotificationItem(NotificationItem? notificationItem);
        Task UpdateNotificationItem(NotificationItem? notificationItem);
        Task DeleteNotificationItem(int id);
        Task<IEnumerable<NotificationItem>> SelectNotificationItemList(string jwtToken);
        Task<IEnumerable<NotificationItem?>> SelectNotificationItemList(List<int> ids);
        Task<NotificationItem?> SelectNotificationItemById(int id);
        Task<List<int>?> GetUserGroupMonitorListIds(string token);
        Task<IEnumerable<NotificationItem?>> SelectNotificationItemByMonitorGroupId(int id);
        Task InsertNotificationLog(NotificationLog notificationLog);
        Task<long> GetNotificationLogCount();
    }
}