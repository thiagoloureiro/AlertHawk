using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class SqlDatabaseVCoreMetricsTests
{
    [Fact]
    public void MetricsToQuery_IncludesCpuAndStoragePair()
    {
        Assert.Contains("cpu_percent", SqlDatabaseVCoreMetrics.MetricsToQuery);
        Assert.Contains(SqlDatabaseVCoreMetrics.StorageUsed, SqlDatabaseVCoreMetrics.MetricsToQuery);
        Assert.Contains(SqlDatabaseVCoreMetrics.StorageAllocated, SqlDatabaseVCoreMetrics.MetricsToQuery);
    }

    [Fact]
    public void TryComputeStorageUtilizationPercent_WhenAllocatedZero_ReturnsNull()
    {
        var result = SqlDatabaseVCoreMetrics.TryComputeStorageUtilizationPercent(10, 20, 0, 0);
        Assert.Null(result);
    }

    [Fact]
    public void TryComputeStorageUtilizationPercent_UsesAllocatedAvgAsDenominator()
    {
        var result = SqlDatabaseVCoreMetrics.TryComputeStorageUtilizationPercent(
            storageUsedAvg: 50,
            storageUsedMax: 80,
            allocatedAvg: 100,
            allocatedMax: 200);

        Assert.NotNull(result);
        Assert.Equal(50, result.Value.AveragePercent);
        Assert.Equal(80, result.Value.MaxPercent);
    }

    [Fact]
    public void TryComputeStorageUtilizationPercent_FallsBackToAllocatedMax()
    {
        var result = SqlDatabaseVCoreMetrics.TryComputeStorageUtilizationPercent(
            storageUsedAvg: 25,
            storageUsedMax: 50,
            allocatedAvg: 0,
            allocatedMax: 100);

        Assert.NotNull(result);
        Assert.Equal(25, result.Value.AveragePercent);
        Assert.Equal(50, result.Value.MaxPercent);
    }
}
