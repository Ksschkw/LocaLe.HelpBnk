using System.ComponentModel.DataAnnotations;

namespace LocaLe.EscrowApi.Models
{
    public enum BookingStatus
    {
        Pending,    // Provider applied, buyer hasn't confirmed
        Active,     // Buyer confirmed, escrow is secured
        Finalized,  // Job done, escrow released
        Cancelled   // Booking was cancelled
    }

    public class Booking
    {
        public int Id { get; set; }

        /// <summary>
        /// The job this booking is for.
        /// </summary>
        public int JobId { get; set; }
        public Job? Job { get; set; }

        /// <summary>
        /// The user who accepted the job (the Provider).
        /// </summary>
        public int ProviderId { get; set; }
        public User? Provider { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
