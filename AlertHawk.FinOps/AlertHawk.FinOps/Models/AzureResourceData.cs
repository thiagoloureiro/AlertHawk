using System.Collections.Generic;

namespace FinOpsToolSample.Models
{
    public class AzureResourceData
    {
        public string SubscriptionName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public decimal TotalMonthlyCost { get; set; }
        public Dictionary<string, decimal> CostsByResourceGroup { get; set; } = new();
        public List<ServiceCostDetail> CostsByService { get; set; } = new();
        public List<ResourceInfo> Resources { get; set; } = new();
    }

    public class ServiceCostDetail
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ResourceGroup { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }

    public class ResourceInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ResourceGroup { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, double> Metrics { get; set; } = new();
        public List<string> Flags { get; set; } = new();
    }
}
