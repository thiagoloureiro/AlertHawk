using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TelegramNotifier : ITelegramNotifier
    {
        public async Task<Message> SendNotification(long chatId, string message, string telegramBotToken)
        {
            var botClient = new TelegramBotClient(telegramBotToken);
            
            message = message.Contains("Success") ? ":heavy_check_mark: " + message : ":x: " + message;

            var result = await botClient.SendTextMessageAsync(chatId, message);

            return result;
        }
    }
}