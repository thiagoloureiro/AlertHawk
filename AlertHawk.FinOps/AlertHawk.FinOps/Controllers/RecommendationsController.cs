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
    public class RecommendationsController : ControllerBase
    {
        private readonly FinOpsDbContext _context;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            FinOpsDbContext context,
            ILogger<RecommendationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get AI recommendations for a specific analysis run
        /// </summary>
        [HttpGet("analysis/{analysisRunId}")]
        public async Task<ActionResult<IEnumerable<AiRecommendation>>> GetRecommendationsByAnalysisRun(int analysisRunId)
        {
            try
            {
                var recommendations = await _context.AiRecommendations
                    .Where(r => r.AnalysisRunId == analysisRunId)
                    .OrderByDescending(r => r.RecordedAt)
                    .ToListAsync();

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommendations for analysis run {AnalysisRunId}", analysisRunId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get the latest AI recommendation for an analysis run
        /// </summary>
        [HttpGet("analysis/{analysisRunId}/latest")]
        public async Task<ActionResult<AiRecommendation>> GetLatestRecommendation(int analysisRunId)
        {
            try
            {
                var recommendation = await _context.AiRecommendations
                    .Where(r => r.AnalysisRunId == analysisRunId)
                    .OrderByDescending(r => r.RecordedAt)
                    .FirstOrDefaultAsync();

                if (recommendation == null)
                {
                    return NotFound();
                }

                return Ok(recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest recommendation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get formatted recommendation text
        /// </summary>
        [HttpGet("{id}/formatted")]
        public async Task<ActionResult<string>> GetFormattedRecommendation(int id)
        {
            try
            {
                var recommendation = await _context.AiRecommendations.FindAsync(id);

                if (recommendation == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    Id = recommendation.Id,
                    AnalysisRunId = recommendation.AnalysisRunId,
                    FormattedText = recommendation.GetFormattedText(),
                    Summary = recommendation.GetSummary(),
                    Model = recommendation.Model,
                    RecordedAt = recommendation.RecordedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving formatted recommendation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all recommendations
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AiRecommendation>>> GetAllRecommendations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.AiRecommendations
                    .Include(r => r.AnalysisRun)
                    .OrderByDescending(r => r.RecordedAt);

                var total = await query.CountAsync();
                var recommendations = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", total.ToString());
                Response.Headers.Append("X-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all recommendations");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}