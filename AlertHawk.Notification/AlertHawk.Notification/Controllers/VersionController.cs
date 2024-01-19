using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Notification.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class VersionController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public string Get()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version!.ToString();
    }
}