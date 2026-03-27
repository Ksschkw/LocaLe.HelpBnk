using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class VouchRepository : Repository<Vouch>, IVouchRepository
    {
        public VouchRepository(EscrowContext context) : base(context)
        {
        }

        public async Task<int> GetTotalPointsForServiceAsync(int serviceId)
        {
            return await _dbSet
                .Where(v => v.ServiceId == serviceId && !v.IsRetracted)
                .SumAsync(v => v.PointsGiven);
        }

        public async Task<bool> HasUserVouchedForServiceAsync(int userId, int serviceId)
        {
            return await _dbSet.AnyAsync(v => v.VoucherId == userId && v.ServiceId == serviceId && !v.IsRetracted);
        }

        public async Task<IEnumerable<Vouch>> GetVouchesForServiceWithUserAsync(int serviceId)
        {
            return await _dbSet
                .Include(v => v.Voucher)
                .Where(v => v.ServiceId == serviceId && !v.IsRetracted)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
    }
}
