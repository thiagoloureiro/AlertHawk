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
    public class ResourcesController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(
            FinOpsDbContext context,
            ILogger<ResourcesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all resources for a specific analysis run
        /// </summary>
        [HttpGet("analysis/{analysisRunId}")]
        public async Task<ActionResult<IEnumerable<ResourceAnalysis>>> GetResourcesByAnalysisRun(
            int analysisRunId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId)
                    .OrderBy(r => r.ResourceType)
                    .ThenBy(r => r.ResourceName);

                var total = await query.CountAsync();
                var resources = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", total.ToString());
                Response.Headers.Append("X-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(resources);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resources for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resources by type
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/type/{resourceType}")]
        public async Task<ActionResult<IEnumerable<ResourceAnalysis>>> GetResourcesByType(
            int analysisRunId,
            string resourceType)
        {
            try
            {
                var resources = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId && r.ResourceType == resourceType)
                    .OrderBy(r => r.ResourceName)
                    .ToListAsync();

                return Ok(resources);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resources by type");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resources by resource group
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/resourcegroup/{resourceGroup}")]
        public async Task<ActionResult<IEnumerable<ResourceAnalysis>>> GetResourcesByResourceGroup(
            int analysisRunId,
            string resourceGroup)
        {
            try
            {
                var resources = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId && r.ResourceGroup == resourceGroup)
                    .OrderBy(r => r.ResourceType)
                    .ThenBy(r => r.ResourceName)
                    .ToListAsync();

                return Ok(resources);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resources by resource group");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resources with specific flags
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/flags")]
        public async Task<ActionResult<IEnumerable<ResourceAnalysis>>> GetResourcesWithFlags(
            int analysisRunId,
            [FromQuery] string? flagContains = null)
        {
            try
            {
                var query = _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId && r.Flags != null && r.Flags != "");

                if (!string.IsNullOrEmpty(flagContains))
                {
                    query = query.Where(r => r.Flags!.Contains(flagContains));
                }

                var resources = await query
                    .OrderBy(r => r.ResourceType)
                    .ThenBy(r => r.ResourceName)
                    .ToListAsync();

                return Ok(resources);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resources with flags");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resource type summary
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/summary/types")]
        public async Task<ActionResult> GetResourceTypeSummary(int analysisRunId)
        {
            try
            {
                var summary = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId)
                    .GroupBy(r => r.ResourceType)
                    .Select(g => new
                    {
                        ResourceType = g.Key,
                        Count = g.Count(),
                        WithFlags = g.Count(r => r.Flags != null && r.Flags != "")
                    })
                    .OrderByDescending(s => s.Count)
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resource type summary");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resource group summary
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/summary/resourcegroups")]
        public async Task<ActionResult> GetResourceGroupSummary(int analysisRunId)
        {
            try
            {
                var summary = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId)
                    .GroupBy(r => r.ResourceGroup)
                    .Select(g => new
                    {
                        ResourceGroup = g.Key,
                        Count = g.Count(),
                        Types = g.Select(r => r.ResourceType).Distinct().Count(),
                        WithFlags = g.Count(r => r.Flags != null && r.Flags != "")
                    })
                    .OrderByDescending(s => s.Count)
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resource group summary");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search resources by name
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/search")]
        public async Task<ActionResult<IEnumerable<ResourceAnalysis>>> SearchResources(
            int analysisRunId,
            [FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var resources = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == analysisRunId &&
                               (r.ResourceName.Contains(searchTerm) ||
                                r.ResourceGroup.Contains(searchTerm) ||
                                r.ResourceType.Contains(searchTerm)))
                    .OrderBy(r => r.ResourceType)
                    .ThenBy(r => r.ResourceName)
                    .ToListAsync();

                return Ok(resources);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error searching resources");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}