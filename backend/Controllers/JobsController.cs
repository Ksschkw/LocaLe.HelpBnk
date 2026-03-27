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

        public JobsController(IJobService jobService)
        {
            _jobService = jobService;
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
        public async Task<IActionResult> GetJob(int id)
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
        public async Task<IActionResult> UpdateJob(int id, [FromBody] UpdateJobRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                var job = await _jobService.UpdateJobAsync(userId, id, request);
                return Ok(job);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        /// <summary>
        /// Delete an existing job (must be the creator and the job must be Open/Closed).
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _jobService.DeleteJobAsync(userId, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
