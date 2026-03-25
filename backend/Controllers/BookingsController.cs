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
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Apply for an open job as a provider. Creates a Pending booking.
        /// </summary>
        [HttpPost("apply/{jobId}")]
        [ProducesResponseType(typeof(BookingResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ApplyToJob(int jobId)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var booking = await _bookingService.ApplyToJobAsync(jobId, providerId);
                return Created($"/api/bookings/{booking.Id}", booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Confirm a pending booking (buyer only). This triggers escrow fund locking.
        /// </summary>
        [HttpPost("{bookingId}/confirm")]
        [ProducesResponseType(typeof(BookingResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            var buyerId = GetCurrentUserId();
            try
            {
                var booking = await _bookingService.ConfirmBookingAsync(bookingId, buyerId);
                return Ok(booking);
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
        /// Get all bookings where the current user is either the buyer or the provider.
        /// </summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(List<BookingResponse>), 200)]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = GetCurrentUserId();
            var bookings = await _bookingService.GetBookingsForUserAsync(userId);
            return Ok(bookings);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
