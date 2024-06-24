using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
[ExcludeFromCodeCoverage]
public class HealthCheckController: ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Health Check Endpoint")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public string Get()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version!.ToString();
    }
}