using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class AzureSecretsConfigDto
{
    public bool Enabled { get; set; }

    public int DaysBeforeExpiryToAlert { get; set; }

    public int MonitorId { get; set; }

    public string Cron { get; set; } = string.Empty;

    public bool HasCredentials { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureSecretsConfigUpdateDto
{
    public bool? Enabled { get; set; }

    public int? DaysBeforeExpiryToAlert { get; set; }

    public int? MonitorId { get; set; }

    public string? Cron { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureSecretsStatusSummary
{
    public bool Enabled { get; set; }

    public int TotalSecrets { get; set; }

    public int ExpiringCount { get; set; }

    public DateTime? LastChecked { get; set; }

    public bool? MonitorStatus { get; set; }

    public int MonitorId { get; set; }

    public string? MonitorName { get; set; }

    public int DaysBeforeExpiryToAlert { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureAppSecretMonitorRequest
{
    public string Name { get; set; } = string.Empty;

    public int MonitorGroup { get; set; }

    public int MonitorRegion { get; set; } = 3;

    public int MonitorEnvironment { get; set; } = 6;

    public int HeartBeatInterval { get; set; } = 60;
}

[ExcludeFromCodeCoverage]
public class AzureAppSecretMonitorUpdateRequest
{
    public string Name { get; set; } = string.Empty;

    public int MonitorGroup { get; set; }

    public int MonitorRegion { get; set; }

    public int MonitorEnvironment { get; set; }

    public int HeartBeatInterval { get; set; }
}
