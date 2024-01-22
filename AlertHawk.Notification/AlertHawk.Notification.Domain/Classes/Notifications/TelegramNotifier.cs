using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlertHawk.Notification.Domain.Classes.Notifications
{
    public static class TelegramNotifier
    {
        public static async Task<Message> SendNotification(long chatId, string message, string telegramBotToken)
        {
            var botClient = new TelegramBotClient(telegramBotToken);

            var result = await botClient.SendTextMessageAsync(chatId, message);

            return result;
        }
    }
}