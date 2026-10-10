using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Metrics.API.Authentication;

public interface IApiKeyValidator
{
    int Count { get; }

    /// <summary>
    /// Returns the configured key name when <paramref name="providedKey"/> matches, otherwise null.
    /// </summary>
    string? Match(string providedKey);
}

public sealed class ApiKeyValidator : IApiKeyValidator
{
    private readonly KeyMaterial[] _keys;

    public ApiKeyValidator(IConfiguration configuration)
    {
        _keys = Load(configuration).ToArray();
    }

    public int Count => _keys.Length;

    public string? Match(string providedKey)
    {
        if (string.IsNullOrEmpty(providedKey) || _keys.Length == 0)
        {
            return null;
        }

        var provided = Encoding.UTF8.GetBytes(providedKey);
        string? matchedName = null;

        foreach (var candidate in _keys)
        {
            if (candidate.Material.Length == provided.Length &&
                CryptographicOperations.FixedTimeEquals(candidate.Material, provided))
            {
                matchedName ??= candidate.Name;
            }
        }

        return matchedName;
    }

    internal static IEnumerable<KeyMaterial> Load(IConfiguration configuration)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in configuration.GetSection("ApiKeys:Keys").GetChildren())
        {
            var key = child["Key"]?.Trim();
            var name = child["Name"]?.Trim();
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
            {
                continue;
            }

            yield return new KeyMaterial(
                string.IsNullOrEmpty(name) ? "api-key" : name,
                Encoding.UTF8.GetBytes(key));
        }

        var raw = configuration["API_KEYS"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        var index = 1;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!seen.Add(part))
            {
                continue;
            }

            yield return new KeyMaterial($"api-key-{index}", Encoding.UTF8.GetBytes(part));
            index++;
        }
    }

    internal readonly record struct KeyMaterial(string Name, byte[] Material);
}
