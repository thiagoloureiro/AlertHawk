using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Services;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationService : INotificationService
    {
        private readonly IMailNotifier _mailNotifier;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ITeamsNotifier _teamsNotifier;
        private readonly ITelegramNotifier _telegramNotifier;

        public NotificationService(IMailNotifier mailNotifier, ISlackNotifier slackNotifier, ITeamsNotifier teamsNotifier, ITelegramNotifier telegramNotifier)
        {
            _mailNotifier = mailNotifier;
            _slackNotifier = slackNotifier;
            _teamsNotifier = teamsNotifier;
            _telegramNotifier = telegramNotifier;
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
                    await _teamsNotifier.SendNotification(notificationSend.NotificationTeams.Message);
                    break;

                case 3: // Telegram
                    await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId, notificationSend.NotificationTelegram.Message, notificationSend.NotificationTelegram.TelegramBotToken);
                    break;

                case 4: // Slack
                    await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel, notificationSend.NotificationSlack.Message);
                    break;
            }

            return false;
        }
    }
}