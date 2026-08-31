using FinOpsToolSample.Configuration;

namespace FinOpsToolSample.Models
{
    public class FinOpsAnalysisSettingsDto
    {
        public string CostQueryType { get; set; } = AzureCostQueryType.Default;

        public string CostQueryTypeLabel { get; set; } = AzureCostQueryType.DisplayLabel(AzureCostQueryType.Default);

        public string CostQueryTypeDescription { get; set; } =
            AzureCostQueryType.Description(AzureCostQueryType.Default);
    }
}
