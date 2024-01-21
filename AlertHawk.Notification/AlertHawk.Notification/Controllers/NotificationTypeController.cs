using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Services;
using EasyMemoryCache;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Notification.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationTypeController : ControllerBase
    {
        private readonly INotificationTypeService _notificationTypeService;
        private readonly ICaching _caching;
        private readonly string _cacheKey = "GetNotificationTypes";

        public NotificationTypeController(INotificationTypeService notificationTypeService, ICaching caching)
        {
            _notificationTypeService = notificationTypeService;
            _caching = caching;
        }

        [HttpGet("GetNotificationType")]
        [SwaggerOperation(Summary = "Return List of notification types")]
        [ProducesResponseType(typeof(List<NotificationType>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationTypes()
        {
            var result = await _caching.GetOrSetObjectFromCacheAsync(_cacheKey, 60,
                () => _notificationTypeService.SelectNotificationType());

            return Ok(result);
        }

        [HttpGet("GetNotificationType/{id}")]
        [SwaggerOperation(Summary = "Return List of notification type by Id")]
        [ProducesResponseType(typeof(NotificationType), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationTypesById(int id)
        {
            var result = await _notificationTypeService.SelectNotificationTypeById(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("InsertNotificationType")]
        [SwaggerOperation(Summary = "Create a new NotificationSend Type")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> InsertNotificationType(NotificationType notification)
        {
            if (notification.Name != null)
            {
                var notificationByName = await _notificationTypeService.SelectNotificationTypeByName(notification.Name);

                if (notificationByName != null)
                {
                    return BadRequest($"NotificationSend with this name {notification.Name} already exists");
                }
            }

            await _notificationTypeService.InsertNotificationType(notification);
            _caching.Invalidate(_cacheKey);
            return Ok("Item successfully created");
        }

        [HttpPut("UpdateNotificationType")]
        [SwaggerOperation(Summary = "Update a NotificationSend Type")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateNotificationType(NotificationType notification)
        {
            await _notificationTypeService.UpdateNotificationType(notification);
            _caching.Invalidate(_cacheKey);
            return Ok("Item successfully updated");
        }

        [HttpDelete("DeleteNotificationType")]
        [SwaggerOperation(Summary = "Delete a NotificationSend Type")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteNotificationType(int id)
        {
            var notificationTypeById = await _notificationTypeService.SelectNotificationTypeById(id);
            if (notificationTypeById == null)
                return NotFound($"NotificationType not found by Id {id}");

            await _notificationTypeService.DeleteNotificationType(id);
            _caching.Invalidate(_cacheKey);
            return Ok("Item successfully deleted");
        }
    }
}