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
    public class HistoricalCostsController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<HistoricalCostsController> _logger;

        public HistoricalCostsController(
            FinOpsDbContext context,
            ILogger<HistoricalCostsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get historical costs for a specific analysis run
        /// </summary>
        [HttpGet("analysis/{analysisRunId}")]
        public async Task<ActionResult<IEnumerable<HistoricalCost>>> GetHistoricalCostsByAnalysisRun(int analysisRunId)
        {
            try
            {
                var costs = await _context.HistoricalCosts
                    .Where(h => h.AnalysisRunId == analysisRunId)
                    .OrderBy(h => h.CostDate)
                    .ToListAsync();

                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical costs for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get historical costs by subscription
        /// </summary>
        [HttpGet("subscription/{subscriptionId}")]
        public async Task<ActionResult<IEnumerable<HistoricalCost>>> GetHistoricalCostsBySubscription(
            string subscriptionId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var query = _context.HistoricalCosts
                    .Where(h => h.SubscriptionId == subscriptionId);

                if (startDate.HasValue)
                {
                    query = query.Where(h => h.CostDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(h => h.CostDate <= endDate.Value);
                }

                var costs = await query
                    .OrderBy(h => h.CostDate)
                    .ToListAsync();

                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical costs for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get total historical costs grouped by date
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/daily-totals")]
        public async Task<ActionResult> GetDailyTotals(int analysisRunId)
        {
            try
            {
                var dailyTotals = await _context.HistoricalCosts
                    .Where(h => h.AnalysisRunId == analysisRunId && h.CostType == "Total")
                    .OrderBy(h => h.CostDate)
                    .Select(h => new
                    {
                        Date = h.CostDate,
                        Cost = h.Cost,
                        Currency = h.Currency
                    })
                    .ToListAsync();

                return Ok(dailyTotals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily totals for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get historical costs by resource group
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/by-resourcegroup")]
        public async Task<ActionResult> GetHistoricalCostsByResourceGroup(
            int analysisRunId,
            [FromQuery] string? resourceGroup = null)
        {
            try
            {
                var query = _context.HistoricalCosts
                    .Where(h => h.AnalysisRunId == analysisRunId && h.CostType == "ResourceGroup");

                if (!string.IsNullOrEmpty(resourceGroup))
                {
                    query = query.Where(h => h.ResourceGroup == resourceGroup);
                }

                var costs = await query
                    .OrderBy(h => h.CostDate)
                    .ThenBy(h => h.ResourceGroup)
                    .Select(h => new
                    {
                        Date = h.CostDate,
                        ResourceGroup = h.ResourceGroup,
                        Cost = h.Cost,
                        Currency = h.Currency
                    })
                    .ToListAsync();

                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical costs by resource group");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get historical costs by service
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/by-service")]
        public async Task<ActionResult> GetHistoricalCostsByService(
            int analysisRunId,
            [FromQuery] string? serviceName = null)
        {
            try
            {
                var query = _context.HistoricalCosts
                    .Where(h => h.AnalysisRunId == analysisRunId && h.CostType == "Service");

                if (!string.IsNullOrEmpty(serviceName))
                {
                    query = query.Where(h => h.Name == serviceName);
                }

                var costs = await query
                    .OrderBy(h => h.CostDate)
                    .ThenBy(h => h.Name)
                    .Select(h => new
                    {
                        Date = h.CostDate,
                        Service = h.Name,
                        ResourceGroup = h.ResourceGroup,
                        Cost = h.Cost,
                        Currency = h.Currency
                    })
                    .ToListAsync();

                return Ok(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical costs by service");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get cost trend summary
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/trend")]
        public async Task<ActionResult> GetCostTrend(int analysisRunId)
        {
            try
            {
                var dailyTotals = await _context.HistoricalCosts
                    .Where(h => h.AnalysisRunId == analysisRunId && h.CostType == "Total")
                    .OrderBy(h => h.CostDate)
                    .ToListAsync();

                if (dailyTotals.Count < 2)
                {
                    return Ok(new
                    {
                        Message = "Not enough data for trend analysis",
                        DataPoints = dailyTotals.Count
                    });
                }

                var firstCost = dailyTotals.First().Cost;
                var lastCost = dailyTotals.Last().Cost;
                var averageCost = dailyTotals.Average(h => h.Cost);
                var maxCost = dailyTotals.Max(h => h.Cost);
                var minCost = dailyTotals.Min(h => h.Cost);

                var trend = lastCost > firstCost ? "Increasing" : lastCost < firstCost ? "Decreasing" : "Stable";
                var changePercent = firstCost > 0 ? ((lastCost - firstCost) / firstCost) * 100 : 0;

                return Ok(new
                {
                    StartDate = dailyTotals.First().CostDate,
                    EndDate = dailyTotals.Last().CostDate,
                    FirstCost = firstCost,
                    LastCost = lastCost,
                    AverageCost = averageCost,
                    MaxCost = maxCost,
                    MinCost = minCost,
                    Trend = trend,
                    ChangePercent = Math.Round(changePercent, 2),
                    DataPoints = dailyTotals.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cost trend");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}