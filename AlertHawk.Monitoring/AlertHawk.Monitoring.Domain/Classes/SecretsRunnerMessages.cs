using AlertHawk.Monitoring.Domain.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AlertHawk.Monitoring.Domain.Classes;

[ExcludeFromCodeCoverage]
public static class SecretsRunnerMessages
{
    public static string BuildResponseMessage(IReadOnlyCollection<AzureAppSecret> expiringSecrets, bool succeeded)
    {
        if (succeeded)
        {
            return "All Azure app registration secrets are within the expiry threshold.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"{expiringSecrets.Count} Azure app secret(s) expiring soon:");
        foreach (var secret in expiringSecrets.OrderBy(s => s.EndDateTime))
        {
            builder.AppendLine(
                $"- {secret.ApplicationDisplayName} ({secret.AppId}): '{secret.SecretDisplayName ?? secret.KeyId.ToString()}' expires in {secret.DaysUntilExpiry} day(s) on {secret.EndDateTime:yyyy-MM-dd}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildNewlyExpiringMessage(IReadOnlyCollection<AzureAppSecret> secrets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("New Azure app secret(s) entered the expiry window:");
        foreach (var secret in secrets.OrderBy(s => s.EndDateTime))
        {
            builder.AppendLine(
                $"- {secret.ApplicationDisplayName}: '{secret.SecretDisplayName ?? secret.KeyId.ToString()}' expires in {secret.DaysUntilExpiry} day(s) on {secret.EndDateTime:yyyy-MM-dd}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildRecoveredMessage(IReadOnlyCollection<AzureAppSecret> secrets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Azure app secret(s) no longer in the expiry window:");
        foreach (var secret in secrets)
        {
            builder.AppendLine($"- {secret.ApplicationDisplayName}: '{secret.SecretDisplayName ?? secret.KeyId.ToString()}'");
        }

        return builder.ToString().TrimEnd();
    }
}
