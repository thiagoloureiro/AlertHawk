using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sentry;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using AlertHawk.Authentication.Application.Interfaces;

namespace AlertHawk.Authentication.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("create")]
    [SwaggerOperation(Summary = "Create User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostUserCreation([FromBody] UserCreation userCreation)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!string.Equals(userCreation.Password, userCreation.RepeatPassword))
        {
            return BadRequest(new Message("Passwords do not match."));
        }

        try
        {
            await _userService.Create(userCreation);
            return Ok(new Message("User account created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            // If user already exists, return 400
            return BadRequest(new Message(ex.Message));
        }
        catch (Exception err)
        {
            SentrySdk.CaptureException(err);
            return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
        }
    }

    [HttpPut("update")]
    [SwaggerOperation(Summary = "Update User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PutUserUpdate([FromBody] UserUpdate userUpdate)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!string.Equals(userUpdate.NewPassword, userUpdate.RepeatNewPassword))
        {
            return BadRequest(new Message("Passwords do not match."));
        }

        var userIdClaim = HttpContext.User.FindFirstValue("id");

        if (!Guid.TryParse(userIdClaim, out var id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new Message("Unauthorized to perform this action."));
        }

        try
        {
            await _userService.Update(id, userUpdate);
            return Ok(new Message("User account updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            SentrySdk.CaptureException(ex);
            return BadRequest(new Message(ex.Message));
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
        }
    }

    [HttpPost("resetPassword/{username}")]
    [SwaggerOperation(Summary = "Reset Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(string username)
    {
        await _userService.ResetPassword(username);
        return Ok();
    }

    [HttpGet("GetAll")]
    [SwaggerOperation(Summary = "Get All Users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var userIdClaim = HttpContext.User.FindFirstValue("id");

        if (!Guid.TryParse(userIdClaim, out var id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new Message("Unauthorized to perform this action."));
        }

        var users = await _userService.GetAll();
        return Ok(users);
    }
    [HttpGet("GetById/{userId}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var userIdClaim = HttpContext.User.FindFirstValue("id");

        if (!Guid.TryParse(userIdClaim, out var id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new Message("Unauthorized to perform this action."));
        }

        var users = await _userService.Get(userId);
        return Ok(users);
    }
    [HttpGet("GetByEmail/{userEmail}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByEmail(string userEmail)
    {
        var userIdClaim = HttpContext.User.FindFirstValue("id");

        if (!Guid.TryParse(userIdClaim, out var id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new Message("Unauthorized to perform this action."));
        }

        var users = await _userService.GetByEmail(userEmail);
        return Ok(users);
    }
    [HttpGet("GetByUserName/{userName}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByUsername(string userName)
    {
        var userIdClaim = HttpContext.User.FindFirstValue("id");

        if (!Guid.TryParse(userIdClaim, out var id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new Message("Unauthorized to perform this action."));
        }

        var users = await _userService.GetByUsername(userName);
        return Ok(users);
    }
}