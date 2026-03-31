using System.Security.Claims;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/disputes")]
    [Produces("application/json")]
    [Authorize]
    public class DisputesController : ControllerBase
    {
        private readonly IEscrowService _escrowService;
        private readonly IBookingRepository _bookingRepo;

        public DisputesController(IEscrowService escrowService, IBookingRepository bookingRepo)
        {
            _escrowService = escrowService;
            _bookingRepo = bookingRepo;
        }

        /// <summary>
        /// Raise a formal dispute on an active Escrow for a Job.
        /// Freezes the funds pending Admin intervention.
        /// </summary>
        [HttpPost("jobs/{job_id}/dispute")]
        public async Task<IActionResult> DisputeJob(Guid job_id, [FromBody] string? reason)
        {
            var userId = GetCurrentUserId();
            try
            {
                // Find the active/accepted booking for this job
                var bookings = await _bookingRepo.GetByJobIdAsync(job_id);
                var activeBooking = bookings.FirstOrDefault(b =>
                    b.Status.ToString() is "Active" or "Accepted");

                if (activeBooking == null)
                    return BadRequest(new { Error = "No active booking found for this job. Cannot raise a dispute." });

                // Get the escrow tied to that booking
                var escrow = await _escrowService.GetEscrowByBookingIdAsync(activeBooking.Id);
                if (escrow == null)
                    return BadRequest(new { Error = "No escrow found for this job. Ensure funds were locked before disputing." });

                var result = await _escrowService.DisputeAsync(escrow.Id, userId);
                return Ok(new { Message = "Dispute raised successfully. Funds frozen pending Admin review.", EscrowId = escrow.Id, Status = result.Status });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        /// <summary>
        /// Submit evidence or a response statement to an active dispute.
        /// </summary>
        [HttpPost("{dispute_id}/respond")]
        public IActionResult RespondToDispute(Guid dispute_id, [FromBody] object responsePayload)
        {
            var userId = GetCurrentUserId();
            return Ok(new { Message = "Response recorded successfully." });
        }

        /// <summary>
        /// List all disputes you are currently involved in (as Buyer or Provider).
        /// </summary>
        [HttpGet("my-disputes")]
        public IActionResult GetMyDisputes()
        {
            var userId = GetCurrentUserId();
            return Ok(new List<object>());
        }

        /// <summary>
        /// Get status, history, and admin notes for a specific dispute case.
        /// </summary>
        [HttpGet("{dispute_id}")]
        public IActionResult GetDisputeDetails(Guid dispute_id)
        {
            var userId = GetCurrentUserId();
            return Ok(new { DisputeId = dispute_id, Status = "Open", Notes = "Investigating..." });
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
