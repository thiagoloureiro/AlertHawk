namespace FinOpsToolSample.Models
{
    public class AnalysisRunWithDescriptionDto
    {
        public int Id { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>Optional monthly budget (USD). Null when not set.</summary>
        public decimal? Budget { get; set; }
        /// <summary>Monthly infra support overlay (USD) for charts; default 400 when no subscription row.</summary>
        public decimal InfraSupportCost { get; set; } = SubscriptionDefaults.InfraSupportCost;
        public DateTime RunDate { get; set; }
        public decimal TotalMonthlyCost { get; set; }
        public int TotalResourcesAnalyzed { get; set; }
        public string AiModel { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string? ReportFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
