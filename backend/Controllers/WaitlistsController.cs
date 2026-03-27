using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Models;
using LocaLe.EscrowApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class WaitlistsController : ControllerBase
    {
        private readonly IWaitlistService _waitlistService;

        public WaitlistsController(IWaitlistService waitlistService)
        {
            _waitlistService = waitlistService;
        }

        [Authorize]
        [HttpPost("{serviceId}")]
        public async Task<IActionResult> JoinWaitlist(int serviceId, [FromBody] string? notes)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _waitlistService.JoinWaitlistAsync(serviceId, userId, notes);
                return Ok(new { Message = "Joined waitlist successfully." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyWaitlist()
        {
            var userId = GetCurrentUserId();
            var entries = await _waitlistService.GetUserWaitlistAsync(userId);
            
            var resp = entries.Select(e => new WaitlistResponse
            {
                Id = e.Id,
                ServiceId = e.ServiceId,
                ServiceTitle = e.Service?.Title ?? "Unknown",
                UserId = e.UserId,
                UserName = e.User?.Name ?? "Me",
                Status = e.Status.ToString(),
                CreatedAt = e.CreatedAt
            });

            return Ok(resp);
        }

        [Authorize]
        [HttpPost("{id}/agree")]
        public async Task<IActionResult> AgreeToTerms(int id, [FromBody] AgreeWaitlistTermsRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _waitlistService.AgreeToTermsAsync(id, userId, request.InitialDepositPercent);
                return Ok(new { Message = "Terms agreed. Job and Escrow created." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
