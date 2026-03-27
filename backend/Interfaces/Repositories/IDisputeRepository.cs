using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IDisputeRepository : IRepository<Dispute>
    {
        Task<IEnumerable<Dispute>> GetAllDisputesDetailedAsync();
        Task<Dispute?> GetDisputeDetailedAsync(int id);
    }
}
