using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace AlertHawk.Monitoring.Helpers;

[ExcludeFromCodeCoverage]
public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }
    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }
        if (!AuthenticateRequest(context))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
            return;
        }
        await _next(context);
    }
    private bool AuthenticateRequest(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            return false;
        var authHeader = authHeaderValues.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Basic "))
            return false;
        var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
        var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
        var username = decodedUsernamePassword.Split(':')[0];
        var password = decodedUsernamePassword.Split(':')[1];
        var appSettingsUsername = _configuration["SwaggerUICredentials:username"];
        var appSettingsPassword = _configuration["SwaggerUICredentials:password"];
        return (username == appSettingsUsername && password == appSettingsPassword);
    }
}