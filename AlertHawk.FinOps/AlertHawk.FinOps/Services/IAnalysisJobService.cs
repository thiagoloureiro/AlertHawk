namespace FinOpsToolSample.Services;

public interface IAnalysisJobService
{
    /// <summary>
    /// Enqueues analysis for the subscription and returns immediately. Work runs in the background.
    /// </summary>
    Guid StartAnalysis(string subscriptionId);

    /// <summary>
    /// Returns false if the job id is unknown (expired or never created).
    /// </summary>
    bool TryGetStatus(Guid jobId, out AnalysisJobStatusDto? status);
}
