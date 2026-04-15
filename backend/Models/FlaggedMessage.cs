using System.ComponentModel.DataAnnotations;

namespace LocaLe.EscrowApi.Models
{
    /// <summary>
    /// Audit record created whenever a chat message is blocked for
    /// containing off-platform contact / payment information.
    /// Visible to SuperAdmins for manual review and potential bans.
    /// </summary>
    public class FlaggedMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The user who sent the blocked message.</summary>
        public Guid OffenderId { get; set; }
        public string OffenderName { get; set; } = string.Empty;

        /// <summary>The job or booking room context where it happened.</summary>
        public Guid? JobId { get; set; }
        public Guid? BookingId { get; set; }

        /// <summary>Raw content of the blocked message (stored for admin review).</summary>
        [MaxLength(4000)]
        public string BlockedContent { get; set; } = string.Empty;

        /// <summary>Which regex label triggered the block (e.g. "phone number").</summary>
        [MaxLength(100)]
        public string ViolationType { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>Whether an admin has reviewed and resolved this flag.</summary>
        public bool IsResolved { get; set; } = false;
        public string? AdminNote { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
