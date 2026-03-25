using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly EscrowContext _context;

        public CatalogService(EscrowContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryResponse>> GetCategoriesAsync(int? parentId = null)
        {
            return await _context.ServiceCategories
                .Where(c => c.ParentId == parentId)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IconUrl = c.IconUrl,
                    ParentId = c.ParentId
                })
                .ToListAsync();
        }

        public async Task<CategoryDetailResponse?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.ServiceCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return null;

            return new CategoryDetailResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IconUrl = category.IconUrl,
                ParentId = category.ParentId,
                SubCategories = category.SubCategories.Select(s => new CategoryResponse
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
            return await _context.Services
                .Include(s => s.Provider)
                .Include(s => s.Category)
                .Where(s => s.CategoryId == categoryId && s.Status == "Active")
                .Select(s => new ServiceResponse
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
                    TrustPoints = s.TrustPoints
                })
                .ToListAsync();
        }

        public async Task<ServiceResponse?> GetServiceByIdAsync(int id)
        {
            var s = await _context.Services
                .Include(s => s.Provider)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

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
                TrustPoints = s.TrustPoints
            };
        }

        public async Task<ServiceResponse> CreateServiceAsync(int providerId, CreateServiceRequest request)
        {
            var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
            {
                throw new ArgumentException("Category does not exist.");
            }

            var service = new Service
            {
                ProviderId = providerId,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Description = request.Description,
                BasePrice = request.BasePrice,
                HourlyRate = request.HourlyRate
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException("Failed to load created service.");
        }
    }
}
