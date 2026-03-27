using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IWalletRepository : IRepository<Wallet>
    {
        Task<Wallet?> GetByUserIdAsync(Guid userId);
    }
}
