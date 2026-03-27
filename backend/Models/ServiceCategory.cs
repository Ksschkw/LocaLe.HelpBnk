using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class ServiceCategory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? IconUrl { get; set; }

        public Guid? ParentId { get; set; }

        [ForeignKey("ParentId")]
        [JsonIgnore]
        public ServiceCategory? Parent { get; set; }

        [JsonIgnore]
        public ICollection<ServiceCategory> SubCategories { get; set; } = new List<ServiceCategory>();

        [JsonIgnore]
        public ICollection<Service> Services { get; set; } = new List<Service>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
