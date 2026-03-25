using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Dispute
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tied to the Job/Booking that had the escrow disputed.
        /// </summary>
        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        [Required]
        public int RaisedById { get; set; }

        [ForeignKey("RaisedById")]
        [JsonIgnore]
        public User? RaisedBy { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string InitialComplaint { get; set; } = string.Empty;

        /// <summary>
        /// State of the dispute investigation (e.g. Open, UnderReview, Resolved)
        /// </summary>
        [MaxLength(50)]
        public string ResolutionStage { get; set; } = "Open";

        [MaxLength(2000)]
        public string? AdminNotes { get; set; }

        [MaxLength(500)]
        public string? FinalDecision { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
