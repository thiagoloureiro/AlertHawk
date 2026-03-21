using FinOpsToolSample.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FinOpsToolSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalysisController : ControllerBase
    {
        private readonly ILogger<AnalysisController> _logger;
        private readonly AnalysisOrchestrationService _analysisService;
        private readonly IConfiguration _configuration;

        public AnalysisController(
            ILogger<AnalysisController> logger,
            AnalysisOrchestrationService analysisService,
            IConfiguration configuration)
        {
            _logger = logger;
            _analysisService = analysisService;
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
    }

    public class AnalysisResult
    {
        public bool Success { get; set; }
        // Other properties...

        [MaxLength(255)]
        public string SubscriptionName { get; set; } = string.Empty;
    }
}