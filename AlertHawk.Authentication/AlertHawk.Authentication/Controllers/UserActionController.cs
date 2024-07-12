using AlertHawk.Application.Interfaces;
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
    public class UserActionController : Controller
    {
        private readonly IUserActionService _userActionService;
        private readonly IGetOrCreateUserService _getOrCreateUserHelper;

        public UserActionController(IUserActionService userActionService, IGetOrCreateUserService getOrCreateUserHelper)
        {
            _getOrCreateUserHelper = getOrCreateUserHelper;
            _userActionService = userActionService;
        }
        
        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create User Action")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostUserActionCreation([FromBody] UserAction userAction)
        {
            if (string.IsNullOrEmpty(userAction.Action))
            {
                return BadRequest("Action is required");
            }

            var user = await GetUserByToken();
            if (user == null)
            {
                return BadRequest("User/Token not found");
            }

            userAction.UserId = user.Id;

            await _userActionService.CreateAsync(userAction);
            return Ok();
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of user actions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserActions()
        {
            var userActions = await _userActionService.GetAsync();
            return Ok(userActions);
        }

        private async Task<UserDto?> GetUserByToken()
        {
            return await _getOrCreateUserHelper.GetUserOrCreateUser(User);
        }
    }
}