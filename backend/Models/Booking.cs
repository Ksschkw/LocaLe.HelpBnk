using System.ComponentModel.DataAnnotations;

namespace LocaLe.EscrowApi.Models
{
    public enum BookingStatus
    {
        Pending,    // Provider applied, waiting for buyer
        Active,     // Buyer confirmed ("Accept & Lock Vault"), escrow is secured
        Finalized,  // Job done, escrow released
        Cancelled   // Booking was cancelled or applicant was rejected
    }

    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The job this booking is for.
        /// </summary>
        public Guid JobId { get; set; }
        public Job? Job { get; set; }

        /// <summary>
        /// The user who accepted the job (the Provider).
        /// </summary>
        public Guid ProviderId { get; set; }
        public User? Provider { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        /// <summary>
        /// Provider's cover letter / pitch note when they applied.
        /// This kick-starts the pre-hire interview thread.
        /// </summary>
        [System.ComponentModel.DataAnnotations.MaxLength(2000)]
        public string? PitchNote { get; set; }

        /// <summary>
        /// True while this booking is still in the "interview" phase.
        /// Neither party can trigger escrow until the buyer clicks "Accept & Lock Vault".
        /// </summary>
        public bool IsPreHire { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
