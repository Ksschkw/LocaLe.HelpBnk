using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IServiceRepository : IRepository<Service>
    {
        Task<Service?> GetServiceDetailedAsync(Guid id);
        Task<IEnumerable<Service>> GetServicesByCategoryIdAsync(Guid categoryId);
        Task<IEnumerable<Service>> SearchServicesAsync(string? query, string? categoryName, decimal? lat = null, decimal? lng = null, decimal? radiusKm = null);
    }
}
