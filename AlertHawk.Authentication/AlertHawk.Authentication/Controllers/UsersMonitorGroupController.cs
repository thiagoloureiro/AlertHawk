using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersMonitorGroupController : Controller
    {
        private readonly GetOrCreateUserHelper _getOrCreateUserHelper;
        private readonly IUsersMonitorGroupService _usersMonitorGroupService;

        public UsersMonitorGroupController(IUserService userService, IUsersMonitorGroupService usersMonitorGroupService)
        {
            _getOrCreateUserHelper = new GetOrCreateUserHelper(userService);
            _usersMonitorGroupService = usersMonitorGroupService;
        }

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create UsersMonitorGroupController")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignUserToGroup([FromBody] List<UsersMonitorGroup> usersMonitorGroup)
        {
            var usr = await GetUserByToken();

            if (usr != null && !usr.IsAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _usersMonitorGroupService.CreateOrUpdateAsync(usersMonitorGroup);
                return Ok(new Message("Settings saved successfully."));
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

        [HttpGet("GetAll")]
        [SwaggerOperation(Summary = "Get All Monitor Group Id By UserId Inside User Token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize()]
        public async Task<IActionResult> GetAll()
        {
            var usrAdmin = await GetUserByToken();
            if (usrAdmin != null)
            {
                return Ok(await _usersMonitorGroupService.GetAsync(usrAdmin.Id));
            }

            return Ok();
        }

        [HttpGet("GetAllByUserId/{userId}")]
        [SwaggerOperation(Summary = "Get All Monitor Group Id By UserId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize()]
        public async Task<IActionResult> GetAllByUserId(Guid userId)
        {
            var usrAdmin = await GetUserByToken();
            if (usrAdmin != null && !usrAdmin.IsAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            return Ok(await _usersMonitorGroupService.GetAsync(userId));
        }

        private async Task<UserDto?> GetUserByToken()
        {
            return await _getOrCreateUserHelper.GetUserOrCreateUser(User);
        }
    }
}