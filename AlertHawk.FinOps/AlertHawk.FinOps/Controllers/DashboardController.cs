using FinOpsToolSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinOpsToolSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            FinOpsDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard summary with key metrics
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetDashboardSummary()
        {
            try
            {
                var latestRun = await _context.AnalysisRuns
                    .OrderByDescending(a => a.RunDate)
                    .FirstOrDefaultAsync();

                if (latestRun == null)
                {
                    return Ok(new
                    {
                        Message = "No analysis runs found. Start a new analysis to see data.",
                        TotalRuns = 0
                    });
                }

                var totalRuns = await _context.AnalysisRuns.CountAsync();

                var resourcesCount = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == latestRun.Id)
                    .CountAsync();

                var resourcesWithFlags = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == latestRun.Id && r.Flags != null && r.Flags != "")
                    .CountAsync();

                var topCostsByResourceGroup = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == latestRun.Id && c.CostType == "ResourceGroup")
                    .OrderByDescending(c => c.Cost)
                    .Take(5)
                    .Select(c => new
                    {
                        c.Name,
                        c.Cost
                    })
                    .ToListAsync();

                var topCostsByService = await _context.CostDetails
                    .Where(c => c.AnalysisRunId == latestRun.Id && c.CostType == "Service")
                    .OrderByDescending(c => c.Cost)
                    .Take(5)
                    .Select(c => new
                    {
                        c.Name,
                        c.Cost
                    })
                    .ToListAsync();

                var hasRecommendations = await _context.AiRecommendations
                    .AnyAsync(r => r.AnalysisRunId == latestRun.Id);

                return Ok(new
                {
                    LatestRun = new
                    {
                        latestRun.Id,
                        latestRun.RunDate,
                        latestRun.SubscriptionName,
                        latestRun.TotalMonthlyCost,
                        latestRun.TotalResourcesAnalyzed
                    },
                    TotalAnalysisRuns = totalRuns,
                    ResourcesAnalyzed = resourcesCount,
                    ResourcesWithIssues = resourcesWithFlags,
                    TopCostsByResourceGroup = topCostsByResourceGroup,
                    TopCostsByService = topCostsByService,
                    HasAiRecommendations = hasRecommendations
                });
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving dashboard summary");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get cost trends over time
        /// </summary>
        [HttpGet("cost-trends")]
        public async Task<ActionResult> GetCostTrends([FromQuery] int months = 6)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddMonths(-months);

                var runs = await _context.AnalysisRuns
                    .Where(a => a.RunDate >= cutoffDate)
                    .OrderBy(a => a.RunDate)
                    .Select(a => new
                    {
                        a.RunDate,
                        a.TotalMonthlyCost,
                        a.TotalResourcesAnalyzed
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Period = $"Last {months} months",
                    DataPoints = runs.Count,
                    Trends = runs
                });
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving cost trends");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get resource type distribution
        /// </summary>
        [HttpGet("resource-distribution")]
        public async Task<ActionResult> GetResourceDistribution()
        {
            try
            {
                var latestRun = await _context.AnalysisRuns
                    .OrderByDescending(a => a.RunDate)
                    .FirstOrDefaultAsync();

                if (latestRun == null)
                {
                    return NotFound("No analysis runs found");
                }

                var distribution = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == latestRun.Id)
                    .GroupBy(r => r.ResourceType)
                    .Select(g => new
                    {
                        ResourceType = g.Key,
                        Count = g.Count(),
                        Percentage = 0.0 // Will calculate after
                    })
                    .ToListAsync();

                var total = distribution.Sum(d => d.Count);
                var result = distribution.Select(d => new
                {
                    d.ResourceType,
                    d.Count,
                    Percentage = total > 0 ? Math.Round((double)d.Count / total * 100, 2) : 0
                }).OrderByDescending(d => d.Count).ToList();

                return Ok(new
                {
                    AnalysisRunId = latestRun.Id,
                    RunDate = latestRun.RunDate,
                    TotalResources = total,
                    Distribution = result
                });
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving resource distribution");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get optimization opportunities summary
        /// </summary>
        [HttpGet("optimization-opportunities")]
        public async Task<ActionResult> GetOptimizationOpportunities()
        {
            try
            {
                var latestRun = await _context.AnalysisRuns
                    .OrderByDescending(a => a.RunDate)
                    .FirstOrDefaultAsync();

                if (latestRun == null)
                {
                    return NotFound("No analysis runs found");
                }

                // Resources with flags (issues)
                var resourcesWithFlags = await _context.ResourceAnalysis
                    .Where(r => r.AnalysisRunId == latestRun.Id && r.Flags != null && r.Flags != "")
                    .GroupBy(r => r.ResourceType)
                    .Select(g => new
                    {
                        ResourceType = g.Key,
                        Count = g.Count(),
                        Issues = g.Select(r => new
                        {
                            r.ResourceName,
                            r.Flags
                        }).ToList()
                    })
                    .ToListAsync();

                var totalOpportunities = resourcesWithFlags.Sum(r => r.Count);

                return Ok(new
                {
                    AnalysisRunId = latestRun.Id,
                    RunDate = latestRun.RunDate,
                    TotalOpportunities = totalOpportunities,
                    ByResourceType = resourcesWithFlags
                });
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Error retrieving optimization opportunities");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}