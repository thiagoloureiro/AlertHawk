namespace FinOpsToolSample.Configuration;

/// <summary>
/// Schedules <see cref="FinOpsToolSample.Services.IAnalysisOrchestrationService.RunAnalysisForSingleSubscriptionAsync"/>
/// for every subscription in <see cref="AzureConfiguration.SubscriptionIds"/> (weekly, UTC).
/// </summary>
public sealed class WeeklyAnalysisOptions
{
    public const string SectionName = "WeeklyAnalysis";

    /// <summary>When false, the background scheduler does nothing.</summary>
    public bool Enabled { get; set; }

    /// <summary>Calendar day (UTC) for the weekly run.</summary>
    public DayOfWeek DayOfWeekUtc { get; set; } = DayOfWeek.Sunday;

    /// <summary>0–23, UTC.</summary>
    public int HourUtc { get; set; } = 2;

    /// <summary>0–59, UTC.</summary>
    public int MinuteUtc { get; set; }
}
