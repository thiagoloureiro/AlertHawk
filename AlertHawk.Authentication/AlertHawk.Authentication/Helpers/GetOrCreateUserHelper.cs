using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Entities;
using System.Security.Claims;
using AlertHawk.Authentication.Domain.Dto;

namespace AlertHawk.Authentication.Helpers;

public class GetOrCreateUserHelper(IUserService userService)
{
    public async Task<UserDto> GetUserOrCreateUser(ClaimsPrincipal claims)
    {
        string userEmail = "";
        var hasEmailIdentityNameLogged = claims.Identity?.Name;
        if (hasEmailIdentityNameLogged != null)
        {
            userEmail = claims.Claims.FirstOrDefault(s => s.Type.Contains("emailaddress"))?.Value ??
                        hasEmailIdentityNameLogged;
        }

        var user = await userService.GetByEmail(userEmail);
        
        // This is for AD First Login only
        if (ReferenceEquals(null, user))
        {
            var name = claims.Claims.FirstOrDefault(s => s.Type.Contains("givenname"))?.Value + " " +
                       claims.Claims.FirstOrDefault(s => s.Type.Contains("surname"))?.Value;
            var newUser = new UserCreationFromAzure(name, userEmail);

            await userService.CreateFromAzure(newUser);
            return (await userService.GetByEmail(userEmail))!;
        }

        return user;
    }
}