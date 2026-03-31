using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 50)
        {
            return await _dbSet
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetForUserAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.ActorId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();
        }
    }
}
