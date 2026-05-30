using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class AzureAppSecret
{
    public int Id { get; set; }

    public string ApplicationObjectId { get; set; } = string.Empty;

    public string ApplicationDisplayName { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public Guid KeyId { get; set; }

    public string? SecretDisplayName { get; set; }

    public DateTimeOffset EndDateTime { get; set; }

    public int DaysUntilExpiry { get; set; }

    public bool IsExpiring { get; set; }

    public DateTime LastChecked { get; set; }
}
