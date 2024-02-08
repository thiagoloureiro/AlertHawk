using AlertHawk.Authentication.Domain.Dto;

namespace AlertHawk.Authentication.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}