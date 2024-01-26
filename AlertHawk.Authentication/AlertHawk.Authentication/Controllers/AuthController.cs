using System.Reflection;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Authentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Authenticate User")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostUserAuth([FromBody] UserAuth userAuth)
        {
            // call service, and check on the database if username and password exists.
            return Ok();
        }
        
        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create User")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostUserCreation([FromBody] UserCreation userCreation)
        {
            // call database and store user credentials
            return Ok();
        }
        
        [HttpPut("update")]
        [SwaggerOperation(Summary = "Update User")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> PutUserUpdate([FromBody] UserCreation userCreation)
        {
            // call database and update user credentials
            return Ok();
        }
    }
}