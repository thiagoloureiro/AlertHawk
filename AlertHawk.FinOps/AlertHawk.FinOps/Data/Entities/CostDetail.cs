using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinOpsToolSample.Data.Entities
{
    [Table("CostDetails")]
    public class CostDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisRunId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CostType { get; set; } = string.Empty; // "ResourceGroup" or "Service"

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ResourceGroup { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation property
        [ForeignKey("AnalysisRunId")]
        public virtual AnalysisRun AnalysisRun { get; set; } = null!;
    }
}
