namespace AlertHawk.Authentication.Domain.Dto;

public record MobileAuthCodeIssued(string Code, DateTime ExpiresAt);
