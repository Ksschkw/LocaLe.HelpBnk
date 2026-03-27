using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IEscrowRepository : IRepository<Escrow>
    {
        Task<Escrow?> GetEscrowDetailedAsync(int id);
        Task<Escrow?> GetByBookingIdAsync(Guid bookingId);
    }
}
