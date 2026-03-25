using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Vouch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        [JsonIgnore]
        public Service? Service { get; set; }

        [Required]
        public int VoucherId { get; set; }

        [ForeignKey("VoucherId")]
        [JsonIgnore]
        public User? Voucher { get; set; }

        public int PointsGiven { get; set; } = 1;

        [MaxLength(500)]
        public string? Comment { get; set; }

        public bool IsRetracted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
