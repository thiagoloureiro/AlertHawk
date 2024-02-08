namespace AlertHawk.Authentication.Domain.Dto;

public record UserDto (Guid Id, string Username, string Email, bool IsAdmin, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? LastLogon);