namespace AlertHawk.Monitoring.Domain.Entities;

public class SystemConfiguration
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
