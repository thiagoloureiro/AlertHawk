using FinOpsToolSample.Data;
using FinOpsToolSample.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinOpsToolSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            FinOpsDbContext context,
            ILogger<SubscriptionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get distinct subscriptions seen in analysis runs (one row per subscription id; name from the latest run).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionSummary>>> GetSubscriptions()
        {
            try
            {
                var subscriptions = await _context.AnalysisRuns
                    .AsNoTracking()
                    .GroupBy(a => a.SubscriptionId)
                    .Select(g => new SubscriptionSummary
                    {
                        SubscriptionId = g.Key,
                        SubscriptionName = g.OrderByDescending(a => a.RunDate)
                            .Select(a => a.SubscriptionName)
                            .First()
                    })
                    .OrderBy(s => s.SubscriptionName)
                    .ThenBy(s => s.SubscriptionId)
                    .ToListAsync();

                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving distinct subscriptions from analysis runs");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
