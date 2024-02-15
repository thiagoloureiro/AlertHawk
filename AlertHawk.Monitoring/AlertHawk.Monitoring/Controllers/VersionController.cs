using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public VersionController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

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