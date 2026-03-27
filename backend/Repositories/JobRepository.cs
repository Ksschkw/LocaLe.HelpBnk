using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class JobRepository : Repository<Job>, IJobRepository
    {
        public JobRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Job>> GetAllOpenJobsAsync()
        {
            return await _dbSet
                .Include(j => j.Creator)
                .Where(j => j.Status == JobStatus.Open)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<Job?> GetJobWithCreatorAsync(Guid id)
        {
            return await _dbSet
                .Include(j => j.Creator)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<IEnumerable<Job>> GetJobsByCreatorAsync(Guid creatorId)
        {
            return await _dbSet
                .Where(j => j.CreatorId == creatorId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> GetAllJobsDetailedAsync()
        {
            return await _dbSet
                .Include(j => j.Creator)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
    }
}
