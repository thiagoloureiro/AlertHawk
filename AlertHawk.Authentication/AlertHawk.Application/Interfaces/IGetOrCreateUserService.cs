using System.Security.Claims;
using AlertHawk.Authentication.Domain.Dto;

namespace AlertHawk.Application.Interfaces;

public interface IGetOrCreateUserService
{
    Task<UserDto?> GetUserOrCreateUser(ClaimsPrincipal user);

}