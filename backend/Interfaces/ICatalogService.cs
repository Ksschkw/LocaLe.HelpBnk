using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface ICatalogService
    {
        Task<List<CategoryResponse>> GetCategoriesAsync(int? parentId = null);
        Task<CategoryDetailResponse?> GetCategoryByIdAsync(int id);
        Task<List<ServiceResponse>> GetServicesByCategoryAsync(int categoryId);
        Task<ServiceResponse?> GetServiceByIdAsync(int id);
        Task<ServiceResponse> CreateServiceAsync(int providerId, CreateServiceRequest request);
    }
}
