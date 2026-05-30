using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public record AzurePasswordCredentialInfo(
    string ApplicationObjectId,
    string ApplicationDisplayName,
    string AppId,
    Guid KeyId,
    string? SecretDisplayName,
    DateTimeOffset EndDateTime);
