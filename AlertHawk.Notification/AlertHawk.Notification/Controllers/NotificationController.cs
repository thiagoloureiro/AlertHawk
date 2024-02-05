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

        [HttpPost("SendManualNotification")]
        [SwaggerOperation(Summary = "Send Manual notification")]
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
            return Ok("Notification Created Successfully");
        }

        [HttpPut("UpdateNotificationItem")]
        [SwaggerOperation(Summary = "Update notification item")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateNotificationItem(NotificationItem notificationItem)
        {
            await _notificationService.UpdateNotificationItem(notificationItem);
            return Ok("Notification Updated Successfully");
        }

        [HttpDelete("DeleteNotificationItem")]
        [SwaggerOperation(Summary = "Delete notification item")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteNotificationItem(int id)
        {
            await _notificationService.DeleteNotificationItem(id);
            return Ok("Notification Deleted Successfully");
        }

        [HttpGet("SelectNotificationItemList")]
        [SwaggerOperation(Summary = "Select Notification Item List")]
        [ProducesResponseType(typeof(List<NotificationItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SelectNotificationItemList()
        {
            var result = await _notificationService.SelectNotificationItemList();
            return Ok(result);
        }
        
        [HttpPost("SelectNotificationItemListByIds")]
        [SwaggerOperation(Summary = "Select Notification Item List By List of Ids")]
        [ProducesResponseType(typeof(List<NotificationItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SelectNotificationItemList([FromBody] List<int> ids)
        {
            var result = await _notificationService.SelectNotificationItemList(ids);
            return Ok(result);
        }

        [HttpGet("SelectNotificationItemById/{id}")]
        [SwaggerOperation(Summary = "Select NotificationItem By Id")]
        [ProducesResponseType(typeof(NotificationItem), StatusCodes.Status200OK)]
        public async Task<IActionResult> SelectNotificationItemList(int id)
        {
            var result = await _notificationService.SelectNotificationItemById(id);
            return Ok(result);
        }
    }
}