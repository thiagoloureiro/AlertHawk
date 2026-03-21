using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FinOpsToolSample.Data.Entities
{
    [Table("AiRecommendations")]
    public class AiRecommendation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisRunId { get; set; }

        [Required]
        public string RecommendationText { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        [MaxLength(100)]
        public string MessageId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ConversationId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        public double Timestamp { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation property
        [ForeignKey("AnalysisRunId")]
        public virtual AnalysisRun AnalysisRun { get; set; } = null!;

        // Helper method to get formatted text
        public string GetFormattedText()
        {
            return RecommendationText
                .Replace("\\n", Environment.NewLine)
                .Replace("\\r\\n", Environment.NewLine);
        }

        // Helper method to get summary
        public string GetSummary()
        {
            if (!string.IsNullOrEmpty(Summary))
                return Summary;

            var lines = RecommendationText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? string.Join(" ", lines.Take(3)) : "";
        }
    }
}
