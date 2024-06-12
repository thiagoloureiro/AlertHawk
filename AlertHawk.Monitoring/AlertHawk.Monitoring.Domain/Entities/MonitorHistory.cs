using System.Text.Json.Serialization;

namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorHistory
{
    [JsonIgnore]
    public long Id { get; set; }
    [JsonIgnore]
    public int MonitorId { get; set; }
    [JsonIgnore]
    public bool Status { get; set; }
    public DateTime TimeStamp { get; set; }
    [JsonIgnore]
    public int StatusCode { get; set; }
    public int ResponseTime { get; set; }
    [JsonIgnore]
    public string? HttpVersion { get; set; }
    public string? ResponseMessage { get; set; }
    public string? ScreenShotUrl { get; set; }
}