using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetBookingDetailedAsync(Guid id);
        Task<IEnumerable<Booking>> GetByJobIdAsync(Guid jobId);
        Task<IEnumerable<Booking>> GetByProviderIdAsync(Guid providerId);
        Task<IEnumerable<Booking>> GetPendingApplicationsForJobAsync(Guid jobId);
        Task<IEnumerable<Booking>> GetUserBookingsAsync(Guid userId);
        Task<bool> HasUserAppliedAsync(Guid jobId, Guid providerId);
    }
}
