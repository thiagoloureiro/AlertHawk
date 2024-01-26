using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Notification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("SendNotification")]
        [SwaggerOperation(Summary = "Send notification")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendNotification(NotificationSend notification)
        {
            var result = await _notificationService.Send(notification);
            return Ok(result);
        }

        [HttpPost("CreateNotificationItem")]
        [SwaggerOperation(Summary = "Create notification item")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> InsertNotificationItem(NotificationItem notificationItem)
        {
            await _notificationService.InsertNotificationItem(notificationItem);
            return Ok();
        }

        private async Task TaskToHangFireSend(int notificationItemId, string message)
        {
            var notificationItem = await _notificationService.SelectNotificationItemById(notificationItemId);

            var notificationSend = new NotificationSend
            {
                NotificationEmail = notificationItem.NotificationEmail,
                Message = message
            };
            await _notificationService.Send(notificationSend);
        }
    }
}