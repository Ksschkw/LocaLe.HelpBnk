using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LocaLe.EscrowApi.Services
{
    public class ServicesService : IServicesService
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IServiceRepository _serviceRepo;
        private readonly IMemoryCache _cache;

        public ServicesService(ICategoryRepository categoryRepo, IServiceRepository serviceRepo, IMemoryCache cache)
        {
            _categoryRepo = categoryRepo;
            _serviceRepo = serviceRepo;
            _cache = cache;
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
                ServiceCount = c.Services.Select(s => s.ProviderId)
                                .Concat(c.SubCategories.SelectMany(sc => sc.Services.Select(s => s.ProviderId)))
                                .Distinct().Count(),
                SubCategories = c.SubCategories.Select(s => new CategoryResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IconUrl = s.IconUrl,
                    ParentId = s.ParentId,
                    ServiceCount = s.Services.Select(sx => sx.ProviderId).Distinct().Count()
                }).ToList()
            }).ToList();
        }

        public async Task<CategoryResponse?> GetCategoryByIdAsync(Guid id)
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
                ServiceCount = c.Services.Select(s => s.ProviderId)
                                .Concat(c.SubCategories.SelectMany(sc => sc.Services.Select(s => s.ProviderId)))
                                .Distinct().Count(),
                SubCategories = c.SubCategories.Select(s => new CategoryResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IconUrl = s.IconUrl,
                    ParentId = s.ParentId,
                    ServiceCount = s.Services.Select(sx => sx.ProviderId).Distinct().Count()
                }).ToList()
            };
        }

        public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var category = new ServiceCategory
            {
                Name = request.Name,
                Description = request.Description,
                IconUrl = request.IconUrl,
                ParentId = request.ParentId,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepo.AddAsync(category);
            await _categoryRepo.SaveChangesAsync();

            _cache.Remove("AllCategories");

            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IconUrl = category.IconUrl,
                ParentId = category.ParentId
            };
        }

        public async Task<List<ServiceResponse>> GetAllActiveServicesAsync()
        {
            var services = await _serviceRepo.GetAllActiveServicesDetailedAsync();
            return MapServices(services);
        }

        public async Task<List<ServiceResponse>> GetServicesByCategoryAsync(Guid categoryId)
        {
            var services = await _serviceRepo.GetServicesByCategoryIdAsync(categoryId);
            return MapServices(services);
        }

        public async Task<ServiceResponse?> GetServiceByIdAsync(Guid id)
        {
            var s = await _serviceRepo.GetServiceDetailedAsync(id);
            if (s == null) return null;

            return new ServiceResponse
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                ProviderName = s.Provider?.Name ?? "Unknown",
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name ?? "Unknown",
                Title = s.Title,
                Description = s.Description,
                BasePrice = s.BasePrice,
                HourlyRate = s.HourlyRate,
                Status = s.Status,
                TrustPoints = s.TrustPoints,
                IsDiscoveryEnabled = s.IsDiscoveryEnabled,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                AreaName = s.AreaName,
                IsRemote = s.IsRemote
            };
        }

        public async Task<ServiceResponse> CreateServiceAsync(Guid providerId, CreateServiceRequest request)
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
                IsRemote = request.IsRemote,
                RequiredVouchPoints = 50,
                IsDiscoveryEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _serviceRepo.AddAsync(service);
            await _serviceRepo.SaveChangesAsync();

            _cache.Remove("AllActiveServices");

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load created service.");
        }

        public async Task<ServiceResponse> UpdateServiceAsync(Guid providerId, Guid serviceId, UpdateServiceRequest request)
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
            if (request.IsRemote.HasValue) service.IsRemote = request.IsRemote.Value;
            
            service.UpdatedAt = DateTime.UtcNow;

            _serviceRepo.Update(service);
            await _serviceRepo.SaveChangesAsync();

            _cache.Remove("AllActiveServices");

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load updated service.");
        }

        public async Task DeleteServiceAsync(Guid providerId, Guid serviceId)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new KeyNotFoundException("Service not found.");
            
            if (service.ProviderId != providerId)
                throw new UnauthorizedAccessException("You can only delete your own services.");

            _serviceRepo.Remove(service);
            await _serviceRepo.SaveChangesAsync();

            _cache.Remove("AllActiveServices");
        }

        public async Task<List<ServiceResponse>> GetServicesByProviderAsync(Guid providerId)
        {
            var services = await _serviceRepo.GetServicesByProviderIdAsync(providerId);
            return MapServices(services);
        }

        public async Task<ServiceResponse> ActivateServiceAsync(Guid providerId, Guid serviceId)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new KeyNotFoundException("Service not found.");
            
            if (service.ProviderId != providerId)
                throw new UnauthorizedAccessException("You can only activate your own services.");

            service.Status = "Active";
            service.IsDiscoveryEnabled = true;
            service.UpdatedAt = DateTime.UtcNow;

            _serviceRepo.Update(service);
            await _serviceRepo.SaveChangesAsync();

            _cache.Remove("AllActiveServices");

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load updated service.");
        }

        private List<ServiceResponse> MapServices(IEnumerable<Service> services)
        {
            return services.Select(s => new ServiceResponse
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                ProviderName = s.Provider?.Name ?? "Unknown",
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name ?? "Unknown",
                Title = s.Title,
                Description = s.Description,
                BasePrice = s.BasePrice,
                HourlyRate = s.HourlyRate,
                Status = s.Status,
                TrustPoints = s.TrustPoints,
                IsDiscoveryEnabled = s.IsDiscoveryEnabled,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                AreaName = s.AreaName,
                IsRemote = s.IsRemote
            }).ToList();
        }
    }
}
