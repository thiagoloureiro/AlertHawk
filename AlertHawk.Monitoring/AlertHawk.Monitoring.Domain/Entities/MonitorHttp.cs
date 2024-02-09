using System.Net;

namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorHttp : Monitor
{
    public int MonitorId { get; set; }
    public bool CheckCertExpiry { get; set; }
    public bool IgnoreTlsSsl { get; set; }
    public bool UpsideDownMode { get; set; }
    public required int MaxRedirects{ get; set; }
    public required string UrlToCheck { get; set; }
    public HttpStatusCode ResponseStatusCode { get; set; }
    public required int Timeout { get; set; }
    public bool LastStatus { get; set; }
    public int ResponseTime { get; set; }
    public string HttpVersion { get; set; }
}