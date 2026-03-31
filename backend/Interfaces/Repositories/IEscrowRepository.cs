using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IEscrowRepository : IRepository<Escrow>
    {
        Task<Escrow?> GetEscrowDetailedAsync(Guid id);
        Task<Escrow?> GetByBookingIdAsync(Guid bookingId);
        Task<List<Escrow>> GetByProviderIdAsync(Guid providerId);
    }
}
