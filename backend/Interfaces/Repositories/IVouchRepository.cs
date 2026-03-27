using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IVouchRepository : IRepository<Vouch>
    {
        Task<int> GetTotalPointsForServiceAsync(Guid serviceId);
        Task<bool> HasUserVouchedForServiceAsync(Guid userId, Guid serviceId);
        Task<IEnumerable<Vouch>> GetVouchesForServiceAsync(Guid serviceId);
    }
}
