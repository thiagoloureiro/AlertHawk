using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(IUserService userService, IJwtTokenService jwtTokenService)
        {
            _userService = userService;
            _jwtTokenService = jwtTokenService;
        }
        
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Authenticate User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Message), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostUserAuth([FromBody] UserAuth userAuth)
        {
            try
            {
                var user = await _userService.Login(userAuth.Email, userAuth.Password);
            
                if (user is null)
                {
                    return BadRequest(new Message("Invalid credentials."));
                }

                var token = _jwtTokenService.GenerateToken(user);
            
                return Ok(new { token });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
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
            catch (Exception)
            {
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
}