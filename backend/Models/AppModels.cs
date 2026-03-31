using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Foreign key to User. One wallet per user.
        /// </summary>
        [ForeignKey("User")]
        public Guid UserId { get; set; }
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
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The booking this escrow is tied to.
        /// </summary>
        public Guid BookingId { get; set; }
        public Booking? Booking { get; set; }

        /// <summary>
        /// Redundant buyer/provider IDs for fast audit lookups
        /// without joining through Booking → Job → Creator.
        /// </summary>
        public Guid BuyerId { get; set; }
        public User? Buyer { get; set; }
        public Guid ProviderId { get; set; }
        public User? Provider { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public EscrowStatus Status { get; set; } = EscrowStatus.Pending;

        /// <summary>
        /// What percentage of the total amount was deposited in the first phase.
        /// e.g. 0.5 for 50%.
        /// </summary>
        public decimal InitialDepositPercentage { get; set; } = 1.0m;

        public bool IsSecondPhaseFunded { get; set; } = false;

        public decimal SecondPhaseAmount => Amount * (1 - InitialDepositPercentage);

        /// <summary>
        /// One-time QR verification token. 8-character uppercase string.
        /// </summary>
        [MaxLength(8)]
        public string? QrToken { get; set; }

        public DateTime? QrTokenExpiry { get; set; }

        /// <summary>
        /// Concurrency token — prevents double-release race conditions.
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Generic reference system: tracks what entity this log is about.
        /// e.g., "Escrow", "Job", "Wallet"
        /// </summary>
        [Required, MaxLength(50)]
        public string ReferenceType { get; set; } = string.Empty;

        public Guid ReferenceId { get; set; }

        /// <summary>
        /// Optional JobId to link all events belonging to a single transaction flow.
        /// </summary>
        public Guid? JobId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// The user ID who performed this action.
        /// </summary>
        public Guid ActorId { get; set; }

        [MaxLength(500)]
        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum WaitlistStatus
    {
        Pending,
        Agreed,     // Terms met, ready for Job conversion
        Negotiating,
        Cancelled
    }

    public class Waitlist
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ServiceId { get; set; }
        public Service? Service { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public WaitlistStatus Status { get; set; } = WaitlistStatus.Pending;

        public string? PrivateNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class IdempotencyRecord
    {
        [Key]
        [MaxLength(255)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public string? ResponseBody { get; set; }

        [MaxLength(255)]
        public string RequestPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum NotificationType
    {
        NewMessage,
        BookingReceived,
        BookingAccepted,
        EscrowLocked,
        EscrowReleased,
        JobCompleted,
        DisputeRaised,
        DisputeResolved,
        WaitlistAgreed,
        DirectJobRequest
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }

        public NotificationType Type { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        /// <summary>Reference entity ID (JobId, BookingId, etc.) for deep-linking.</summary>
        public Guid? ReferenceId { get; set; }

        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

