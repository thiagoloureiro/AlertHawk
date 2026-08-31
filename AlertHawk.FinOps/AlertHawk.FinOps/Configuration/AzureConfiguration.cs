namespace FinOpsToolSample.Configuration
{
    public class AzureConfiguration
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SubscriptionIds { get; set; } = string.Empty;

        /// <summary>
        /// Azure Cost Management query type: <see cref="AzureCostQueryType.ActualCost"/> (default, invoice/cash)
        /// or <see cref="AzureCostQueryType.AmortizedCost"/> (reservation/SP spread for FinOps trends).
        /// Config key: Azure:CostQueryType or Azure__CostQueryType.
        /// </summary>
        public string CostQueryType { get; set; } = AzureCostQueryType.Default;

        public string GetNormalizedCostQueryType() => AzureCostQueryType.Normalize(CostQueryType);

        public List<string> GetSubscriptionIdList()
        {
            return SubscriptionIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();
        }
    }
}
