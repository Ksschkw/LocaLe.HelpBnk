using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 50);
    }
}
