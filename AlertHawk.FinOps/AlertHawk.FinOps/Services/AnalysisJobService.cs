using System.Collections.Concurrent;

namespace FinOpsToolSample.Services;

/// <summary>
/// In-memory analysis jobs. Status is lost if the process restarts.
/// </summary>
public sealed class AnalysisJobService : IAnalysisJobService
{
    private static readonly TimeSpan JobTtl = TimeSpan.FromHours(24);
    private static readonly int MaxJobs = 500;

    private readonly ConcurrentDictionary<Guid, JobEntry> _jobs = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisJobService> _logger;

    public AnalysisJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<AnalysisJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Guid StartAnalysis(string subscriptionId)
    {
        TrimIfNeeded();

        var jobId = Guid.NewGuid();
        var entry = new JobEntry
        {
            JobId = jobId,
            SubscriptionId = subscriptionId,
            CreatedAt = DateTimeOffset.UtcNow,
            Phase = AnalysisJobPhase.Pending
        };

        _jobs[jobId] = entry;
        _ = RunAsync(entry);
        return jobId;
    }

    public bool TryGetStatus(Guid jobId, out AnalysisJobStatusDto? status)
    {
        if (!_jobs.TryGetValue(jobId, out var entry))
        {
            status = null;
            return false;
        }

        lock (entry.Gate)
        {
            status = ToDto(entry);
            return true;
        }
    }

    private async Task RunAsync(JobEntry entry)
    {
        lock (entry.Gate)
        {
            entry.Phase = AnalysisJobPhase.Running;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var orchestration = scope.ServiceProvider.GetRequiredService<IAnalysisOrchestrationService>();
            var result = await orchestration.RunAnalysisForSingleSubscriptionAsync(entry.SubscriptionId);

            lock (entry.Gate)
            {
                entry.Result = result;
                entry.Phase = AnalysisJobPhase.Completed;
                entry.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background analysis failed for job {JobId}", entry.JobId);
            SentrySdk.CaptureException(ex);
            lock (entry.Gate)
            {
                entry.Phase = AnalysisJobPhase.Failed;
                entry.HostError = ex.Message;
                entry.HostErrorDetails = ex.ToString();
                entry.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private void TrimIfNeeded()
    {
        if (_jobs.Count < MaxJobs)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - JobTtl;
        foreach (var kv in _jobs)
        {
            if (_jobs.Count < MaxJobs)
            {
                break;
            }

            var e = kv.Value;
            bool removable;
            lock (e.Gate)
            {
                removable = e.CompletedAt.HasValue && e.CompletedAt.Value < cutoff;
            }

            if (removable)
            {
                _jobs.TryRemove(kv.Key, out _);
            }
        }
    }

    private static AnalysisJobStatusDto ToDto(JobEntry entry)
    {
        var status = entry.Phase switch
        {
            AnalysisJobPhase.Pending => "pending",
            AnalysisJobPhase.Running => "running",
            AnalysisJobPhase.Completed => "completed",
            AnalysisJobPhase.Failed => "failed",
            _ => "unknown"
        };

        var dto = new AnalysisJobStatusDto
        {
            JobId = entry.JobId,
            SubscriptionId = entry.SubscriptionId,
            Status = status,
            CreatedAt = entry.CreatedAt,
            CompletedAt = entry.CompletedAt
        };

        if (entry.Phase == AnalysisJobPhase.Completed && entry.Result != null)
        {
            var r = entry.Result;
            return new AnalysisJobStatusDto
            {
                JobId = entry.JobId,
                SubscriptionId = entry.SubscriptionId,
                Status = status,
                CreatedAt = entry.CreatedAt,
                CompletedAt = entry.CompletedAt,
                Success = r.Success,
                SubscriptionName = r.SubscriptionName,
                AnalysisRunId = r.AnalysisRunId,
                TotalMonthlyCost = r.TotalMonthlyCost,
                ResourcesAnalyzed = r.ResourcesAnalyzed,
                Message = r.Message,
                ErrorDetails = r.ErrorDetails
            };
        }

        if (entry.Phase == AnalysisJobPhase.Failed)
        {
            return new AnalysisJobStatusDto
            {
                JobId = entry.JobId,
                SubscriptionId = entry.SubscriptionId,
                Status = status,
                CreatedAt = entry.CreatedAt,
                CompletedAt = entry.CompletedAt,
                Success = false,
                Message = entry.HostError,
                ErrorDetails = entry.HostErrorDetails
            };
        }

        return dto;
    }

    private enum AnalysisJobPhase
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    private sealed class JobEntry
    {
        public required Guid JobId { get; init; }
        public required string SubscriptionId { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public object Gate { get; } = new();
        public AnalysisJobPhase Phase { get; set; }
        public SubscriptionAnalysisResult? Result { get; set; }
        public string? HostError { get; set; }
        public string? HostErrorDetails { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}