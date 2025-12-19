using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Metrics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ExcludeFromCodeCoverage]
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
    }
}