namespace FinOpsToolSample.Models
{
    public class AnalysisRunWithDescriptionDto
    {
        public int Id { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime RunDate { get; set; }
        public decimal TotalMonthlyCost { get; set; }
        public int TotalResourcesAnalyzed { get; set; }
        public string AiModel { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string? ReportFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
