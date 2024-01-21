using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationTypeService : INotificationTypeService
    {
        private readonly INotificationTypeRepository _notificationTypeRepository;

        public NotificationTypeService(INotificationTypeRepository notificationTypeRepository)

        {
            _notificationTypeRepository = notificationTypeRepository;
        }

        public async Task<IEnumerable<NotificationType>> SelectNotificationType()
        {
            return await _notificationTypeRepository.SelectNotificationType();
        }

        public async Task<NotificationType?> SelectNotificationTypeById(int id)
        {
            return await _notificationTypeRepository.SelectNotificationTypeById(id);
        }

        public async Task<NotificationType?> SelectNotificationTypeByName(string name)
        {
            return await _notificationTypeRepository.SelectNotificationTypeByName(name);
        }

        public async Task InsertNotificationType(NotificationType notificationtype)
        {
            await _notificationTypeRepository.InsertNotificationType(notificationtype);
        }

        public async Task UpdateNotificationType(NotificationType notificationtype)
        {
            await _notificationTypeRepository.UpdateNotificationType(notificationtype);
        }

        public async Task DeleteNotificationType(int id)
        {
            await _notificationTypeRepository.DeleteNotificationType(id);
        }
    }
}