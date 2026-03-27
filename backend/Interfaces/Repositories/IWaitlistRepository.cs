using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IWaitlistRepository : IRepository<Waitlist>
    {
        Task<IEnumerable<Waitlist>> GetByServiceIdAsync(int serviceId);
        Task<IEnumerable<Waitlist>> GetByUserIdAsync(Guid userId);
        Task<Waitlist?> GetWithDetailsAsync(Guid id);
    }
}
