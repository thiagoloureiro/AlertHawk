using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AlertHawk.Authentication.Filters;

[ExcludeFromCodeCoverage]
public class BlockedDomainsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Skip check if endpoint has [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

        if (allowAnonymous)
        {
            return;
        }

        // Check if user is authenticated
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Check blocked domains
        var blockedDomains = Environment.GetEnvironmentVariable("BLOCKED_DOMAINS") ?? "";

        if (string.IsNullOrWhiteSpace(blockedDomains))
        {
            return;
        }

        var claims = context.HttpContext.User.Identity as ClaimsIdentity;
        var upnMail = claims?.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value ?? "";

        if (string.IsNullOrWhiteSpace(upnMail))
        {
            return;
        }

        var isBlocked = blockedDomains.Split(',')
            .Any(domain => upnMail.EndsWith("@" + domain.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isBlocked)
        {
            context.Result = new StatusCodeResult(403);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }
}