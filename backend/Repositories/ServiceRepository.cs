using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class ServiceRepository : Repository<Service>, IServiceRepository
    {
        public ServiceRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<Service?> GetServiceDetailedAsync(int id)
        {
            return await _dbSet
                .Include(s => s.Provider)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Service>> GetServicesByCategoryIdAsync(int categoryId)
        {
            return await _dbSet
                .Include(s => s.Provider)
                .Include(s => s.Category)
                .Where(s => s.CategoryId == categoryId && s.Status == "Active")
                .ToListAsync();
        }

        public async Task<IEnumerable<Service>> SearchServicesAsync(string? query, string? categoryName, decimal? lat = null, decimal? lng = null, decimal? radiusKm = null)
        {
            var sq = _dbSet
                .Include(s => s.Provider)
                .Include(s => s.Category)
                .Where(s => s.Status == "Active" && s.IsDiscoveryEnabled)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var lowerCat = categoryName.ToLower();
                sq = sq.Where(s => s.Category != null && s.Category.Name.ToLower().Contains(lowerCat));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowerQ = query.ToLower();
                sq = sq.Where(s => s.Title.ToLower().Contains(lowerQ) || s.Description.ToLower().Contains(lowerQ));
            }

            // Simple bounding box for proximity (approx 1 degree = 111km)
            if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            {
                var delta = radiusKm.Value / 111.0m;
                var minLat = lat.Value - delta;
                var maxLat = lat.Value + delta;
                var minLng = lng.Value - delta;
                var maxLng = lng.Value + delta;

                sq = sq.Where(s => s.Latitude >= minLat && s.Latitude <= maxLat &&
                                   s.Longitude >= minLng && s.Longitude <= maxLng);
            }

            return await sq.OrderByDescending(s => s.TrustPoints).ToListAsync();
        }
    }
}
