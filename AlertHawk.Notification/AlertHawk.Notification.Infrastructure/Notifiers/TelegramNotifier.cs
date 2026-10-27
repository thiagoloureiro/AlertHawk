using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlertHawk.Notification.Infrastructure.Notifiers
{
    public class TelegramNotifier : ITelegramNotifier
    {
        private readonly HttpClient? _httpClient;

        public TelegramNotifier()
        {
        }

        public TelegramNotifier(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Message> SendNotification(long chatId, string message, string telegramBotToken)
        {
            var botClient = _httpClient is null
                ? new TelegramBotClient(telegramBotToken)
                : new TelegramBotClient(telegramBotToken, _httpClient);

            // Check if message indicates healthy or has issues
            // For node metrics: "is healthy" or "has issues"
            // For monitor notifications: "Success" or "Error"
            var isHealthy = message.Contains("healthy", StringComparison.OrdinalIgnoreCase) || 
                           message.Contains("Success", StringComparison.OrdinalIgnoreCase);
            var hasIssues = message.Contains("has issues", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("Error", StringComparison.OrdinalIgnoreCase);
            
            message = isHealthy 
                ? "\u2705 " + message
                : (hasIssues ? "\u274c " + message : message);

            var result = await botClient.SendMessage(chatId, message);

            return result;
        }
    }
}