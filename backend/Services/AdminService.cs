using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IDisputeRepository _disputeRepo;
        private readonly IAuditLogRepository _auditRepo;

        public AdminService(
            IUserRepository userRepo,
            IJobRepository jobRepo,
            IDisputeRepository disputeRepo,
            IAuditLogRepository auditRepo,
            IWalletRepository walletRepo)
        {
            _userRepo = userRepo;
            _jobRepo = jobRepo;
            _disputeRepo = disputeRepo;
            _auditRepo = auditRepo;
            _walletRepo = walletRepo;
        }

        private readonly IWalletRepository _walletRepo;

        public async Task<PagedResult<AdminUserResponse>> GetUsersAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var total = await _userRepo.GetCountAsync();
            var users = await _userRepo.GetPagedUsersAsync((page - 1) * pageSize, pageSize);

            var items = users.Select(u => new AdminUserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                TrustScore = u.TrustScore,
                VerificationLevel = u.VerificationLevel,
                IsNinVerified = u.IsNinVerified,
                AvatarUrl = u.AvatarUrl,
                Phone = u.Phone,
                Tier = u.Tier.ToString(),
                TotalVouchPoints = u.TotalVouchPoints,
                CreatedAt = u.CreatedAt
            }).ToList();

            return new PagedResult<AdminUserResponse>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task PromoteToAdminAsync(Guid superAdminId, Guid targetUserId)
        {
            await SetUserRoleAsync(targetUserId, "Admin", superAdminId);
        }

        public async Task<AdminUserResponse?> GetUserByIdAsync(Guid id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) return null;

            return new AdminUserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                TrustScore = u.TrustScore,
                VerificationLevel = u.VerificationLevel,
                IsNinVerified = u.IsNinVerified,
                AvatarUrl = u.AvatarUrl,
                Phone = u.Phone,
                Tier = u.Tier.ToString(),
                TotalVouchPoints = u.TotalVouchPoints,
                CreatedAt = u.CreatedAt
            };
        }

        public async Task SetUserRoleAsync(Guid targetUserId, string newRole, Guid actorId)
        {
            var actor = await _userRepo.GetByIdAsync(actorId)
                ?? throw new KeyNotFoundException("Actor not found.");

            if (actor.Role != UserRole.SuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmins can change user roles.");

            if (!Enum.TryParse<UserRole>(newRole, ignoreCase: true, out var parsedRole))
                throw new ArgumentException($"Invalid role '{newRole}'. Valid values: User, Admin, SuperAdmin.");

            var target = await _userRepo.GetByIdAsync(targetUserId)
                ?? throw new KeyNotFoundException($"User {targetUserId} not found.");

            if (target.Id == actorId && parsedRole != UserRole.SuperAdmin)
                throw new InvalidOperationException("SuperAdmin cannot demote themselves.");

            var oldRole = target.Role;
            target.Role = parsedRole;
            
            _userRepo.Update(target);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "User",
                ReferenceId = targetUserId,
                Action = "RoleChanged",
                ActorId = actorId,
                Details = $"Role changed from {oldRole} to {parsedRole}"
            });

            await _userRepo.SaveChangesAsync();
            await _auditRepo.SaveChangesAsync();
        }

        public async Task<List<AdminJobResponse>> GetAllJobsAsync()
        {
            var jobs = await _jobRepo.GetAllJobsDetailedAsync();
            return jobs.Select(j => new AdminJobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Amount = j.Amount,
                Status = j.Status.ToString(),
                CreatorId = j.CreatorId,
                CreatorName = j.Creator != null ? j.Creator.Name : "Unknown",
                CreatedAt = j.CreatedAt
            }).ToList();
        }

        public async Task<List<AdminDisputeResponse>> GetAllDisputesAsync()
        {
            var disputes = await _disputeRepo.GetAllDisputesDetailedAsync();
            return disputes.Select(d => new AdminDisputeResponse
            {
                Id = d.Id,
                JobId = d.JobId,
                JobTitle = d.Job != null ? d.Job.Title : "Unknown",
                RaisedById = d.RaisedById,
                RaisedByName = d.RaisedBy != null ? d.RaisedBy.Name : "Unknown",
                Reason = d.Reason,
                Status = d.ResolutionStage,
                AdminNotes = d.AdminNotes,
                CreatedAt = d.CreatedAt
            }).ToList();
        }

        public async Task ResolveDisputeAsync(Guid disputeId, string resolution, Guid actorId)
        {
            var dispute = await _disputeRepo.GetDisputeDetailedAsync(disputeId)
                ?? throw new KeyNotFoundException($"Dispute {disputeId} not found.");

            if (dispute.ResolutionStage == "Resolved")
                throw new InvalidOperationException("Dispute is already resolved.");

            dispute.ResolutionStage = "Resolved";
            dispute.AdminNotes = resolution;
            dispute.FinalDecision = resolution;
            dispute.ResolvedAt = DateTime.UtcNow;

            _disputeRepo.Update(dispute);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Dispute",
                ReferenceId = disputeId,
                Action = "DisputeResolved",
                ActorId = actorId,
                Details = $"Admin resolved dispute {disputeId}: {resolution}"
            });

            await _disputeRepo.SaveChangesAsync();
            await _auditRepo.SaveChangesAsync();
        }

        public async Task<AdminUserResponse> CreateAdminAsync(CreateAdminRequest request, Guid actorId)
        {
            var actor = await _userRepo.GetByIdAsync(actorId)
                ?? throw new KeyNotFoundException("Actor not found.");

            if (actor.Role != UserRole.SuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmins can create new administrators.");

            var exists = await _userRepo.ExistsByEmailAsync(request.Email.ToLowerInvariant());
            if (exists)
                throw new InvalidOperationException("A user with this email already exists.");

            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsedRole) || 
                (parsedRole != UserRole.Admin && parsedRole != UserRole.SuperAdmin))
            {
                throw new ArgumentException("Invalid role. Role must be 'Admin' or 'SuperAdmin'.");
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = parsedRole
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            // Admins also get a wallet (starts at 0)
            await _walletRepo.AddAsync(new Wallet { UserId = user.Id, Balance = 0m });
            await _walletRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "User",
                ReferenceId = user.Id,
                Action = "AdminCreated",
                ActorId = actorId,
                Details = $"New {parsedRole} account created for {user.Email} by SuperAdmin {actorId}."
            });
            await _auditRepo.SaveChangesAsync();

            return new AdminUserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                TrustScore = user.TrustScore,
                VerificationLevel = user.VerificationLevel,
                IsNinVerified = user.IsNinVerified,
                AvatarUrl = user.AvatarUrl,
                Phone = user.Phone,
                Tier = user.Tier.ToString(),
                TotalVouchPoints = user.TotalVouchPoints,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
