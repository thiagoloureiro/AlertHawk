namespace FinOpsToolSample.Configuration
{
    public class AzureConfiguration
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SubscriptionIds { get; set; } = string.Empty;

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
