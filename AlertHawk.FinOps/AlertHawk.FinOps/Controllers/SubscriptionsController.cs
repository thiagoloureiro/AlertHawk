using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinOpsToolSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(
            FinOpsDbContext context,
            ILogger<SubscriptionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all subscriptions with descriptions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .OrderBy(s => s.SubscriptionId)
                    .ToListAsync();

                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions");
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific subscription by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Subscription>> GetSubscription(int id)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);

                if (subscription == null)
                {
                    return NotFound(new { Message = "Subscription not found" });
                }

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription {Id}", id);
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }

        /// <summary>
        /// Get a subscription by subscription ID
        /// </summary>
        [HttpGet("by-subscription-id/{subscriptionId}")]
        public async Task<ActionResult<Subscription>> GetSubscriptionBySubscriptionId(string subscriptionId)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

                if (subscription == null)
                {
                    return NotFound(new { Message = "Subscription not found" });
                }

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }

        /// <summary>
        /// Create or update a subscription description
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Subscription>> CreateOrUpdateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubscriptionId))
                {
                    return BadRequest(new { Message = "SubscriptionId is required" });
                }

                var existing = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == dto.SubscriptionId);

                if (existing != null)
                {
                    existing.Description = dto.Description ?? string.Empty;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.Subscriptions.Update(existing);
                }
                else
                {
                    var subscription = new Subscription
                    {
                        SubscriptionId = dto.SubscriptionId.Trim(),
                        Description = dto.Description ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Subscriptions.Add(subscription);
                    existing = subscription;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Subscription {SubscriptionId} created or updated", dto.SubscriptionId);

                return Ok(existing);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true || 
                                               ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                _logger.LogError(ex, "Duplicate subscription ID {SubscriptionId}", dto.SubscriptionId);
                return Conflict(new { Message = "A subscription with this ID already exists" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating subscription");
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }

        /// <summary>
        /// Update a subscription description
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto dto)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);

                if (subscription == null)
                {
                    return NotFound(new { Message = "Subscription not found" });
                }

                subscription.Description = dto.Description ?? string.Empty;
                subscription.UpdatedAt = DateTime.UtcNow;

                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription {Id} updated", id);

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {Id}", id);
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }

        /// <summary>
        /// Delete a subscription
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);

                if (subscription == null)
                {
                    return NotFound(new { Message = "Subscription not found" });
                }

                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription {Id} deleted", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {Id}", id);
                return StatusCode(500, new { Message = "Internal server error", ErrorDetails = ex.Message });
            }
        }
    }

    public class CreateSubscriptionDto
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateSubscriptionDto
    {
        public string Description { get; set; } = string.Empty;
    }
}
