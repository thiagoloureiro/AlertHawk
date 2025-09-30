using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorHttpHeaders
{
   public int MonitorId { get; set; }
   public string? CacheControl { get; set; }
   public string? StrictTransportSecurity { get; set; }
   public string? PermissionsPolicy { get; set; }
   public string? XFrameOptions { get; set; }
   public string? XContentTypeOptions { get; set; }
   public string? ReferrerPolicy { get; set; }
   public string? ContentSecurityPolicy { get; set; }
}