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
                    .ThenInclude(s => s.Services) // Load nested services
                .Include(c => c.Services) // Load direct services
                .ToListAsync();
        }

        public async Task<ServiceCategory?> GetCategoryWithSubsByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.SubCategories)
                    .ThenInclude(s => s.Services)
                .Include(c => c.Services)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
