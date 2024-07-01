using AlertHawk.Authentication.Domain.Dto;

namespace AlertHawk.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(UserDto? user);
}