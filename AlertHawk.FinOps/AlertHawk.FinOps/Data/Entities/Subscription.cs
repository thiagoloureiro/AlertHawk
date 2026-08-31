using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinOpsToolSample.Models;

namespace FinOpsToolSample.Data.Entities
{
    [Table("Subscriptions")]
    public class Subscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubscriptionId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional monthly budget in USD. Null means no budget configured.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        /// <summary>
        /// Monthly infra support overlay (USD) used in historical cost charts. Default $400.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal InfraSupportCost { get; set; } = SubscriptionDefaults.InfraSupportCost;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
