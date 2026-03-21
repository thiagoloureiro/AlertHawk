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
    public class AnalysisRunsController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<AnalysisRunsController> _logger;

        public AnalysisRunsController(
            FinOpsDbContext context,
            ILogger<AnalysisRunsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all analysis runs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AnalysisRun>>> GetAnalysisRuns(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.AnalysisRuns
                    .OrderByDescending(a => a.RunDate);

                var total = await query.CountAsync();
                var runs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", total.ToString());
                Response.Headers.Append("X-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(runs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis runs");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific analysis run by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AnalysisRun>> GetAnalysisRun(int id)
        {
            try
            {
                var run = await _context.AnalysisRuns
                    .Include(a => a.CostDetails)
                    .Include(a => a.Resources)
                    .Include(a => a.AiRecommendations)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (run == null)
                {
                    return NotFound();
                }

                return Ok(run);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis run {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get the latest analysis run
        /// </summary>
        [HttpGet("latest")]
        public async Task<ActionResult<AnalysisRun>> GetLatestAnalysisRun()
        {
            try
            {
                var run = await _context.AnalysisRuns
                    .Include(a => a.CostDetails)
                    .Include(a => a.Resources)
                    .Include(a => a.AiRecommendations)
                    .OrderByDescending(a => a.RunDate)
                    .FirstOrDefaultAsync();

                if (run == null)
                {
                    return NotFound();
                }

                return Ok(run);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest analysis run");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get the latest analysis run for each subscription
        /// </summary>
        [HttpGet("latest-per-subscription")]
        public async Task<ActionResult<IEnumerable<AnalysisRun>>> GetLatestAnalysisRunsPerSubscription()
        {
            try
            {
                var latestRuns = await _context.AnalysisRuns
                    .Where(ar => ar.RunDate == _context.AnalysisRuns
                        .Where(ar2 => ar2.SubscriptionId == ar.SubscriptionId)
                        .Max(ar2 => ar2.RunDate))
                    .OrderBy(a => a.SubscriptionName)
                    .ToListAsync();

                return Ok(latestRuns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest analysis runs per subscription");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get analysis runs by subscription ID
        /// </summary>
        [HttpGet("subscription/{subscriptionId}")]
        public async Task<ActionResult<IEnumerable<AnalysisRun>>> GetAnalysisRunsBySubscription(
            string subscriptionId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.AnalysisRuns
                    .Where(a => a.SubscriptionId == subscriptionId)
                    .OrderByDescending(a => a.RunDate);

                var total = await query.CountAsync();
                var runs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", total.ToString());
                Response.Headers.Append("X-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(runs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis runs for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete an analysis run
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnalysisRun(int id)
        {
            try
            {
                var run = await _context.AnalysisRuns.FindAsync(id);
                if (run == null)
                {
                    return NotFound();
                }

                _context.AnalysisRuns.Remove(run);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting analysis run {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}