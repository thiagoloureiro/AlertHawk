using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Sentry;

namespace AlertHawk.Authentication.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserSubscriptionsController : Controller
{
    private readonly IGetOrCreateUserService _getOrCreateUserService;
    private readonly IUserSubscriptionsService _userSubscriptionsService;

    public UserSubscriptionsController(
        IUserSubscriptionsService userSubscriptionsService,
        IGetOrCreateUserService getOrCreateUserService)
    {
        _getOrCreateUserService = getOrCreateUserService;
        _userSubscriptionsService = userSubscriptionsService;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Add a subscription to a user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] UserSubscriptions userSubscription)
    {
        var usrAdmin = await IsUserAdmin();
        if (usrAdmin != null)
        {
            return usrAdmin;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userSubscriptionsService.CreateAsync(userSubscription);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new Message(ex.Message));
        }
        catch (Exception err)
        {
            SentrySdk.CaptureException(err);
            return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
        }
    }

    [HttpPost("CreateOrUpdate")]
    [SwaggerOperation(
        Summary = "Add or update multiple subscriptions for a user. Send empty list to remove all subscriptions.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateUserSubscriptionsRequest request)
    {
        var usrAdmin = await IsUserAdmin();
        if (usrAdmin != null)
        {
            return usrAdmin;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.Subscriptions == null)
        {
            return BadRequest(new Message("Subscriptions list cannot be null"));
        }

        try
        {
            await _userSubscriptionsService.CreateOrUpdateAsync(request.UserId, request.Subscriptions);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new Message(ex.Message));
        }
        catch (Exception err)
        {
            SentrySdk.CaptureException(err);
            return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
        }
    }

    [HttpGet("GetAllByUserId/{userId}")]
    [SwaggerOperation(Summary = "Get all user subscriptions by userId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllByUserId(Guid userId)
    {
        var userDetails = await GetUserByToken();
        if (userDetails == null)
        {
            return Unauthorized(new Message("User not found"));
        }

        if (userDetails.Id != userId && !userDetails.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new Message("This user is not authorized to view this operation"));
        }

        try
        {
            var subscriptions = await _userSubscriptionsService.GetByUserIdAsync(userId);
            return Ok(subscriptions);
        }
        catch (Exception err)
        {
            SentrySdk.CaptureException(err);
            return StatusCode(StatusCodes.Status500InternalServerError, new Message("Something went wrong."));
        }
    }

    private async Task<ObjectResult?> IsUserAdmin()
    {
        var usr = await _getOrCreateUserService.GetUserOrCreateUser(User);
        if (usr != null && !usr.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new Message("This user is not authorized to do this operation"));
        }

        return null;
    }

    private async Task<Domain.Dto.UserDto?> GetUserByToken()
    {
        return await _getOrCreateUserService.GetUserOrCreateUser(User);
    }
}

public class CreateOrUpdateUserSubscriptionsRequest
{
    public Guid UserId { get; set; }
    public List<Guid> Subscriptions { get; set; } = new();
}
