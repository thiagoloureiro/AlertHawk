using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorHistory
{
    [JsonIgnore]
    public long Id { get; set; }

    [JsonIgnore]
    public int MonitorId { get; set; }

    public bool Status { get; set; }
    public DateTime TimeStamp { get; set; }
    public int StatusCode { get; set; }
    public int ResponseTime { get; set; }

    [JsonIgnore]
    public string? HttpVersion { get; set; }

    public string? ResponseMessage { get; set; }
    public string? ScreenShotUrl { get; set; }
}