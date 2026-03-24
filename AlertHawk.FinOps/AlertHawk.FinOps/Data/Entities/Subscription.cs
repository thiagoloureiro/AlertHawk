using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
