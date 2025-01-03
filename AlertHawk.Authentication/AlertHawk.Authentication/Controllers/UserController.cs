using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IGetOrCreateUserService _getOrCreateUserService;

    public UserController(IUserService userService, IGetOrCreateUserService getOrCreateUserService)
    {
        _userService = userService;
        _getOrCreateUserService = getOrCreateUserService;
    }

    [AllowAnonymous]
    [HttpPost("create")]
    [SwaggerOperation(Summary = "Create User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostUserCreation([FromBody] UserCreation userCreation)
    {
        var enabledLoginAuth = Environment.GetEnvironmentVariable("ENABLED_LOGIN_AUTH")?.ToLower() ?? "true";
        if (enabledLoginAuth == "false")
        {
            return BadRequest(new Message("Login is disabled."));
        }

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

    [HttpDelete("delete/{userId}")]
    [SwaggerOperation(Summary = "Delete User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var usrAdmin = await IsUserAdmin();
        if (!usrAdmin)
        {
            return BadRequest(
                new Message("This user is not authorized to do this operation"));
        }

        var user = await _userService.Get(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        await _userService.Delete(userId);
        return Ok();
    }

    [HttpPut("update")]
    [SwaggerOperation(Summary = "Update User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PutUserUpdate([FromBody] UserDto userUpdate)
    {
        var usrAdmin = await IsUserAdmin();
        if (!usrAdmin)
        {
            return BadRequest(
                new Message("This user is not authorized to do this operation"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userService.Update(userUpdate);
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

    [AllowAnonymous]
    [HttpPost("resetPassword/{email}")]
    [SwaggerOperation(Summary = "Reset Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(string email)
    {
        var enabledLoginAuth = Environment.GetEnvironmentVariable("ENABLED_LOGIN_AUTH")?.ToLower() ?? "true";
        if (enabledLoginAuth == "false")
        {
            return BadRequest(new Message("Login is disabled."));
        }

        var user = await _userService.GetByEmail(email);

        if (user == null)
        {
            return Ok();
        }

        await _userService.ResetPassword(email);
        return Ok();
    }

    [HttpPost("updatePassword")]
    [SwaggerOperation(Summary = "Update Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePassword([FromBody] UserPassword userPassword)
    {
        var enabledLoginAuth = Environment.GetEnvironmentVariable("ENABLED_LOGIN_AUTH")?.ToLower() ?? "true";
        if (enabledLoginAuth == "false")
        {
            return BadRequest(new Message("Login is disabled."));
        }

        var user = await _getOrCreateUserService.GetUserOrCreateUser(User);
        if (user == null)
        {
            return BadRequest();
        }

        var validUser = await _userService.LoginWithEmail(user.Email, userPassword.CurrentPassword);

        if (validUser == null)
        {
            return BadRequest("Invalid password");
        }

        await _userService.UpdatePassword(user.Email, userPassword.NewPassword);
        return Ok();
    }

    [HttpGet("GetAll")]
    [SwaggerOperation(Summary = "Get All Users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize()]
    public async Task<IActionResult> GetAll()
    {
        var usrAdmin = await IsUserAdmin();
        if (!usrAdmin)
        {
            return BadRequest(new Message("This user is not authorized to do this operation"));
        }

        return Ok(await _userService.GetAll());
    }
    
    [HttpGet("GetAllByGroupId/{groupId}")]
    [SwaggerOperation(Summary = "Get All Users by GroupId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize()]
    public async Task<IActionResult> GetAllByGroupId(int groupId)
    {
        return Ok(await _userService.GetAllByGroupId(groupId));
    }

    private async Task<bool> IsUserAdmin()
    {
        var usr = await _getOrCreateUserService.GetUserOrCreateUser(User);
        if (usr == null)
        {
            return false;
        }

        if (!usr.IsAdmin)
        {
            return false;
        }

        return usr.IsAdmin;
    }

    [HttpGet("GetById/{userId}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid userId)
    {
        return Ok(await _userService.Get(userId));
    }

    [HttpGet("GetByEmail/{userEmail}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByEmail(string userEmail)
    {
        return Ok(await _userService.GetByEmail(userEmail));
    }

    [HttpGet("GetByUserName/{userName}")]
    [SwaggerOperation(Summary = "Get User by Id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByUsername(string userName)
    {
        return Ok(await _userService.GetByUsername(userName));
    }

    [HttpGet("{email}")]
    [SwaggerOperation(Summary =
        "Returns user by email. If user does not exist, it will create a new user. (Azure AD Login) - JWT Token required")]
    [Authorize]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    public async Task<ActionResult> Get(string email)
    {
        var result = await _userService.GetByEmail(email);
        if (ReferenceEquals(result, null))
        {
            result = await _getOrCreateUserService.GetUserOrCreateUser(User);
        }

        return Ok(result);
    }

    [HttpGet("GetUserCount")]
    [SwaggerOperation(Summary = "Get Count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserCount()
    {
        var users = await _userService.GetAll();
        return Ok(users?.Count());
    }

    [HttpGet("GetUserDetailsByToken")]
    [SwaggerOperation(Summary = "GetUserDetailsByToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDetailsByToken()
    {
        return Ok(await GetUserByToken());
    }

    [HttpPost("UpdateUserDeviceToken")]
    [SwaggerOperation(Summary = "Update user device token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserDeviceToken([FromBody] UserDeviceToken userDeviceToken)
    {
        var user = await GetUserByToken();
        if (user == null)
        {
            return BadRequest();
        }

        await _userService.UpdateUserDeviceToken(userDeviceToken.DeviceToken, user.Id);
        return Ok();
    }
    
    [HttpGet("GetUserDeviceTokenList")]
    [SwaggerOperation(Summary = "GetUserDeviceTokenList by Bearer Token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDeviceTokenList()
    {
        var user = await GetUserByToken();
        if (user == null)
        {
            return BadRequest();
        }

        return Ok(await _userService.GetUserDeviceTokenList(user.Id));
    }
    
    [HttpGet("GetUserDeviceTokenListByUserId/{userId}")]
    [SwaggerOperation(Summary = "GetUserDeviceTokenList by userId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDeviceTokenListByUserId(Guid userId)
    {
        return Ok(await _userService.GetUserDeviceTokenList(userId));
    }
    
    [HttpGet("GetUserDeviceTokenListByGroupId/{groupId}")]
    [SwaggerOperation(Summary = "GetUserDeviceTokenList by groupId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDeviceTokenListByGroupId(int groupId)
    {
        return Ok(await _userService.GetUserDeviceTokenListByGroupId(groupId));
    }

    private async Task<UserDto?> GetUserByToken()
    {
        return await _getOrCreateUserService.GetUserOrCreateUser(User);
    }
}