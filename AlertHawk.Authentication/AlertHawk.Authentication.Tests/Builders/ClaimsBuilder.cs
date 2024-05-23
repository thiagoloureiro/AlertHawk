using System.Security.Claims;

namespace AlertHawk.Authentication.Tests.Builders;

public class ClaimsBuilder
{
    private ClaimsPrincipal _claimsPrincipal;

    public ClaimsPrincipal DefaulClaimsPrincipal(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Name")
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal EmailTypeClaimsPrincipal(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("email", email),
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal PreferredUsernameClaimsPrincipal(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", email),
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal EmptyClaimsPrincipal()
    {
        _claimsPrincipal = new ClaimsPrincipal();
        return _claimsPrincipal;
    }
}