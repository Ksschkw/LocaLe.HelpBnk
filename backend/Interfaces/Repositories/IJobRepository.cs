using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IJobRepository : IRepository<Job>
    {
        Task<IEnumerable<Job>> GetAllOpenJobsAsync();
        Task<Job?> GetJobWithCreatorAsync(Guid id);
        Task<IEnumerable<Job>> GetJobsByCreatorAsync(Guid creatorId);
        Task<IEnumerable<Job>> GetAllJobsDetailedAsync();
    }
}
