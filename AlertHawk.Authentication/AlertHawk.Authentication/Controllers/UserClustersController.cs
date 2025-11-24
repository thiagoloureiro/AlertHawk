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
public class UserClustersController : Controller
{
    private readonly IGetOrCreateUserService _getOrCreateUserService;
    private readonly IUserClustersService _userClustersService;

    public UserClustersController(IUserClustersService userClustersService, IGetOrCreateUserService getOrCreateUserService)
    {
        _getOrCreateUserService = getOrCreateUserService;
        _userClustersService = userClustersService;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Add a cluster to a user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] UserClusters userCluster)
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
            await _userClustersService.CreateAsync(userCluster);
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
    [SwaggerOperation(Summary = "Add or update multiple clusters for a user. Send empty list to remove all clusters.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateUserClustersRequest request)
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

        if (request.Clusters == null)
        {
            return BadRequest(new Message("Clusters list cannot be null"));
        }

        try
        {
            await _userClustersService.CreateOrUpdateAsync(request.UserId, request.Clusters);
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
    [SwaggerOperation(Summary = "Get all user clusters by userId")]
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

        // Users can only see their own clusters, admins can see any user's clusters
        if (userDetails.Id != userId && !userDetails.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new Message("This user is not authorized to view this operation"));
        }

        try
        {
            var clusters = await _userClustersService.GetByUserIdAsync(userId);
            return Ok(clusters);
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

public class CreateOrUpdateUserClustersRequest
{
    public Guid UserId { get; set; }
    public List<string> Clusters { get; set; } = new();
}

