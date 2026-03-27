using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserWithWalletAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<int> GetCountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<IEnumerable<User>> GetPagedUsersAsync(int skip, int take)
        {
            return await _dbSet
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
