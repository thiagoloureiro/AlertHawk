using Telegram.Bot.Types;

namespace AlertHawk.Notification.Domain.Interfaces.Notifiers
{
    public interface ITelegramNotifier
    {
        Task<Message> SendNotification(long chatId, string message, string telegramBotToken);
    }
}