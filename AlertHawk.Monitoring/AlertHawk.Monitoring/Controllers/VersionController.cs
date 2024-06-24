using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        [SwaggerOperation(Summary = "Retrieve API version")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public string Get()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version!.ToString();
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Post Request test")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public string GetDataPost([FromBody] string value)
        {
            return value;
        }
    }
}