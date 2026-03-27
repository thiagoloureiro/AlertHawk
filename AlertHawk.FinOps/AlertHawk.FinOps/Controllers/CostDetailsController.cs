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
    public class CostDetailsController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<CostDetailsController> _logger;

        public CostDetailsController(
            FinOpsDbContext context,
            ILogger<CostDetailsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get cost details for a specific analysis run
        /// </summary>
        [HttpGet("analysis/{analysisRunId}")]
        public async Task<ActionResult<IEnumerable<CostDetail>>> GetCostDetailsByAnalysisRun(int analysisRunId)
        {
            try
            {
                var costDetails = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == analysisRunId)
                    .OrderByDescending(c => c.Cost)
                    .ToListAsync();

                return Ok(costDetails);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving cost details for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get cost details by type (ResourceGroup or Service)
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/type/{costType}")]
        public async Task<ActionResult<IEnumerable<CostDetail>>> GetCostDetailsByType(
            int analysisRunId,
            string costType)
        {
            try
            {
                var costDetails = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == analysisRunId && c.CostType == costType)
                    .OrderByDescending(c => c.Cost)
                    .ToListAsync();

                return Ok(costDetails);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving cost details by type for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get top N most expensive items
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/top/{count}")]
        public async Task<ActionResult<IEnumerable<CostDetail>>> GetTopCostDetails(
            int analysisRunId,
            int count = 10)
        {
            try
            {
                var costDetails = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == analysisRunId)
                    .OrderByDescending(c => c.Cost)
                    .Take(count)
                    .ToListAsync();

                return Ok(costDetails);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving top cost details for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get cost summary by resource group
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/summary/resourcegroups")]
        public async Task<ActionResult> GetCostSummaryByResourceGroup(int analysisRunId)
        {
            try
            {
                var summary = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == analysisRunId && c.CostType == "ResourceGroup")
                    .GroupBy(c => c.Name)
                    .Select(g => new
                    {
                        ResourceGroup = g.Key,
                        TotalCost = g.Sum(c => c.Cost),
                        ItemCount = g.Count()
                    })
                    .OrderByDescending(s => s.TotalCost)
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving cost summary by resource group");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get cost summary by service
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/summary/services")]
        public async Task<ActionResult> GetCostSummaryByService(int analysisRunId)
        {
            try
            {
                var summary = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == analysisRunId && c.CostType == "Service")
                    .GroupBy(c => c.Name)
                    .Select(g => new
                    {
                        Service = g.Key,
                        TotalCost = g.Sum(c => c.Cost),
                        ItemCount = g.Count()
                    })
                    .OrderByDescending(s => s.TotalCost)
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving cost summary by service");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}