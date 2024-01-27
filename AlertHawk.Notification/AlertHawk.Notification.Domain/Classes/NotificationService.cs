using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationService : INotificationService
    {
        private readonly IMailNotifier _mailNotifier;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ITeamsNotifier _teamsNotifier;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(IMailNotifier mailNotifier, ISlackNotifier slackNotifier,
            ITeamsNotifier teamsNotifier, ITelegramNotifier telegramNotifier,
            INotificationRepository notificationRepository)
        {
            _mailNotifier = mailNotifier;
            _slackNotifier = slackNotifier;
            _teamsNotifier = teamsNotifier;
            _telegramNotifier = telegramNotifier;
            _notificationRepository = notificationRepository;
        }

        public async Task<bool> Send(NotificationSend notificationSend)
        {
            switch (notificationSend.NotificationTypeId)
            {
                case 1: // Email SMTP - Those will be fixed.
                    // should retrieve configs from DB for the notificationEmail object
                    var result = await _mailNotifier.Send(notificationSend.NotificationEmail);
                    return result;
                    break;

                case 2: // MS Teams
                    await _teamsNotifier.SendNotification(notificationSend.Message, notificationSend.NotificationTeams.WebHookUrl);
                    return true;
                    break;

                case 3: // Telegram
                    await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId, notificationSend.Message,
                        notificationSend.NotificationTelegram.TelegramBotToken);
                    return true;
                    break;

                case 4: // Slack
                    await _slackNotifier.SendNotification(notificationSend.NotificationSlack.ChannelName,
                        notificationSend.Message, notificationSend.NotificationSlack.WebHookUrl);
                    return true;
                    break;
            }

            return false;
        }

        public async Task InsertNotificationItem(NotificationItem notificationItem)
        {
            switch (notificationItem.NotificationTypeId)
            {
                case 1: // Email SMTP 
                    await _notificationRepository.InsertNotificationItemEmailSmtp(notificationItem);
                    break;
                case 2: // MS Teams
                    await _notificationRepository.InsertNotificationItemMSTeams(notificationItem);
                    break;
                case 3: // Telegram
                    await _notificationRepository.InsertNotificationItemTelegram(notificationItem);
                    break;
                case 4: // Slack
                    await _notificationRepository.InsertNotificationItemSlack(notificationItem);
                    break;
            }
        }

        public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList()
        {
            return await _notificationRepository.SelectNotificationItemList();
        }

        public async Task<NotificationItem> SelectNotificationItemById(int id)
        {
            return await _notificationRepository.SelectNotificationItemById(id);
        }
    }
}