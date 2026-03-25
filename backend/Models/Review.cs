using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        [Required]
        public int ReviewerId { get; set; }

        [ForeignKey("ReviewerId")]
        [JsonIgnore]
        public User? Reviewer { get; set; }

        [Required]
        public int RevieweeId { get; set; }

        [ForeignKey("RevieweeId")]
        [JsonIgnore]
        public User? Reviewee { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public bool IsVisible { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
