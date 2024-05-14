using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<NotificationItem>> SelectNotificationItemList();
    Task<int> InsertNotificationItem(NotificationItem notificationItem);
    Task InsertNotificationItemEmailSmtp(NotificationItem notificationItem);
    Task UpdateNotificationItem(NotificationItem notificationItem);
    Task InsertNotificationItemMsTeams(NotificationItem notificationItem);
    Task InsertNotificationItemTelegram(NotificationItem notificationItem);
    Task InsertNotificationItemSlack(NotificationItem notificationItem);
    Task InsertNotificationItemWebHook(NotificationItem notificationItem);
    Task<NotificationItem?> SelectNotificationItemById(int id);
    Task<IEnumerable<NotificationItem>> SelectNotificationItemList(List<int> ids);
    Task DeleteNotificationItem(int id);
    Task<IEnumerable<NotificationItem?>> SelectNotificationItemByMonitorGroupId(int id);
    Task InsertNotificationLog(NotificationLog notificationLog);
}