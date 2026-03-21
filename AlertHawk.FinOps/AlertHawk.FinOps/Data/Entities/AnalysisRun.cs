using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinOpsToolSample.Data.Entities
{
    [Table("AnalysisRuns")]
    public class AnalysisRun
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubscriptionId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string SubscriptionName { get; set; } = string.Empty;

        [Required]
        public DateTime RunDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalMonthlyCost { get; set; }

        public int TotalResourcesAnalyzed { get; set; }

        [MaxLength(50)]
        public string AiModel { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ConversationId { get; set; } = string.Empty;

        public string? ReportFilePath { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<CostDetail> CostDetails { get; set; } = new List<CostDetail>();
        public virtual ICollection<ResourceAnalysis> Resources { get; set; } = new List<ResourceAnalysis>();
        public virtual ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();
    }
}
