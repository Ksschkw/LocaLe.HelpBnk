using System.ComponentModel.DataAnnotations;

namespace LocaLe.EscrowApi.DTOs
{
    public class CreateCategoryRequest
    {
        [Required]
        [MinLength(3)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        
        public Guid? ParentId { get; set; }
    }
}
