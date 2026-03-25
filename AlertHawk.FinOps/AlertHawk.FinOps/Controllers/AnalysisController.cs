using FinOpsToolSample.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FinOpsToolSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalysisController : ControllerBase
    {
        private readonly ILogger<AnalysisController> _logger;
        private readonly IAnalysisOrchestrationService _analysisService;
        private readonly IAnalysisJobService _analysisJobService;
        private readonly IConfiguration _configuration;

        public AnalysisController(
            ILogger<AnalysisController> logger,
            IAnalysisOrchestrationService analysisService,
            IAnalysisJobService analysisJobService,
            IConfiguration configuration)
        {
            _logger = logger;
            _analysisService = analysisService;
            _analysisJobService = analysisJobService;
            _configuration = configuration;
        }

        /// <summary>
        /// Triggers a new Azure FinOps analysis run for a specific subscription
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription ID to analyze</param>
        /// <returns>Response with analysis results</returns>
        [HttpPost("start")]
        public async Task<IActionResult> StartAnalysis([FromBody] string subscriptionId)
        {
            _logger.LogInformation("Starting analysis for subscription: {SubscriptionId}", subscriptionId);

            try
            {
                if (string.IsNullOrWhiteSpace(subscriptionId))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Subscription ID is required"
                    });
                }

                var result = await _analysisService.RunAnalysisForSingleSubscriptionAsync(subscriptionId);

                _logger.LogInformation("Completed analysis for subscription {SubscriptionId}: {Message}",
                    subscriptionId, result.Message);

                return Ok(new
                {
                    Success = result.Success,
                    SubscriptionId = result.SubscriptionId,
                    SubscriptionName = result.SubscriptionName,
                    AnalysisRunId = result.AnalysisRunId,
                    ResourcesAnalyzed = result.ResourcesAnalyzed,
                    TotalMonthlyCost = result.TotalMonthlyCost,
                    Message = result.Message,
                    ErrorDetails = result.ErrorDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running analysis");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Starts analysis in the background and returns a job id. Poll GET jobs/{jobId} until status is completed or failed.
        /// </summary>
        [HttpPost("start-async")]
        public IActionResult StartAnalysisAsync([FromBody] string subscriptionId)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Subscription ID is required"
                });
            }

            var jobId = _analysisJobService.StartAnalysis(subscriptionId.Trim());
            _logger.LogInformation("Queued analysis job {JobId} for subscription {SubscriptionId}", jobId, subscriptionId);

            return AcceptedAtAction(
                nameof(GetAnalysisJobStatus),
                new { jobId },
                new
                {
                    JobId = jobId,
                    subscriptionId = subscriptionId.Trim(),
                    Status = "pending"
                });
        }

        /// <summary>
        /// Returns the current status of an analysis job created via start-async.
        /// </summary>
        [HttpGet("jobs/{jobId:guid}", Name = "GetAnalysisJobStatus")]
        public ActionResult<AnalysisJobStatusDto> GetAnalysisJobStatus(Guid jobId)
        {
            if (!_analysisJobService.TryGetStatus(jobId, out var status) || status == null)
            {
                return NotFound(new { Message = "Unknown or expired job id" });
            }

            return Ok(status);
        }
    }

    [ExcludeFromCodeCoverage]
    public class AnalysisResult
    {
        public bool Success { get; set; }
        // Other properties...

        [MaxLength(255)]
        public string SubscriptionName { get; set; } = string.Empty;
    }
}