using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class WaitlistRepository : Repository<Waitlist>, IWaitlistRepository
    {
        public WaitlistRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Waitlist>> GetByServiceIdAsync(Guid serviceId)
        {
            return await _dbSet
                .Include(w => w.User)
                .Where(w => w.ServiceId == serviceId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(w => w.Service)
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }

        public async Task<Waitlist?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(w => w.Service)
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
    }
}
