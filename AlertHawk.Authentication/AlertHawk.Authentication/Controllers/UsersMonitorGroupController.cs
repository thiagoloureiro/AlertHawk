using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersMonitorGroupController : Controller
    {
        private readonly IGetOrCreateUserService _getOrCreateUserService;
        private readonly IUsersMonitorGroupService _usersMonitorGroupService;

        public UsersMonitorGroupController(IUsersMonitorGroupService usersMonitorGroupService, IGetOrCreateUserService getOrCreateUserService)
        {
            _getOrCreateUserService = getOrCreateUserService;
            _usersMonitorGroupService = usersMonitorGroupService;
        }
        
        [HttpPost("AssignUserToGroup")]
        [SwaggerOperation(Summary = "AssignUserToGroup after group Creation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignUserToGroup([FromBody] UsersMonitorGroup usersMonitorGroup)
        {
            var usr = await GetUserByToken();
            if (usr != null) usersMonitorGroup.UserId = usr.Id;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _usersMonitorGroupService.AssignUserToGroup(usersMonitorGroup);
                return Ok();
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

        [HttpPost("create")]
        [SwaggerOperation(Summary = "AssignUserToGroups")]
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
                return Ok();
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
            var userDetails = await GetUserByToken();
            if (userDetails != null)
            {
                var userGroups = await _usersMonitorGroupService.GetAsync(userDetails.Id);
                return Ok(userGroups);
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

        [HttpDelete("{groupMonitorId}")]
        [SwaggerOperation(Summary = "Delete all group monitor relationship by groupMonitorId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize()]
        public async Task<IActionResult> DeleteMonitorGroupByGroupMonitorId(int groupMonitorId)
        {
            var usrAdmin = await IsUserAdmin();
            if (usrAdmin == null)
            {
                await _usersMonitorGroupService.DeleteAllByGroupMonitorIdAsync(groupMonitorId);
                return Ok();
            }
            else
            {
                return usrAdmin;
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

            return null; // or return a default value if needed
        }
        private async Task<UserDto?> GetUserByToken()
        {
            return await _getOrCreateUserService.GetUserOrCreateUser(User);
        }
    }
}