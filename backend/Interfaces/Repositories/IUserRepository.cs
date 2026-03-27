using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithDetailsAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<int> GetCountAsync();
        Task<IEnumerable<User>> GetPagedUsersAsync(int skip, int take);
    }
}
