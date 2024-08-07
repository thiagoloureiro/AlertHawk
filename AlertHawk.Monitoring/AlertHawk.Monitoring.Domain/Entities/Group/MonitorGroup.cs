using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<Monitor>? Monitors { get; set; }
    public double? AvgUptime1Hr { get; set; }
    public double? AvgUptime24Hrs { get; set; }
    public double? AvgUptime7Days { get; set; }
    public double? AvgUptime30Days { get; set; }
    public double? AvgUptime3Months { get; set; }
    public double? AvgUptime6Months { get; set; }
}