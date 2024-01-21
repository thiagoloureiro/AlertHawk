using AlertHawk.Notification.Domain.Entities;

namespace AlertHawk.Notification.Domain.Interfaces.Repositories
{
    public interface INotificationTypeRepository
    {
        Task<IEnumerable<NotificationType>> SelectNotificationType();

        Task<NotificationType?> SelectNotificationTypeById(int id);

        Task<NotificationType?> SelectNotificationTypeByName(string name);

        Task InsertNotificationType(NotificationType notificationtype);

        Task UpdateNotificationType(NotificationType notificationtype);

        Task DeleteNotificationType(int id);
    }
}