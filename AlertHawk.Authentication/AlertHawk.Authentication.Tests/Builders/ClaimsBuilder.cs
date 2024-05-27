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
    public ClaimsPrincipal NameTypeClaimsPrincipal(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, email)
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal DefaulClaimsPrincipalWithgivenName(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Name"),
            new Claim(type: "givenname", value: "givenname"),
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal DefaulClaimsPrincipalWithsurName(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Name"),
            new Claim(type: "surname", value: "surname"),
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal DefaulClaimsPrincipalWithsurNameAndGivenName(string email)
    {
        _claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Name"),
            new Claim(type: "surname", value: "surname"),
            new Claim(type: "givenname", value: "givenname")
        }));
        return _claimsPrincipal;
    }
    public ClaimsPrincipal EmptyClaimsPrincipal()
    {
        _claimsPrincipal = new ClaimsPrincipal();
        return _claimsPrincipal;
    }
}