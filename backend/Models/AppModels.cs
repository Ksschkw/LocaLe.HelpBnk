using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaLe.EscrowApi.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to User. One wallet per user.
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Current available balance. Uses decimal for financial precision.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Concurrency token — prevents double-spend race conditions.
        /// If two requests try to modify the same wallet simultaneously,
        /// the second one will fail with a concurrency exception.
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();
    }

    public enum EscrowStatus
    {
        Pending,
        Secured,
        Released,
        InDispute,
        Cancelled
    }

    public class Escrow
    {
        public int Id { get; set; }

        /// <summary>
        /// The booking this escrow is tied to.
        /// </summary>
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        /// <summary>
        /// Redundant buyer/provider IDs for fast audit lookups
        /// without joining through Booking → Job → Creator.
        /// </summary>
        public int BuyerId { get; set; }
        public int ProviderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public EscrowStatus Status { get; set; } = EscrowStatus.Pending;

        /// <summary>
        /// One-time QR verification token. 8-character uppercase string.
        /// </summary>
        [MaxLength(8)]
        public string? QrToken { get; set; }

        /// <summary>
        /// Concurrency token — prevents double-release race conditions.
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>
        /// Generic reference system: tracks what entity this log is about.
        /// e.g., "Escrow", "Job", "Wallet"
        /// </summary>
        [Required, MaxLength(50)]
        public string ReferenceType { get; set; } = string.Empty;

        public int ReferenceId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// The user ID who performed this action.
        /// </summary>
        public int ActorId { get; set; }

        [MaxLength(500)]
        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
