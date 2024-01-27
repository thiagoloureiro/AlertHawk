namespace AlertHawk.Notification.Domain.Entities;

public class NotificationItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int NotificationTypeId { get; set; }
    public NotificationSlack? NotificationSlack { get; set; }
    public NotificationEmail? NotificationEmail { get; set; }
    public NotificationTeams? NotificationTeams { get; set; }
    public NotificationTelegram? NotificationTelegram { get; set; }
}