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
        public async Task<IActionResult> Vouch(int serviceId, [FromBody] string? comment)
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

        [HttpGet("{serviceId}/points")]
        public async Task<IActionResult> GetPoints(int serviceId)
        {
            var points = await _vouchService.GetServicePointsAsync(serviceId);
            return Ok(new { ServiceId = serviceId, TotalPoints = points });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
