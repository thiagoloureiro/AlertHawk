using FinOpsToolSample.Data;
using Microsoft.EntityFrameworkCore;

namespace FinOpsToolSample.Services;

public class DataCleanupService : IDataCleanupService
{
    private readonly FinOpsDbContext _dbContext;
    private readonly ILogger<DataCleanupService> _logger;

    public DataCleanupService(FinOpsDbContext dbContext, ILogger<DataCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CleanupResult> CleanupOldAnalysisRunsAsync()
    {
        var result = new CleanupResult();

        try
        {
            _logger.LogInformation("Starting cleanup of old analysis runs");

            // Get all subscriptions with their runs
            var subscriptionsWithRuns = await _dbContext.AnalysisRuns
                .GroupBy(ar => ar.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    RunCount = g.Count(),
                    LatestRunId = g.OrderByDescending(ar => ar.RunDate).First().Id
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} subscriptions with analysis runs", subscriptionsWithRuns.Count);

            // Get IDs of runs to delete (all except the latest per subscription)
            var runIdsToKeep = subscriptionsWithRuns.Select(s => s.LatestRunId).ToList();
            result.AnalysisRunsKept = runIdsToKeep.Count;

            var runsToDelete = await _dbContext.AnalysisRuns
                .Where(ar => !runIdsToKeep.Contains(ar.Id))
                .Select(ar => new
                {
                    ar.Id,
                    ar.SubscriptionId,
                    ar.SubscriptionName,
                    ar.RunDate
                })
                .ToListAsync();

            if (!runsToDelete.Any())
            {
                _logger.LogInformation("No old analysis runs to delete");
                result.Success = true;
                result.Message = "No old analysis runs found to delete. All subscriptions have only their latest run.";
                return result;
            }

            _logger.LogInformation("Found {Count} analysis runs to delete", runsToDelete.Count);

            // Delete the old runs (cascade delete will handle related records)
            foreach (var runToDelete in runsToDelete)
            {
                var runEntity = await _dbContext.AnalysisRuns.FindAsync(runToDelete.Id);
                if (runEntity != null)
                {
                    _dbContext.AnalysisRuns.Remove(runEntity);
                    result.DeletedRunDetails.Add(
                        $"Run #{runToDelete.Id} - Subscription: {runToDelete.SubscriptionName} ({runToDelete.SubscriptionId}) - Date: {runToDelete.RunDate:yyyy-MM-dd HH:mm:ss}"
                    );
                }
            }

            await _dbContext.SaveChangesAsync();
            result.AnalysisRunsDeleted = runsToDelete.Count;
            result.Success = true;
            result.Message = $"Successfully deleted {result.AnalysisRunsDeleted} old analysis run(s). Kept {result.AnalysisRunsKept} latest run(s) (one per subscription).";

            _logger.LogInformation(
                "Cleanup completed: Deleted {Deleted} runs, Kept {Kept} runs",
                result.AnalysisRunsDeleted,
                result.AnalysisRunsKept
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of old analysis runs");
            result.Success = false;
            result.Message = "Failed to cleanup old analysis runs";
            result.ErrorDetails = ex.Message;
        }

        return result;
    }
}
