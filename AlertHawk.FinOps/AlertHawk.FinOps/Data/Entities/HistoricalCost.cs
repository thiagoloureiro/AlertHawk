using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinOpsToolSample.Data.Entities
{
    [Table("HistoricalCosts")]
    public class HistoricalCost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisRunId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubscriptionId { get; set; } = string.Empty;

        [Required]
        public DateTime CostDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string CostType { get; set; } = string.Empty; // "Total", "ResourceGroup", "Service"

        [MaxLength(255)]
        public string? ResourceGroup { get; set; } // Always populated with resource group name

        [MaxLength(255)]
        public string? Name { get; set; } // Service name (for CostType="Service"), NULL otherwise

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [MaxLength(20)]
        public string Currency { get; set; } = "USD";

        public DateTime RecordedAt { get; set; }

        // Navigation property
        [ForeignKey("AnalysisRunId")]
        public virtual AnalysisRun AnalysisRun { get; set; } = null!;
    }
}
