using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<Booking?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Job)
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetByJobIdAsync(Guid jobId)
        {
            return await _dbSet
                .Where(b => b.JobId == jobId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByProviderIdAsync(Guid providerId)
        {
            return await _dbSet
                .Include(b => b.Job)
                .Where(b => b.ProviderId == providerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(Guid userId)
        {
            return await _dbSet
                .Include(b => b.Job)
                .Include(b => b.Provider)
                .Where(b => b.ProviderId == userId || (b.Job != null && b.Job.CreatorId == userId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserAppliedAsync(Guid jobId, Guid providerId)
        {
            return await _dbSet.AnyAsync(b => b.JobId == jobId && b.ProviderId == providerId);
        }

        public async Task<IEnumerable<Booking>> GetPendingApplicationsForJobAsync(int jobId)
        {
            return await _dbSet
                .Include(b => b.Provider)
                .Where(b => b.JobId == jobId && b.Status == BookingStatus.Pending)
                .ToListAsync();
        }
    }
}
