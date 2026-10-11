using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AlertHawk.Metrics.API.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    private readonly IApiKeyValidator _validator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var provided = headerValues.ToString().Trim();
        if (string.IsNullOrEmpty(provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var name = _validator.Match(provided);
        if (name == null)
        {
            Logger.LogWarning("Rejected a request with an invalid API key.");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, name),
            new Claim("api_key_name", name),
        ],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
