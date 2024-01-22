namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSlack
    {
        public int NotificationId { get; set; }
        public string Channel { get; set; }
        public string WebHookUrl { get; set; }
        public string Username { get; set; }
        public string ChannelName { get; set; }
    }
}