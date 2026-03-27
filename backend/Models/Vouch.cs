using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Vouch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        [JsonIgnore]
        public Service? Service { get; set; }

        [Required]
        public Guid VoucherId { get; set; }

        [ForeignKey("VoucherId")]
        [JsonIgnore]
        public User? Voucher { get; set; }

        public bool IsPlatformUser { get; set; } = false;

        public int PointsGiven { get; set; } = 1;

        [MaxLength(500)]
        public string? Comment { get; set; }

        public bool IsRetracted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
