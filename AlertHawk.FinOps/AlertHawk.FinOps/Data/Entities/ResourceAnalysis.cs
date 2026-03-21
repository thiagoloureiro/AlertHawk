using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinOpsToolSample.Data.Entities
{
    [Table("ResourceAnalysis")]
    public class ResourceAnalysis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisRunId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ResourceType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string ResourceName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string ResourceGroup { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Location { get; set; } = string.Empty;

        public string? PropertiesJson { get; set; }

        public string? MetricsJson { get; set; }

        public string? Flags { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation property
        [ForeignKey("AnalysisRunId")]
        public virtual AnalysisRun AnalysisRun { get; set; } = null!;
    }
}
