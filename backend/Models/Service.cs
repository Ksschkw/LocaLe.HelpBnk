using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Service
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        [JsonIgnore]
        public User? Provider { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [JsonIgnore]
        public ServiceCategory? Category { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal BasePrice { get; set; } = 0;

        [Range(0, 10000000)]
        public decimal HourlyRate { get; set; } = 0;

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Paused, Unlisted

        public int TrustPoints { get; set; } = 0;

        // Proximity & Discovery
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Longitude { get; set; }
        [MaxLength(100)]
        public string? AreaName { get; set; }

        public bool IsRemote { get; set; } = false;


        public int RequiredVouchPoints { get; set; } = 50; // Threshold to enable
        public bool IsDiscoveryEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Concurrency token to prevent dirty reads/writes on Service updates
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();
    }
}
