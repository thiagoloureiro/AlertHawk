using FinOpsToolSample.Configuration;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Builds Azure Cost Management Query API request bodies.
    /// </summary>
    public static class CostManagementQueryBuilder
    {
        public static object BuildMonthToDateQuery(string costQueryType) =>
            new
            {
                type = AzureCostQueryType.Normalize(costQueryType),
                timeframe = "MonthToDate",
                dataset = DailyResourceGroupAndServiceDataset(),
            };

        public static object BuildCustomDailyQuery(string costQueryType, DateTime startDate, DateTime endDate) =>
            new
            {
                type = AzureCostQueryType.Normalize(costQueryType),
                timeframe = "Custom",
                timePeriod = new
                {
                    from = startDate.ToString("yyyy-MM-ddT00:00:00Z"),
                    to = endDate.ToString("yyyy-MM-ddT23:59:59Z"),
                },
                dataset = DailyResourceGroupAndServiceDataset(),
            };

        private static object DailyResourceGroupAndServiceDataset() =>
            new
            {
                granularity = "Daily",
                aggregation = new Dictionary<string, object>
                {
                    ["totalCost"] = new { name = "PreTaxCost", function = "Sum" },
                },
                grouping = new[]
                {
                    new { type = "Dimension", name = "ResourceGroupName" },
                    new { type = "Dimension", name = "ServiceName" },
                },
            };
    }
}
