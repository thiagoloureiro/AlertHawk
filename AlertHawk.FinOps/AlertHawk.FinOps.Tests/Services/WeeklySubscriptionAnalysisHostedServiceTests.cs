using FinOpsToolSample.Configuration;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class WeeklySubscriptionAnalysisHostedServiceTests
{
    [Fact]
    public void ComputeDelayUntilNextRunUtc_ReturnsPositiveDelayWithinOneWeek()
    {
        var opts = new WeeklyAnalysisOptions
        {
            DayOfWeekUtc = DayOfWeek.Monday,
            HourUtc = 4,
            MinuteUtc = 30
        };

        var delay = WeeklySubscriptionAnalysisHostedService.ComputeDelayUntilNextRunUtc(opts);

        Assert.True(delay > TimeSpan.Zero);
        Assert.True(delay <= TimeSpan.FromDays(8));
    }
}
