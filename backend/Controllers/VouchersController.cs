using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VouchersController : ControllerBase
    {
        private readonly IVouchService _vouchService;

        public VouchersController(IVouchService vouchService)
        {
            _vouchService = vouchService;
        }

        [Authorize]
        [HttpPost("{serviceId}")]
        public async Task<IActionResult> Vouch(Guid serviceId, [FromBody] string? comment)
        {
            var voucherId = GetCurrentUserId();
            try
            {
                await _vouchService.VouchAsync(serviceId, voucherId, comment);
                return Ok(new { Message = "Vouch recorded successfully." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
        }

        [AllowAnonymous]
        [HttpPost("guest/{serviceId}")]
        public async Task<IActionResult> GuestVouch(Guid serviceId, [FromBody] GuestVouchRequest request)
        {
            // Extract IP and UserAgent for fingerprint heuristic
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var ua = HttpContext.Request.Headers["User-Agent"].ToString();

            try
            {
                await _vouchService.GuestVouchAsync(serviceId, request.Phone, request.Name, ip, ua, request.Comment);
                return Ok(new { Message = "Guest vouch recorded successfully." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
        }

        [AllowAnonymous]
        [HttpGet("{serviceId}/points")]
        [ProducesResponseType(typeof(ServicePointsResponse), 200)]
        public async Task<IActionResult> GetPoints(Guid serviceId)
        {
            var breakdown = await _vouchService.GetServicePointsBreakdownAsync(serviceId);
            return Ok(new ServicePointsResponse
            {
                ServiceId = serviceId,
                TotalPoints = breakdown.Total,
                PlatformPoints = breakdown.Platform,
                GuestPoints = breakdown.Guest
            });
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
