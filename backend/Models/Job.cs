using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocaLe.EscrowApi.Models
{
    public enum JobStatus
    {
        Open,       // Posted, waiting for a provider to accept
        Assigned,   // A provider has been booked
        Completed,  // Job done, escrow released
        Cancelled   // Job cancelled before completion
    }

    public class Job
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The agreed-upon price for the job in the local currency (e.g., NGN).
        /// Uses decimal for financial precision.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// The user who posted this job (the Buyer/Hirer).
        /// </summary>
        public Guid CreatorId { get; set; }
        public User? Creator { get; set; }

        public JobStatus Status { get; set; } = JobStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional link back to a Service if this Job was created
        /// by a Buyer directly hiring a Provider from the catalog.
        /// </summary>
        public Guid? ServiceId { get; set; }
        public Service? Service { get; set; }
    }
}
