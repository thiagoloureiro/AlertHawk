using AlertHawk.Notification.Domain.Classes.Notifications;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationService : INotificationService
    {
        public async Task<bool> Send(NotificationSend notificationSend)
        {
            switch (notificationSend.NotificationTypeId)
            {
                case 1: // Email SMTP - Those will be fixed.
                        // should retrieve configs from DB for the notificationEmail object
                    var result = await MailNotifier.Send(notificationSend.NotificationEmail);
                    return result;
                    break;

                case 2: // MS Teams
                    await TeamsNotifier.SendNotification(notificationSend.NotificationTeams.Message);
                    break;

                case 3: // Telegram
                        // await TelegramNotifier.SendNotification(notificationSend.NotificationTelegram.Message, notificationSend.NotificationTelegram.ChatId);
                    break;

                case 4: // Slack
                    await SlackNotifier.SendNotification(notificationSend.NotificationSlack.Channel,
                        notificationSend.NotificationSlack.Message);
                    break;
            }

            return false;
        }
    }
}