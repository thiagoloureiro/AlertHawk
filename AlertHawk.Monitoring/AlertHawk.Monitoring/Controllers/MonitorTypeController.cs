using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorTypeController : ControllerBase
    {
        private readonly IMonitorTypeService _monitorTypeService;
        private readonly ICaching _caching;

        public MonitorTypeController(IMonitorTypeService monitorTypeService, ICaching caching)
        {
            _monitorTypeService = monitorTypeService;
            _caching = caching;
        }

        [HttpGet]
        public async Task<IActionResult> GetMonitorType()
        {
            var result = await
                _caching.GetOrSetObjectFromCacheAsync("monitorTypeList", 60,
                    () => _monitorTypeService.GetMonitorType());
            return Ok(result);
        }
    }
}