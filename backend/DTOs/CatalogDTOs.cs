namespace LocaLe.EscrowApi.DTOs
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? ParentId { get; set; }
    }

    public class CategoryDetailResponse : CategoryResponse
    {
        public List<CategoryResponse> SubCategories { get; set; } = new List<CategoryResponse>();
    }

    public class ServiceResponse
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal HourlyRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TrustPoints { get; set; }
    }

    public class CreateServiceRequest
    {
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal HourlyRate { get; set; }
    }
}
