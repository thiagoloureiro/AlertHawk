namespace AlertHawk.Notification.Domain.Entities;

public class NotificationPush
{
    public int NotificationId { get; set; }
    public PushNotificationBody PushNotificationBody { get; set; } = null!;
}

public class PushNotificationData
{
    public string message { get; set; }
}

public class PushNotificationItem
{
    public string title { get; set; }
    public string body { get; set; }
    public int badge { get; set; }
    public string sound { get; set; }
}

public class PushNotificationBody
{
    public string to { get; set; }
    public PushNotificationData data { get; set; }
    public PushNotificationItem notification { get; set; }
}

