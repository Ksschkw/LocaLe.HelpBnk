using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class EscrowController : ControllerBase
    {
        private readonly IEscrowService _escrowService;

        public EscrowController(IEscrowService escrowService)
        {
            _escrowService = escrowService;
        }

        /// <summary>
        /// Get the escrow details for a specific booking.
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        [ProducesResponseType(typeof(EscrowResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByBooking(Guid bookingId)
        {
            var escrow = await _escrowService.GetEscrowByBookingIdAsync(bookingId);
            if (escrow == null) return NotFound(new { Error = "No escrow found for this booking." });
            return Ok(escrow);
        }

        /// <summary>
        /// Release escrowed funds to the provider by verifying the one-time QR token.
        /// Only the assigned provider can call this.
        /// </summary>
        [HttpPost("{escrowId}/release")]
        [ProducesResponseType(typeof(EscrowResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Release(Guid escrowId, [FromBody] ReleaseEscrowRequest request)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var result = await _escrowService.ReleaseFundsAsync(escrowId, request.QrToken, providerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Dispute an escrow — freezes funds pending resolution.
        /// Either the buyer or provider can initiate.
        /// </summary>
        [HttpPost("{escrowId}/dispute")]
        [ProducesResponseType(typeof(EscrowResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Dispute(Guid escrowId)
        {
            var actorId = GetCurrentUserId();
            try
            {
                var result = await _escrowService.DisputeAsync(escrowId, actorId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Cancel escrow and refund the buyer. Only the buyer can cancel.
        /// </summary>
        [HttpPost("{escrowId}/cancel")]
        [ProducesResponseType(typeof(EscrowResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Cancel(Guid escrowId)
        {
            var actorId = GetCurrentUserId();
            try
            {
                var result = await _escrowService.CancelAsync(escrowId, actorId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Generates a new QR token for a secured escrow if the old one expired.
        /// </summary>
        [HttpPost("{escrowId}/refresh-qr")]
        [ProducesResponseType(typeof(EscrowResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RefreshQr(Guid escrowId)
        {
            var buyerId = GetCurrentUserId();
            try
            {
                var result = await _escrowService.RefreshQrAsync(escrowId, buyerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get the immutable audit trail for an escrow.
        /// </summary>
        [HttpGet("{escrowId}/audit")]
        [ProducesResponseType(typeof(List<AuditLogResponse>), 200)]
        public async Task<IActionResult> GetAuditLogs(Guid escrowId)
        {
            var logs = await _escrowService.GetAuditLogsAsync(escrowId);
            return Ok(logs);
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
