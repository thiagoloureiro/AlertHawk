namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationTelegram
    {
        public string Message { get; set; }
        public long ChatId { get; set; }
        public string TelegramBotToken { get; set; }
    }
}