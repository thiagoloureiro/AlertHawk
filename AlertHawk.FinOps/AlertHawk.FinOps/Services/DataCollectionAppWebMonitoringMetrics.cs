using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services;

/// <summary>
/// Maps Azure Monitor metric aggregates onto <see cref="ResourceInfo"/> for App Services (unit-tested).
/// </summary>
internal static class DataCollectionAppWebMonitoringMetrics
{
    internal static void ApplyMetric(ResourceInfo resource, string metricName, double avgValue, double totalValue)
    {
        switch (metricName)
        {
            case "Requests":
                resource.Metrics["Requests_Total"] = totalValue;
                break;
            case "MemoryWorkingSet":
                resource.Metrics["Memory_Average_MB"] = avgValue / 1024 / 1024;
                break;
            case "Http5xx":
                resource.Metrics["Http5xx_Errors_Total"] = totalValue;
                if (totalValue > 0)
                {
                    resource.Flags.Add($"HTTP 5xx errors detected: {totalValue:F0} errors in last 7 days");
                }

                break;
            case "AverageResponseTime":
                resource.Metrics["Response_Time_Average_Seconds"] = avgValue;
                if (avgValue > 3)
                {
                    resource.Flags.Add($"Slow response time: {avgValue:F2}s average");
                }

                break;
        }
    }
}
