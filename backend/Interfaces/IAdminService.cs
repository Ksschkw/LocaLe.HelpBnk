using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface IAdminService
    {
        Task PromoteToAdminAsync(Guid superAdminId, Guid targetUserId);

        /// <summary>Create a new admin user. Only SuperAdmin can call this.</summary>
        Task<AdminUserResponse> CreateAdminAsync(Guid superAdminId, CreateAdminRequest request);

        /// <summary>Get all users (paginated). Admins and SuperAdmins only.</summary>
        Task<PagedResult<AdminUserResponse>> GetUsersAsync(int page, int pageSize);

        /// <summary>Get a single user's full detail.</summary>
        Task<AdminUserResponse?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Change a user's role. Only a SuperAdmin can call this.
        /// Throws UnauthorizedAccessException if actor is not SuperAdmin.
        /// </summary>
        Task SetUserRoleAsync(int targetUserId, string newRole, int actorId);

        /// <summary>Get all jobs regardless of status (admin overview).</summary>
        Task<PagedResult<AdminJobResponse>> GetJobsAsync(int page, int pageSize);

        /// <summary>Get all disputes.</summary>
        Task<List<AdminDisputeResponse>> GetAllDisputesAsync();

        /// <summary>Resolve a dispute.</summary>
        Task ResolveDisputeAsync(int disputeId, string resolution, int actorId);

        /// <summary>Create a new admin user. Only SuperAdmin can call this.</summary>
        Task<AdminUserResponse> CreateAdminAsync(CreateAdminRequest request, int actorId);
    }
}
