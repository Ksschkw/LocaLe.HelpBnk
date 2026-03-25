using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Community & Identity Fields
        public string? Phone { get; set; }
        public string? NIN { get; set; }
        public bool IsNinVerified { get; set; } = false;
        public string? AvatarUrl { get; set; }
        public int TrustScore { get; set; } = 0;
        public int VerificationLevel { get; set; } = 0;

        /// <summary>
        /// Concurrency token to prevent race conditions on user updates.
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
