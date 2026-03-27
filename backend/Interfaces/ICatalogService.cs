using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface ICatalogService
    {
        Task<List<CategoryResponse>> GetCategoriesTreeAsync();
        Task<CategoryResponse?> GetCategoryByIdAsync(Guid id);
        Task<List<ServiceResponse>> GetServicesByCategoryAsync(Guid categoryId);
        Task<ServiceResponse?> GetServiceByIdAsync(Guid id);
        Task<List<ServiceResponse>> GetAllServicesAsync(string? search = null, string? categoryName = null, decimal? lat = null, decimal? lng = null, decimal? radiusKm = null);
        Task<ServiceResponse> CreateServiceAsync(Guid providerId, CreateServiceRequest request);
        Task<ServiceResponse> UpdateServiceAsync(Guid providerId, Guid serviceId, UpdateServiceRequest request);
        Task DeleteServiceAsync(Guid providerId, Guid serviceId);
    }
}
