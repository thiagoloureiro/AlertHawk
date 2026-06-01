using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class AzureSecretsConfigDto
{
    public bool Enabled { get; set; }

    public bool HasCredentials { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureSecretsConfigUpdateDto
{
    public bool? Enabled { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureSecretsStatusSummary
{
    public bool Enabled { get; set; }

    public int TotalSecrets { get; set; }

    public int ExpiringCount { get; set; }

    public DateTime? LastChecked { get; set; }

    public int RegisteredAppsCount { get; set; }
}
