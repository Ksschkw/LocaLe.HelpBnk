using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface IServicesService
    {
        Task<List<CategoryResponse>> GetCategoriesTreeAsync();
        Task<CategoryResponse?> GetCategoryByIdAsync(Guid id);
        Task<List<ServiceResponse>> GetServicesByCategoryAsync(Guid categoryId);
        Task<List<ServiceResponse>> GetAllActiveServicesAsync();
        Task<ServiceResponse?> GetServiceByIdAsync(Guid id);
        Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);
        Task<ServiceResponse> CreateServiceAsync(Guid providerId, CreateServiceRequest request);
        Task<ServiceResponse> UpdateServiceAsync(Guid providerId, Guid serviceId, UpdateServiceRequest request);
        Task DeleteServiceAsync(Guid providerId, Guid serviceId);
        Task<List<ServiceResponse>> GetServicesByProviderAsync(Guid providerId);
        Task<ServiceResponse> ActivateServiceAsync(Guid providerId, Guid serviceId);
    }
}
