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
            var result = await _adminService.GetAllUsersAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Get full details of a specific user.
        /// </summary>
        [HttpGet("users/{id}")]
        [ProducesResponseType(typeof(AdminUserResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(int id)
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
        public async Task<IActionResult> SetUserRole(int id, [FromBody] SetRoleRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _adminService.SetUserRoleAsync(id, request.Role, GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
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
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
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
        public async Task<IActionResult> ResolveDispute(int id, [FromBody] ResolveDisputeRequest request)
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

        // ─── Helpers ─────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
