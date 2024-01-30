using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Sentry;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("create")]
    [SwaggerOperation(Summary = "Create User")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostUserCreation([FromBody] UserCreation userCreation)
    {
        if (!string.Equals(userCreation.Password, userCreation.RepeatPassword))
        {
            return BadRequest(new Message("Passwords do not match."));
        }

        try
        {
            await _userService.Create(userCreation);
            return Created();
        }
        catch (InvalidOperationException ex)
        {
            // If user already exists, return 400
            return BadRequest(new Message(ex.Message));
        }
        catch (Exception err)
        {
            SentrySdk.CaptureException(err);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    // [HttpPut("update")]
    // [SwaggerOperation(Summary = "Update User")]
    // [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    // public async Task<IActionResult> PutUserUpdate([FromBody] UserCreation userCreation)
    // {
    //     // call database and update user credentials
    //     return Ok();
    // }
}