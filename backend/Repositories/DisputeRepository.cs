using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class DisputeRepository : Repository<Dispute>, IDisputeRepository
    {
        public DisputeRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Dispute>> GetAllDisputesDetailedAsync()
        {
            return await _dbSet
                .Include(d => d.Job)
                .Include(d => d.RaisedBy)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dispute?> GetDisputeWithJobAsync(Guid id)
        {
            return await _dbSet
                .Include(d => d.Job)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
