namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSlack
    {
        public int NotificationId { get; set; }
        public required string ChannelName { get; set; }
        public required string WebHookUrl { get; set; }
    }
}