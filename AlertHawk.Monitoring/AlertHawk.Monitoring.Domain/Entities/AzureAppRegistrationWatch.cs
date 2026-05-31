using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class AzureAppRegistrationWatch
{
    public int Id { get; set; }

    public string ApplicationObjectId { get; set; } = string.Empty;

    public string ApplicationDisplayName { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}

[ExcludeFromCodeCoverage]
public class AzureAppRegistrationSummary
{
    public string ApplicationObjectId { get; set; } = string.Empty;

    public string ApplicationDisplayName { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public bool IsRegistered { get; set; }
}

[ExcludeFromCodeCoverage]
public class RegisterAzureAppRegistrationRequest
{
    public string ApplicationObjectId { get; set; } = string.Empty;

    public string? ApplicationDisplayName { get; set; }

    public string? AppId { get; set; }
}
