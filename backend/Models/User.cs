using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    /// <summary>
    /// Role hierarchy: User (default) to Admin to SuperAdmin.
    /// SuperAdmin can promote/demote Admins. Admins can manage disputes and monitor the platform.
    /// </summary>
    public enum UserRole
    {
        User,
        Admin,
        SuperAdmin
    }

    public enum UserTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Role of this user. Default is User. Only SuperAdmin can promote to Admin/SuperAdmin.
        /// </summary>
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Community & Identity Fields
        public string? Phone { get; set; }
        public string? NIN { get; set; }
        public bool IsNinVerified { get; set; } = false;
        public string? AvatarUrl { get; set; }
        public int TrustScore { get; set; } = 0;
        public int VerificationLevel { get; set; } = 0;
        public UserTier Tier { get; set; } = UserTier.Bronze;
        public int TotalVouchPoints { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Bio { get; set; }
        public int JobsCompleted { get; set; } = 0;

        // Location data
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,6)")]
        public decimal? Latitude { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,6)")]
        public decimal? Longitude { get; set; }
        [MaxLength(100)]
        public string? AreaName { get; set; }


        /// <summary>
        /// Concurrency token to prevent race conditions on user updates.
        /// </summary>
        [ConcurrencyCheck]
        public Guid Version { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        public ICollection<Service> Services { get; set; } = new List<Service>();

        [JsonIgnore]
        public Wallet? Wallet { get; set; }
    }
}
