using AlertHawk.Metrics.Collectors;
using Xunit;

namespace AlertHawk.Metrics.Tests.Collectors;

public class HostMetricsCollectorTests
{
    [Fact]
    public async Task CollectAsync_ReturnsValidStructure()
    {
        // Act - runs on current OS (Windows/Linux); may return 0 for unsupported OS
        var (cpuPercent, memoryTotal, memoryUsed, disks) = await HostMetricsCollector.CollectAsync();

        // Assert - structure and value bounds
        Assert.InRange(cpuPercent, 0, 100);
        Assert.NotNull(disks);
        // Memory can be 0 on unsupported OS or in some environments
        Assert.True(memoryTotal >= 0);
        Assert.True(memoryUsed >= 0);
        Assert.True(memoryUsed <= memoryTotal || memoryTotal == 0);
    }

    [Fact]
    public async Task CollectAsync_DoesNotThrow()
    {
        // Act & Assert - smoke test that collection completes without exception
        var result = await HostMetricsCollector.CollectAsync();
        Assert.NotNull(result.Disks);
    }

    [Fact]
    public async Task CollectAsync_DisksListIsConsistent()
    {
        // Act
        var (_, _, _, disks) = await HostMetricsCollector.CollectAsync();

        // Assert
        Assert.NotNull(disks);
        foreach (var disk in disks)
        {
            Assert.False(string.IsNullOrEmpty(disk.DriveName));
            Assert.True(disk.FreeBytes <= disk.TotalBytes);
        }
    }
}
