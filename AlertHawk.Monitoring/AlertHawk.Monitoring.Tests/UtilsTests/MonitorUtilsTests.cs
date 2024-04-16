using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Utils;

namespace AlertHawk.Monitoring.Tests.UtilsTests;

public class MonitorUtilsTests
{
    [Theory]
    [InlineData(MonitorRegion.Europe, "1")]
    [InlineData(MonitorRegion.Oceania, "2")]
    [InlineData(MonitorRegion.NorthAmerica, "3")]
    [InlineData(MonitorRegion.SouthAmerica, "4")]
    [InlineData(MonitorRegion.Africa, "5")]
    [InlineData(MonitorRegion.Asia, "6")]
    [InlineData(MonitorRegion.Custom, "7")]
    [InlineData(MonitorRegion.Custom2, "8")]
    public void Should_Return_MonitorRegion_Env_Variable(MonitorRegion monitorRegion, string monitorRegionString)
    {
        // Arrange
        Environment.SetEnvironmentVariable("monitor_region", monitorRegionString);
        
        // Act
        var monitorRegionVariable = MonitorUtils.GetMonitorRegionVariable();
        
        // Assert
        Assert.True(monitorRegion == monitorRegionVariable);
    }
}