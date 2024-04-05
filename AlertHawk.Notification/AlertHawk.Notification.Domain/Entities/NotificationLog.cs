namespace AlertHawk.Notification.Domain.Entities;

public class NotificationLog
{
    public int Id { get; set; }
    public DateTime TimeStamp  { get; set; }
    public int NotificationTypeId { get; set; }
    public string Message { get; set; }
}