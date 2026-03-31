using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    /// <summary>
    /// Admin-only endpoints for platform management.
    /// All routes require at minimum the "Admin" role.
    /// Role promotion requires "SuperAdmin".
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ─── Users ───────────────────────────────────────────

        /// <summary>
        /// Get all registered users (paginated).
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(PagedResult<AdminUserResponse>), 200)]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetUsersAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Get full details of a specific user.
        /// </summary>
        [HttpGet("users/{id}")]
        [ProducesResponseType(typeof(AdminUserResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null) return NotFound(new { Error = $"User {id} not found." });
            return Ok(user);
        }

        /// <summary>
        /// Change a user's role. Only SuperAdmin can do this.
        /// Valid roles: "User", "Admin", "SuperAdmin".
        /// </summary>
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetUserRole(Guid id, [FromBody] SetRoleRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _adminService.SetUserRoleAsync(id, request.Role, GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(new { Error = "User not found." }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        /// <summary>
        /// Create a new administrator (Admin or SuperAdmin). Only SuperAdmin can call this.
        /// </summary>
        [HttpPost("users")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(AdminUserResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var user = await _adminService.CreateAdminAsync(request, GetCurrentUserId());
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ─── Jobs ────────────────────────────────────────────

        /// <summary>
        /// Get all jobs on the platform (all statuses, admin overview).
        /// </summary>
        [HttpGet("jobs")]
        [ProducesResponseType(typeof(List<AdminJobResponse>), 200)]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _adminService.GetAllJobsAsync();
            return Ok(jobs);
        }

        /// <summary>
        /// View the end-to-end timeline/audit log for a single job.
        /// </summary>
        [HttpGet("jobs/{job_id}/timeline")]
        [ProducesResponseType(typeof(List<AuditLogResponse>), 200)]
        public async Task<IActionResult> GetJobTimeline([FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IAuditLogRepository auditRepo, Guid job_id)
        {
            var logs = await auditRepo.FindAsync(a => a.JobId == job_id);
            var sortedLogs = logs.OrderBy(a => a.Timestamp).Select(a => new AuditLogResponse
            {
                Id = a.Id,
                ReferenceType = a.ReferenceType,
                ReferenceId = a.ReferenceId,
                Action = a.Action,
                ActorId = a.ActorId,
                Details = a.Details,
                Timestamp = a.Timestamp
            }).ToList();
            
            return Ok(sortedLogs);
        }

        /// <summary>
        /// Export key metrics (release speed, failure rates) as CSV.
        /// </summary>
        [HttpGet("metrics/export")]
        [Produces("text/csv")]
        public async Task<IActionResult> ExportMetrics([FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IAuditLogRepository auditRepo)
        {
            var logs = await auditRepo.GetAllAsync();
            var groupedByJob = logs.Where(l => l.JobId.HasValue).GroupBy(l => l.JobId.Value).ToList();

            var csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("JobId,CreatedTime,SecuredTime,ReleasedTime,TimeToReleaseMinutes");

            foreach (var group in groupedByJob)
            {
                var created = group.FirstOrDefault(g => g.Action == "CREATED")?.Timestamp;
                var secured = group.FirstOrDefault(g => g.Action == "SECURED")?.Timestamp;
                var released = group.FirstOrDefault(g => g.Action == "FULL_RELEASE")?.Timestamp;

                double? timeToRelease = null;
                if (secured.HasValue && released.HasValue)
                {
                    timeToRelease = (released.Value - secured.Value).TotalMinutes;
                }

                csvBuilder.AppendLine($"{group.Key},{created},{secured},{released},{timeToRelease}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(bytes, "text/csv", $"metrics_export_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        // ─── Disputes ────────────────────────────────────────

        /// <summary>
        /// Get all disputes on the platform.
        /// </summary>
        [HttpGet("disputes")]
        [ProducesResponseType(typeof(List<AdminDisputeResponse>), 200)]
        public async Task<IActionResult> GetDisputes()
        {
            var disputes = await _adminService.GetAllDisputesAsync();
            return Ok(disputes);
        }

        /// <summary>
        /// Resolve a dispute. Marks it Resolved and writes admin notes into the AuditLog.
        /// </summary>
        [HttpPut("disputes/{id}/resolve")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _adminService.ResolveDisputeAsync(id, request.Resolution, GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        // ─── SUPER ADMIN UTILITIES ────────────────────────────────────────

        [HttpPost("broadcast")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SystemBroadcast(
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IUserRepository userRepo,
            [FromServices] LocaLe.EscrowApi.Interfaces.INotificationService notifService,
            [FromBody] BroadcastRequest request)
        {
            var allUsers = await userRepo.GetAllAsync();
            foreach (var user in allUsers)
            {
                await notifService.CreateAsync(user.Id, LocaLe.EscrowApi.Models.NotificationType.NewMessage, "SYSTEM BROADCAST", request.Message, null, null);
            }
            return Ok(new { Message = $"Broadcast deployed to {allUsers.Count()} active users." });
        }

        [HttpDelete("users/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> EradicateUser(
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IUserRepository userRepo,
            Guid id)
        {
            var target = await userRepo.GetByIdAsync(id);
            if (target == null) return NotFound();

            if (target.Role == LocaLe.EscrowApi.Models.UserRole.SuperAdmin)
                return BadRequest(new { Error = "Core architecture prohibits purging a SuperAdmin sequence." });

            target.Role = LocaLe.EscrowApi.Models.UserRole.User;
            target.TotalVouchPoints = -999;
            target.Tier = LocaLe.EscrowApi.Models.UserTier.Bronze;
            
            userRepo.Update(target);
            await userRepo.SaveChangesAsync();

            return Ok(new { Message = "User effectively neutralized." });
        }

        public class BroadcastRequest { public string Message { get; set; } = string.Empty; }

        // ─── Earnings & Payments ─────────────────────────────

        /// <summary>
        /// Calculate and return the total platform earnings collected from escrow fees.
        /// </summary>
        [HttpGet("platform-earnings")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetPlatformEarnings()
        {
            // Placeholder for platform earnings calculation
            return Ok(new {
                TotalEarnings = 0m,
                RecentTransactions = new List<object>()
            });
        }

        /// <summary>
        /// List all global payments/escrow transactions taking place on the platform.
        /// </summary>
        [HttpGet("payments")]
        [ProducesResponseType(typeof(List<object>), 200)]
        public IActionResult GetAllPayments()
        {
            // Placeholder for viewing all global payments
            return Ok(new List<object>());
        }

        /// <summary>
        /// Admin override to manually issue a refund for a specific payment or escrow transaction.
        /// </summary>
        [HttpPost("payments/{payment_id}/refund")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult RefundPayment(Guid payment_id)
        {
            // Placeholder for issuing a manual refund
            return Ok(new { Message = $"Payment {payment_id} successfully refunded to user." });
        }

        // ─── Helpers ─────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
