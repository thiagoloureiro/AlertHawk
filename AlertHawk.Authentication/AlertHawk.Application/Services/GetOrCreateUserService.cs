using System.Security.Claims;
using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Services;

public class GetOrCreateUserService(IUserService userService) : IGetOrCreateUserService
{
    public async Task<UserDto?> GetUserOrCreateUser(ClaimsPrincipal claims)
    {
        string? userEmail = "";
        var hasEmailIdentityNameLogged = claims.Identity?.Name;
        if (hasEmailIdentityNameLogged != null)
        {
            userEmail = claims.Claims.FirstOrDefault(s => s.Type.Contains("emailaddress"))?.Value ??
                        hasEmailIdentityNameLogged;
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            userEmail = claims.Claims?.FirstOrDefault(c => c.Type == "email")?.Value;
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            userEmail = claims.Claims?.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        }
        
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            userEmail = claims.Claims?.FirstOrDefault(c => c.Type == "emailaddress")?.Value;
        }

        if (userEmail != null)
        {
            var user = await userService.GetByEmail(userEmail);

            // This is for AD First Login only
            if (ReferenceEquals(null, user))
            {
                if (claims.Claims != null)
                {
                    var name = claims.Claims.FirstOrDefault(s => s.Type.Contains("givenname"))?.Value + " " +
                               claims.Claims.FirstOrDefault(s => s.Type.Contains("surname"))?.Value;
                    var newUser = new UserCreationFromAzure(name, userEmail);

                    await userService.CreateFromAzure(newUser);
                }

                return (await userService.GetByEmail(userEmail))!;
            }

            return user;
        }

        return null;
    }
}