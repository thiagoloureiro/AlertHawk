namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationWebHook
    {
        public int NotificationId { get; set; }
        public string Message { get; set; }
        public string WebHookUrl { get; set; }
        public string Body { get; set; }
        public string? HeadersJson { get; set; }
        public List<Tuple<string, string>>? Headers { get; set; }
    }
}

