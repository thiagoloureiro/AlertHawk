namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSend
    {
        public int NotificationTypeId { get; set; }
        public string Message { get; set; } = string.Empty;
        public int MonitorGroupId { get; set; }
        public int MonitorId { get; set; }
        public string Service { get; set; } = string.Empty;
        public int Region { get; set; }
        public int Environment { get; set; }
        public string URL { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool Success { get; set; }

        public NotificationEmail NotificationEmail { get; set; } = null!;
        public NotificationSlack NotificationSlack { get; set; } = null!;
        public NotificationTeams NotificationTeams { get; set; } = null!;
        public NotificationTelegram NotificationTelegram { get; set; } = null!;
        public NotificationWebHook NotificationWebHook { get; set; } = null!;
        public NotificationPush? NotificationPush { get; set; }
        public DateTime NotificationTimeStamp { get; set; }
    }
}