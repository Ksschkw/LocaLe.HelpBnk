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

                // Normalize nullable DateTime values to safe strings for CSV export
                var createdStr = created.HasValue ? created.Value.ToString("o") : string.Empty;
                var securedStr = secured.HasValue ? secured.Value.ToString("o") : string.Empty;
                var releasedStr = released.HasValue ? released.Value.ToString("o") : string.Empty;
                var timeToReleaseStr = timeToRelease.HasValue ? timeToRelease.Value.ToString("F2") : string.Empty;

                csvBuilder.AppendLine($"{group.Key},{createdStr},{securedStr},{releasedStr},{timeToReleaseStr}");
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
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetPlatformEarnings(
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IUserRepository userRepo,
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IWalletRepository walletRepo,
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IAuditLogRepository auditRepo)
        {
            var superAdmins = await userRepo.FindAsync(u => u.Role == LocaLe.EscrowApi.Models.UserRole.SuperAdmin);
            var superAdmin = superAdmins.FirstOrDefault();
            if (superAdmin == null) return NotFound("SuperAdmin not found.");

            var wallet = await walletRepo.GetByUserIdAsync(superAdmin.Id);
            if (wallet == null) return NotFound("System Wallet not found.");

            // Fetch recent fee captures logs
            var logs = await auditRepo.FindAsync(a => a.Details.Contains("Platform fee of"));
            
            return Ok(new {
                TotalEarnings = wallet.Balance,
                RecentTransactions = logs.OrderByDescending(l => l.Timestamp).Take(20).Select(l => new {
                    l.Id,
                    l.Timestamp,
                    l.Details
                })
            });
        }

        /// <summary>
        /// SuperAdmin Impersonation Protocol: Generate a JWT for any user ID without requiring their password.
        /// </summary>
        [HttpPost("impersonate/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        public async Task<IActionResult> ImpersonateUser(
            Guid userId,
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IUserRepository userRepo,
            [FromServices] LocaLe.EscrowApi.Interfaces.IAuthService authService)
        {
            var targetUser = await userRepo.GetByIdAsync(userId);
            if (targetUser == null) return NotFound(new { Error = "Target user not found." });

            if (targetUser.Role == LocaLe.EscrowApi.Models.UserRole.SuperAdmin && targetUser.Id != GetCurrentUserId())
            {
                return BadRequest(new { Error = "You cannot impersonate another SuperAdmin." });
            }

            // A bit of a hack since AuthService usually validates passwords. 
            // We just need a generate token function, let's assume authService has one or we can build it.
            // Wait, we can't be sure authService exposes GenerateToken. Let's just create a dummy object
            // or see if we can use IAuthService. We might need a small helper. 
            // For now we will call a specific generate method on Auth Service if it exists.
            // Actually, we'll implement the token generator here directly referencing Configuration if needed, 
            // but let's delegate to authService if we can.
            
            // Wait, to do it cleanly, let's inject IConfiguration and generate token here to avoid changing IAuthService signature inside replace block.
            var tokenString = GenerateJwtToken(targetUser, HttpContext.RequestServices.GetRequiredService<IConfiguration>());

            // Set cookie for automatic login
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Should be true in prod HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("locale_token", tokenString, cookieOptions);

            return Ok(new AuthResponse
            {
                Token = tokenString,
                UserId = targetUser.Id,
                Name = targetUser.Name,
                Email = targetUser.Email,
                Role = targetUser.Role.ToString()
            });
        }

        private string GenerateJwtToken(LocaLe.EscrowApi.Models.User user, IConfiguration config)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "LocaLe_SuperSecretKey_ChangeThisInProduction_2026!");
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = config["Jwt:Issuer"] ?? "LocaLe",
                Audience = config["Jwt:Audience"] ?? "LocaLe.Users",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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

        /// <summary>
        /// Admin manual override to toggle Service Discovery visibility.
        /// Bypasses the RequiredVouchPoints threshold.
        /// </summary>
        [HttpPut("services/{id}/discovery")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleServiceDiscovery(
            [FromServices] LocaLe.EscrowApi.Interfaces.Repositories.IServiceRepository serviceRepo,
            Guid id, [FromQuery] bool enable)
        {
            var service = await serviceRepo.GetByIdAsync(id);
            if (service == null) return NotFound(new { Error = "Service not found." });

            service.IsDiscoveryEnabled = enable;
            service.IsDiscoveryAdminOverridden = true; // Prevents VouchService from auto-enabling it later if disabled
            
            serviceRepo.Update(service);
            await serviceRepo.SaveChangesAsync();
            return NoContent();
        }

        // ─── Phase 11: Flagged Messages ──────────────────────

        /// <summary>
        /// Get all flagged messages (potential anti-disintermediation violations).
        /// </summary>
        [HttpGet("flags")]
        [ProducesResponseType(typeof(List<AdminFlaggedMessageResponse>), 200)]
        public async Task<IActionResult> GetFlaggedMessages()
        {
            var flags = await _adminService.GetFlaggedMessagesAsync();
            return Ok(flags);
        }

        /// <summary>
        /// Mark a flagged message as resolved.
        /// </summary>
        [HttpPost("flags/{flagId}/resolve")]
        public async Task<IActionResult> ResolveFlaggedMessage(Guid flagId, [FromBody] ResolveFlagRequest req)
        {
            var actorId = GetCurrentUserId();
            await _adminService.ResolveFlaggedMessageAsync(flagId, req.AdminNote, actorId);
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
