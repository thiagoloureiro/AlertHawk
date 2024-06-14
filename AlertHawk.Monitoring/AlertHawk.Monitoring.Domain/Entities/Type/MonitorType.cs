using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;
[ExcludeFromCodeCoverage]
public class MonitorType
{
    public int Id { get; set; }
    public required string Name { get; set; }
}