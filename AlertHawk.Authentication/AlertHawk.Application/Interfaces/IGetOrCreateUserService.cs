using AlertHawk.Authentication.Domain.Dto;
using System.Security.Claims;

namespace AlertHawk.Application.Interfaces;

public interface IGetOrCreateUserService
{
    Task<UserDto?> GetUserOrCreateUser(ClaimsPrincipal claims);
}