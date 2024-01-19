using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Notification.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class NotificationController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Send()
    {
        return Ok("Notification sent");
    }
}