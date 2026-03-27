using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface ICategoryRepository : IRepository<ServiceCategory>
    {
        /// <summary>
        /// Fetches the full root category tree including subcategories in a single query.
        /// No need for parentId parameters.
        /// </summary>
        Task<IEnumerable<ServiceCategory>> GetRootCategoriesTreeAsync();
        
        Task<ServiceCategory?> GetCategoryWithSubsByIdAsync(Guid id);
    }
}
