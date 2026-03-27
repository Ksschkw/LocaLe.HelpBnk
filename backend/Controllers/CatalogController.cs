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
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        /// <summary>
        /// Get all root categories. Each root category includes its full tree of subcategories.
        /// No parentId guessing required.
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryResponse>), 200)]
        public async Task<IActionResult> GetCategoriesTree()
        {
            var cats = await _catalogService.GetCategoriesTreeAsync();
            return Ok(cats);
        }

        /// <summary>
        /// Get a specific category and its immediate subcategories.
        /// </summary>
        [HttpGet("categories/{id}")]
        [ProducesResponseType(typeof(CategoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var cat = await _catalogService.GetCategoryByIdAsync(id);
            if (cat == null) return NotFound(new { Error = "Category not found." });
            return Ok(cat);
        }

        /// <summary>
        /// Get services listed under a specific category.
        /// </summary>
        [HttpGet("categories/{categoryId}/services")]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetServicesByCategory(int categoryId)
        {
            var services = await _catalogService.GetServicesByCategoryAsync(categoryId);
            return Ok(services);
        }

        /// <summary>
        /// Get details of a specific service.
        /// </summary>
        [HttpGet("services/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetService(int id)
        {
            var s = await _catalogService.GetServiceByIdAsync(id);
            if (s == null) return NotFound(new { Error = "Service not found." });
            return Ok(s);
        }

        /// <summary>
        /// Browse all active services. Optionally filter by category slug/name or search by keyword.
        /// No authentication required — anyone can browse the marketplace.
        /// </summary>
        [HttpGet("services")]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetAllServices(
            [FromQuery] string? search = null,
            [FromQuery] string? categoryName = null,
            [FromQuery] decimal? lat = null,
            [FromQuery] decimal? lng = null,
            [FromQuery] decimal? radiusKm = 10.0m) // Default 10km radius
        {
            var services = await _catalogService.GetAllServicesAsync(search, categoryName, lat, lng, radiusKm);
            return Ok(services);
        }

        /// <summary>
        /// Post a new service to the catalog (you become the provider).
        /// Requires authentication.
        /// </summary>
        [Authorize]
        [HttpPost("services")]
        [ProducesResponseType(typeof(ServiceResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var s = await _catalogService.CreateServiceAsync(providerId, request);
                return CreatedAtAction(nameof(GetService), new { id = s.Id }, s);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing service (must be the provider).
        /// </summary>
        [Authorize]
        [HttpPut("services/{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var s = await _catalogService.UpdateServiceAsync(providerId, id, request);
                return Ok(s);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
        }

        /// <summary>
        /// Delete an existing service (must be the provider).
        /// </summary>
        [Authorize]
        [HttpDelete("services/{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteService(int id)
        {
            var providerId = GetCurrentUserId();
            try
            {
                await _catalogService.DeleteServiceAsync(providerId, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
