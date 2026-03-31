using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class EscrowRepository : Repository<Escrow>, IEscrowRepository
    {
        public EscrowRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<Escrow?> GetEscrowDetailedAsync(Guid id)
        {
            return await _dbSet
                .Include(e => e.Booking!)
                    .ThenInclude(b => b.Job!)
                .Include(e => e.Buyer)
                .Include(e => e.Provider)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Escrow?> GetByBookingIdAsync(Guid bookingId)
        {
            return await _dbSet
                .Include(e => e.Booking!)
                    .ThenInclude(b => b.Job!)
                .Include(e => e.Buyer)
                .Include(e => e.Provider)
                .FirstOrDefaultAsync(e => e.BookingId == bookingId);
        }

        public async Task<List<Escrow>> GetByProviderIdAsync(Guid providerId)
        {
            return await _dbSet
                .Where(e => e.ProviderId == providerId)
                .ToListAsync();
        }
    }
}
