using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using System.Security.Cryptography;

namespace AlertHawk.Application.Services;

public class MobileAuthCodeService(IMobileAuthCodeRepository repository) : IMobileAuthCodeService
{
    public const int LifetimeMinutes = 10;
    private const int CodeLength = 8;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<MobileAuthCodeIssued> IssueAsync(Guid userId)
    {
        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.AddMinutes(LifetimeMinutes);
        await repository.CreateAsync(code, userId, expiresAt);
        return new MobileAuthCodeIssued(FormatCode(code), expiresAt);
    }

    public async Task<Guid?> ConsumeAsync(string? code)
    {
        var normalized = Normalize(code);
        if (normalized is null)
        {
            return null;
        }

        return await repository.ConsumeAsync(normalized);
    }

    private static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        Span<byte> bytes = stackalloc byte[CodeLength];
        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }

    private static string FormatCode(string code)
    {
        return $"{code[..4]}-{code[4..]}";
    }

    private static string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var filtered = new string(code.Where(character => character is not ('-' or ' ')).ToArray())
            .ToUpperInvariant();
        if (filtered.Length != CodeLength)
        {
            return null;
        }

        if (filtered.Any(character => !Alphabet.Contains(character)))
        {
            return null;
        }

        return filtered;
    }
}
