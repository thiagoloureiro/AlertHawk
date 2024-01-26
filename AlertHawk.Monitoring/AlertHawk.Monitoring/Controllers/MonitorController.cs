using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedModels;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public MonitorController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> ProduceNotification(int notificationId, string message)
        {
            for (int i = 0; i < 100; i++)
            {
                await _publishEndpoint.Publish<NotificationAlert>(new
                {
                    NotificationId = notificationId,
                    TimeStamp = DateTime.UtcNow,
                    Message = message + "_" + i
                });
            }

            return Ok();
        }
    }
}