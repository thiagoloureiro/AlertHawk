using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<NotificationItem>> SelectNotificationItemList();
    Task InsertNotificationItemEmailSmtp(NotificationItem notificationItem);
    Task InsertNotificationItemMSTeams(NotificationItem notificationItem);
    Task InsertNotificationItemTelegram(NotificationItem notificationItem);
    Task InsertNotificationItemSlack(NotificationItem notificationItem);
    Task<NotificationItem> SelectNotificationItemById(int id);
}