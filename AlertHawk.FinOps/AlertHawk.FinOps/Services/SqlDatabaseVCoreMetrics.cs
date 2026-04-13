namespace FinOpsToolSample.Services
{
    /// <summary>
    /// vCore-based Azure SQL databases do not expose <c>storage_percent</c> in Azure Monitor.
    /// Use <c>storage</c> (used bytes) and <c>allocated_data_storage</c> (quota bytes) instead.
    /// </summary>
    internal static class SqlDatabaseVCoreMetrics
    {
        internal const string StorageUsed = "storage";
        internal const string StorageAllocated = "allocated_data_storage";

        internal static string[] MetricsToQuery { get; } =
            { "cpu_percent", StorageUsed, StorageAllocated };

        /// <summary>
        /// Returns average and peak storage utilization as a percentage of allocated capacity.
        /// </summary>
        internal static (double AveragePercent, double MaxPercent)? TryComputeStorageUtilizationPercent(
            double storageUsedAvg,
            double storageUsedMax,
            double allocatedAvg,
            double allocatedMax)
        {
            var denominator = allocatedAvg > 0 ? allocatedAvg : allocatedMax;
            if (denominator <= 0)
            {
                return null;
            }

            var averagePercent = storageUsedAvg / denominator * 100.0;
            var maxPercent = storageUsedMax / denominator * 100.0;
            return (averagePercent, maxPercent);
        }
    }
}
