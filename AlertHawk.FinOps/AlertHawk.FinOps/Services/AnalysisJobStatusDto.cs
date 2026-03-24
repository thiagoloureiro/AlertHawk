namespace FinOpsToolSample.Services;

/// <summary>
/// Serializable job status for polling. <see cref="Status"/> is lowercase for stable JSON clients.
/// </summary>
public sealed class AnalysisJobStatusDto
{
    public Guid JobId { get; init; }
    public string SubscriptionId { get; init; } = string.Empty;

    /// <summary>pending, running, completed, or failed</summary>
    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Set when status is completed (whether analysis succeeded or not).</summary>
    public bool? Success { get; init; }

    public string? SubscriptionName { get; init; }
    public int? AnalysisRunId { get; init; }
    public decimal? TotalMonthlyCost { get; init; }
    public int? ResourcesAnalyzed { get; init; }
    public string? Message { get; init; }
    public string? ErrorDetails { get; init; }
}
