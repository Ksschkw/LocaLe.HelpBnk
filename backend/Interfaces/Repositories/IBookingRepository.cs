using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetWithDetailsAsync(Guid id);
        Task<IEnumerable<Booking>> GetByJobIdAsync(Guid jobId);
        Task<IEnumerable<Booking>> GetByProviderIdAsync(Guid providerId);
        Task<IEnumerable<Booking>> GetPendingApplicationsForJobAsync(int jobId);
    }
}
