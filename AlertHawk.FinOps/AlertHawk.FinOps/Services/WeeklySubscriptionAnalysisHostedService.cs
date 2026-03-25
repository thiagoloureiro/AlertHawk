using FinOpsToolSample.Configuration;
using Microsoft.Extensions.Options;

namespace FinOpsToolSample.Services;

/// <summary>
/// Runs analysis for each configured subscription ID once per week at the configured UTC day/time.
/// </summary>
public sealed class WeeklySubscriptionAnalysisHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WeeklyAnalysisOptions> _weeklyOptions;
    private readonly IOptions<AzureConfiguration> _azureOptions;
    private readonly ILogger<WeeklySubscriptionAnalysisHostedService> _logger;

    public WeeklySubscriptionAnalysisHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<WeeklyAnalysisOptions> weeklyOptions,
        IOptions<AzureConfiguration> azureOptions,
        ILogger<WeeklySubscriptionAnalysisHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _weeklyOptions = weeklyOptions;
        _azureOptions = azureOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _weeklyOptions.Value;
        if (!opts.Enabled)
        {
            _logger.LogInformation("Weekly subscription analysis is disabled (WeeklyAnalysis:Enabled = false).");
            return;
        }

        ValidateSchedule(opts);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRunUtc(opts);
            var nextRun = DateTimeOffset.UtcNow.Add(delay);
            _logger.LogInformation(
                "Next weekly analysis scheduled at {NextRunUtc:O} UTC (in {Delay}).",
                nextRun,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunAllSubscriptionsAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunAllSubscriptionsAsync(CancellationToken stoppingToken)
    {
        var subscriptionIds = _azureOptions.Value.GetSubscriptionIdList();
        if (subscriptionIds.Count == 0)
        {
            _logger.LogWarning("Weekly analysis skipped: Azure:SubscriptionIds is empty.");
            return;
        }

        _logger.LogInformation(
            "Weekly analysis starting for {Count} subscription(s).",
            subscriptionIds.Count);

        foreach (var subscriptionId in subscriptionIds)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var orchestration = scope.ServiceProvider.GetRequiredService<IAnalysisOrchestrationService>();

            _logger.LogInformation("Weekly analysis running for subscription {SubscriptionId}.", subscriptionId);

            var result = await orchestration
                .RunAnalysisForSingleSubscriptionAsync(subscriptionId)
                .ConfigureAwait(false);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Weekly analysis finished for subscription {SubscriptionId} ({SubscriptionName}).",
                    result.SubscriptionId,
                    result.SubscriptionName);
            }
            else
            {
                _logger.LogWarning(
                    "Weekly analysis reported failure for subscription {SubscriptionId}: {Message}",
                    subscriptionId,
                    result.Message);
            }
        }

        _logger.LogInformation("Weekly analysis pass completed for all configured subscriptions.");
    }

    private static void ValidateSchedule(WeeklyAnalysisOptions opts)
    {
        if (opts.HourUtc is < 0 or > 23)
        {
            throw new OptionsValidationException(
                nameof(opts.HourUtc),
                typeof(WeeklyAnalysisOptions),
                ["WeeklyAnalysis:HourUtc must be between 0 and 23."]);
        }

        if (opts.MinuteUtc is < 0 or > 59)
        {
            throw new OptionsValidationException(
                nameof(opts.MinuteUtc),
                typeof(WeeklyAnalysisOptions),
                ["WeeklyAnalysis:MinuteUtc must be between 0 and 59."]);
        }
    }

    internal static TimeSpan ComputeDelayUntilNextRunUtc(WeeklyAnalysisOptions opts)
    {
        var now = DateTimeOffset.UtcNow;
        var todayUtc = now.UtcDateTime.Date;

        var daysToAdd = ((int)opts.DayOfWeekUtc - (int)now.DayOfWeek + 7) % 7;
        var nextDate = todayUtc.AddDays(daysToAdd);
        var nextRun = new DateTimeOffset(
            nextDate.Year,
            nextDate.Month,
            nextDate.Day,
            opts.HourUtc,
            opts.MinuteUtc,
            0,
            TimeSpan.Zero);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(7);
        }

        return nextRun - now;
    }
}
