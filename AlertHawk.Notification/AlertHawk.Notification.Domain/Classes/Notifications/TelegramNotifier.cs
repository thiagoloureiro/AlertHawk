using Telegram.Bot;

namespace AlertHawk.Notification.Domain.Classes.Notifications
{
    public static class TelegramNotifier
    {
        public static async Task SendNotification(string message, string chatId)
        {
            try
            {
                var TelegramBotToken = "";
                TelegramBotClient botClient = new TelegramBotClient(TelegramBotToken);

                await botClient.SendTextMessageAsync(chatId, message);

                Console.WriteLine("Notification sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}