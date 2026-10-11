using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Metrics.API.Authentication;

public interface IApiKeyValidator
{
    bool IsConfigured { get; }

    /// <summary>
    /// Returns <c>api-key</c> when <paramref name="providedKey"/> matches the configured key, otherwise null.
    /// </summary>
    string? Match(string providedKey);
}

public sealed class ApiKeyValidator : IApiKeyValidator
{
    public const string ConfigurationKey = "ApiKey";
    public const string IdentityName = "api-key";

    private readonly byte[]? _key;

    public ApiKeyValidator(IConfiguration configuration)
    {
        var value = configuration[ConfigurationKey]?.Trim();
        _key = string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value);
    }

    public bool IsConfigured => _key != null;

    public string? Match(string providedKey)
    {
        if (string.IsNullOrEmpty(providedKey) || _key == null)
        {
            return null;
        }

        var provided = Encoding.UTF8.GetBytes(providedKey);
        if (provided.Length == _key.Length && CryptographicOperations.FixedTimeEquals(provided, _key))
        {
            return IdentityName;
        }

        return null;
    }
}
