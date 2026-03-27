using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class CategoryRepository : Repository<ServiceCategory>, ICategoryRepository
    {
        public CategoryRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ServiceCategory>> GetRootCategoriesTreeAsync()
        {
            return await _dbSet
                .Where(c => c.ParentId == null) // Root categories
                .Include(c => c.SubCategories)
                .ToListAsync();
        }

        public async Task<ServiceCategory?> GetCategoryWithSubsByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
