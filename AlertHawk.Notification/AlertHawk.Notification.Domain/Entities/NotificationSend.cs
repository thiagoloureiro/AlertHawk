namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSend
    {
        public int NotificationTypeId { get; set; }
        public NotificationEmail NotificationEmail { get; set; }
        public NotificationSlack NotificationSlack { get; set; }
        public NotificationTeams NotificationTeams { get; set; }
        public NotificationTelegram NotificationTelegram { get; set; }
    }
}