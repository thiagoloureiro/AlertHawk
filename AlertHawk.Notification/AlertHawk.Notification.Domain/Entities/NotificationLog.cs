using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Notification.Domain.Entities;

[ExcludeFromCodeCoverage]
public class NotificationLog
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public int NotificationTypeId { get; set; }
    public string? Message { get; set; }
}