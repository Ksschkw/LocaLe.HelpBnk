using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IServiceRepository _serviceRepo;

        public CatalogService(ICategoryRepository categoryRepo, IServiceRepository serviceRepo)
        {
            _categoryRepo = categoryRepo;
            _serviceRepo = serviceRepo;
        }

        public async Task<List<CategoryResponse>> GetCategoriesTreeAsync()
        {
            var roots = await _categoryRepo.GetRootCategoriesTreeAsync();
            
            return roots.Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconUrl = c.IconUrl,
                ParentId = c.ParentId,
                SubCategories = c.SubCategories.Select(s => new CategoryResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IconUrl = s.IconUrl,
                    ParentId = s.ParentId
                }).ToList()
            }).ToList();
        }

        public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
        {
            var c = await _categoryRepo.GetCategoryWithSubsByIdAsync(id);
            if (c == null) return null;

            return new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconUrl = c.IconUrl,
                ParentId = c.ParentId,
                SubCategories = c.SubCategories.Select(s => new CategoryResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IconUrl = s.IconUrl,
                    ParentId = s.ParentId
                }).ToList()
            };
        }

        public async Task<List<ServiceResponse>> GetServicesByCategoryAsync(int categoryId)
        {
            var services = await _serviceRepo.GetServicesByCategoryIdAsync(categoryId);
            return MapServices(services);
        }

        public async Task<ServiceResponse?> GetServiceByIdAsync(int id)
        {
            var s = await _serviceRepo.GetServiceDetailedAsync(id);
            if (s == null) return null;

            return new ServiceResponse
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                ProviderName = s.Provider != null ? s.Provider.Name : "Unknown",
                CategoryId = s.CategoryId,
                CategoryName = s.Category != null ? s.Category.Name : "Unknown",
                Title = s.Title,
                Description = s.Description,
                BasePrice = s.BasePrice,
                HourlyRate = s.HourlyRate,
                Status = s.Status,
                TrustPoints = s.TrustPoints,
                IsDiscoveryEnabled = s.IsDiscoveryEnabled,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                AreaName = s.AreaName
            };
        }

        public async Task<List<ServiceResponse>> GetAllServicesAsync(string? search = null, string? categoryName = null, decimal? lat = null, decimal? lng = null, decimal? radiusKm = null)
        {
            var services = await _serviceRepo.SearchServicesAsync(search, categoryName, lat, lng, radiusKm);
            return MapServices(services);
        }

        public async Task<ServiceResponse> CreateServiceAsync(int providerId, CreateServiceRequest request)
        {
            var categoryExists = await _categoryRepo.GetByIdAsync(request.CategoryId) != null;
            if (!categoryExists)
                throw new ArgumentException("Category does not exist.");

            var service = new Service
            {
                ProviderId = providerId,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Description = request.Description,
                BasePrice = request.BasePrice,
                HourlyRate = request.HourlyRate,
                Status = "Active",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AreaName = request.AreaName,
                RequiredVouchPoints = 50, // Default threshold
                IsDiscoveryEnabled = false, // Start hidden until vouched
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _serviceRepo.AddAsync(service);
            await _serviceRepo.SaveChangesAsync();

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load created service.");
        }

        public async Task<ServiceResponse> UpdateServiceAsync(int providerId, int serviceId, UpdateServiceRequest request)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new KeyNotFoundException("Service not found.");
            
            if (service.ProviderId != providerId)
                throw new UnauthorizedAccessException("You can only update your own services.");

            if (request.Title != null) service.Title = request.Title;
            if (request.Description != null) service.Description = request.Description;
            if (request.BasePrice.HasValue) service.BasePrice = request.BasePrice.Value;
            if (request.HourlyRate.HasValue) service.HourlyRate = request.HourlyRate.Value;
            if (request.Status != null) service.Status = request.Status;
            if (request.Latitude.HasValue) service.Latitude = request.Latitude;
            if (request.Longitude.HasValue) service.Longitude = request.Longitude;
            if (request.AreaName != null) service.AreaName = request.AreaName;
            
            service.UpdatedAt = DateTime.UtcNow;

            _serviceRepo.Update(service);
            await _serviceRepo.SaveChangesAsync();

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load updated service.");
        }

        public async Task DeleteServiceAsync(int providerId, int serviceId)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new KeyNotFoundException("Service not found.");
            
            if (service.ProviderId != providerId)
                throw new UnauthorizedAccessException("You can only delete your own services.");

            // Instead of hard delete, we could mark as Archived, but user requested full CRUD.
            // Let's physically remove it:
            _serviceRepo.Remove(service);
            await _serviceRepo.SaveChangesAsync();
        }

        private List<ServiceResponse> MapServices(IEnumerable<Service> services)
        {
            return services.Select(s => new ServiceResponse
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                ProviderName = s.Provider != null ? s.Provider.Name : "Unknown",
                CategoryId = s.CategoryId,
                CategoryName = s.Category != null ? s.Category.Name : "Unknown",
                Title = s.Title,
                Description = s.Description,
                BasePrice = s.BasePrice,
                HourlyRate = s.HourlyRate,
                Status = s.Status,
                TrustPoints = s.TrustPoints,
                IsDiscoveryEnabled = s.IsDiscoveryEnabled,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                AreaName = s.AreaName
            }).ToList();
        }
    }
}
