namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSend
    {
        public int NotificationTypeId { get; set; }
        public string Message { get; set; }
        public int MonitorGroupId { get; set; }
        public NotificationEmail NotificationEmail { get; set; } = null!;
        public NotificationSlack NotificationSlack { get; set; } = null!;
        public NotificationTeams NotificationTeams { get; set; } = null!;
        public NotificationTelegram NotificationTelegram { get; set; } = null!;
        public NotificationWebHook NotificationWebHook { get; set; } = null!;
        public NotificationPush NotificationPush { get; set; }
        public DateTime NotificationTimeStamp { get; set; }
    }
}