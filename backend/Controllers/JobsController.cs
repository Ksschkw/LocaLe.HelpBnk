using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IBookingService _bookingService;

        public JobsController(IJobService jobService, IBookingService bookingService)
        {
            _jobService = jobService;
            _bookingService = bookingService;
        }

        /// <summary>
        /// Get all open jobs available in the marketplace.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<JobResponse>), 200)]
        public async Task<IActionResult> GetOpenJobs()
        {
            var jobs = await _jobService.GetOpenJobsAsync();
            return Ok(jobs);
        }

        /// <summary>
        /// Get a specific job by its ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(JobResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetJob(Guid id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null) return NotFound(new { Error = "Job not found." });
            return Ok(job);
        }

        /// <summary>
        /// Post a new job to the marketplace (you become the buyer/hirer).
        /// Requires authentication.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(JobResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                var job = await _jobService.CreateJobAsync(userId, request);
                return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing job (must be the creator and the job must be Open).
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(JobResponse), 200)]
        public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                var job = await _jobService.UpdateJobAsync(userId, id, request);
                return Ok(job);
            }
            catch (KeyNotFoundException) { return NotFound(new { Error = "Job not found." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        /// <summary>
        /// Delete an existing job (must be the creator and the job must be Open/Closed).
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteJob(Guid id)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _jobService.DeleteJobAsync(userId, id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(new { Error = "Job not found." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }

        /// <summary>
        /// View jobs or requests where you are the Buyer/Creator.
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize]
        [ProducesResponseType(typeof(List<JobResponse>), 200)]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetCurrentUserId();
            var jobs = await _jobService.GetMyRequestsAsync(userId);
            return Ok(jobs);
        }

        /// <summary>
        /// View jobs that specifically targeted your catalog services (You are the requested Provider).
        /// </summary>
        [HttpGet("my-service-requests")]
        [Authorize]
        [ProducesResponseType(typeof(List<JobResponse>), 200)]
        public async Task<IActionResult> GetMyServiceRequests()
        {
            var userId = GetCurrentUserId();
            var jobs = await _jobService.GetMyServiceRequestsAsync(userId);
            return Ok(jobs);
        }

        /// <summary>
        /// View jobs or bookings where you applied to offer your service.
        /// </summary>
        [HttpGet("my-offers")]
        [Authorize]
        [ProducesResponseType(typeof(List<BookingResponse>), 200)]
        public async Task<IActionResult> GetMyOffers()
        {
            var userId = GetCurrentUserId();
            var bookings = await _bookingService.GetBookingsForUserAsync(userId);
            return Ok(bookings);
        }

        /// <summary>
        /// Mark an ongoing job as officially Completed under Escrow guidelines.
        /// Must be called by the Buyer.
        /// </summary>
        [HttpPost("{job_id}/confirm-completion")]
        [Authorize]
        [ProducesResponseType(typeof(JobResponse), 200)]
        public async Task<IActionResult> ConfirmCompletion(Guid job_id)
        {
            var userId = GetCurrentUserId();
            try
            {
                var job = await _jobService.ConfirmCompletionAsync(userId, job_id);
                return Ok(job);
            }
            catch (KeyNotFoundException) { return NotFound(new { Error = "Job not found." }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// [PROVIDER] Accept a service request made to your catalog.
        /// This instantly creates a Booking, marks the Job as Assigned,
        /// and locks the Buyer's full payment into Escrow. Buyer must have sufficient wallet funds.
        /// </summary>
        [HttpPost("{job_id}/accept")]
        [Authorize]
        [ProducesResponseType(typeof(BookingResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AcceptJob(Guid job_id)
        {
            var userId = GetCurrentUserId();
            try
            {
                var booking = await _bookingService.AcceptJobAsync(job_id, userId);
                return CreatedAtAction(nameof(GetJob), new { id = job_id }, booking);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// Directly request a job using a specific Service catalog item.
        /// Bypasses the need to create an open unassigned job.
        /// </summary>
        [HttpPost("services/{service_id}/request")]
        [Authorize]
        [ProducesResponseType(typeof(JobResponse), 201)]
        public async Task<IActionResult> RequestService(Guid service_id, [FromBody] CreateJobRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                var job = await _jobService.CreateJobForServiceAsync(userId, service_id, request);
                return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
        }
    }
}
