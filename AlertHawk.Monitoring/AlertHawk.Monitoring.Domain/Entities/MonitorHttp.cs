using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorHttp : Monitor
{
    public int MonitorId { get; set; }
    public bool IgnoreTlsSsl { get; set; }
    public int MaxRedirects { get; set; }
    public new string UrlToCheck { get; set; }
    public HttpStatusCode ResponseStatusCode { get; set; }
    public int Timeout { get; set; }
    public bool LastStatus { get; set; }
    public int ResponseTime { get; set; }
    public string? HttpVersion { get; set; }
    public MonitorHttpMethod MonitorHttpMethod { get; set; }
    public string? Body { get; set; }
    public string? HeadersJson { get; set; }
    public List<Tuple<string, string>>? Headers { get; set; }
    public int? HttpResponseCodeFrom { get; set; }
    public int? HttpResponseCodeTo { get; set; }
    public bool? CheckMonitorHttpHeaders { get; set; } = false;
}