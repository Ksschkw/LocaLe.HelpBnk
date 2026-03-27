namespace LocaLe.EscrowApi.DTOs
{
    public class CategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public Guid? ParentId { get; set; }
        
        /// <summary>
        /// Populated for root categories to contain all subcategories in one tree.
        /// </summary>
        public List<CategoryResponse> SubCategories { get; set; } = new List<CategoryResponse>();
    }

    public class ServiceResponse
    {
        public Guid Id { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal HourlyRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TrustPoints { get; set; }
        public bool IsDiscoveryEnabled { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AreaName { get; set; }
    }

    public class CreateServiceRequest
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AreaName { get; set; }
    }

    public class UpdateServiceRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? HourlyRate { get; set; }
        public string? Status { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AreaName { get; set; }
    }
}
