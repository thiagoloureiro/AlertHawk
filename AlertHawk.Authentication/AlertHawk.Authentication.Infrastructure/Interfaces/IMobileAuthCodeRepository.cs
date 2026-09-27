namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IMobileAuthCodeRepository
{
    Task EnsureTableExistsAsync();

    Task CreateAsync(string code, Guid userId, DateTime expiresAt);

    Task<Guid?> ConsumeAsync(string code);
}
