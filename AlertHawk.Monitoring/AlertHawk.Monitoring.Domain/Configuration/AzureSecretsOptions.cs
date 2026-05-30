using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Configuration;

[ExcludeFromCodeCoverage]
public class AzureSecretsOptions
{
    public const string SectionName = "AzureSecrets";

    public bool Enabled { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Alert when a secret expires within this many days.
    /// </summary>
    public int DaysBeforeExpiryToAlert { get; set; } = 30;

    /// <summary>
    /// Monitor used for notifications and alert history (must exist in Monitor table).
    /// </summary>
    public int MonitorId { get; set; }

    /// <summary>
    /// Hangfire cron expression for the secrets sync job.
    /// </summary>
    public string Cron { get; set; } = "0 */6 * * *";
}
