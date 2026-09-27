using AlertHawk.Authentication.Domain.Dto;

namespace AlertHawk.Application.Interfaces;

public interface IMobileAuthCodeService
{
    Task<MobileAuthCodeIssued> IssueAsync(Guid userId);

    Task<Guid?> ConsumeAsync(string? code);
}
