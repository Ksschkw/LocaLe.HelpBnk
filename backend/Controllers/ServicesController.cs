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
    public class ServicesController : ControllerBase
    {
        private readonly IServicesService _servicesService;

        public ServicesController(IServicesService servicesService)
        {
            _servicesService = servicesService;
        }

        /// <summary>
        /// List all global service categories (Flat list).
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryResponse>), 200)]
        public async Task<IActionResult> GetCategories()
        {
            var tree = await _servicesService.GetCategoriesTreeAsync();
            var flat = new List<CategoryResponse>();

            void Flatten(IEnumerable<CategoryResponse> categories)
            {
                foreach (var c in categories)
                {
                    flat.Add(c);
                    Flatten(c.SubCategories);
                }
            }

            Flatten(tree);
            return Ok(flat);
        }

        [HttpGet("categories/{id}")]
        [ProducesResponseType(typeof(CategoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCategory(Guid id)
        {
            var cat = await _servicesService.GetCategoryByIdAsync(id);
            if (cat == null) return NotFound(new { Error = "Category not found." });
            return Ok(cat);
        }

        [HttpGet("categories/{categoryId}/services")]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetServicesByCategory(Guid categoryId)
        {
            var services = await _servicesService.GetServicesByCategoryAsync(categoryId);
            return Ok(services);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetService(Guid id)
        {
            var s = await _servicesService.GetServiceByIdAsync(id);
            if (s == null) return NotFound(new { Error = "Service not found." });
            return Ok(s);
        }

        /// <summary>
        /// Get a flat list of all globally Active and Discoverable services on the platform.
        /// Useful for a homepage feed.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetAllServices([FromQuery] double? lat, [FromQuery] double? lon, [FromQuery] double? radiusKm)
        {
            var services = await _servicesService.GetAllActiveServicesAsync();

            // Trust Engine: Bubble highest trust services to the top naturally
            services = services.OrderByDescending(s => s.TrustPoints).ToList();

            return Ok(services);
        }

        /// <summary>
        /// Create a brand new Custom Category.
        /// Use this if your service doesn't fit into the existing pre-seeded categories.
        /// </summary>
        [Authorize]
        [HttpPost("categories")]
        [ProducesResponseType(typeof(CategoryResponse), 201)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var c = await _servicesService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategory), new { id = c.Id }, c);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var s = await _servicesService.CreateServiceAsync(providerId, request);
                return CreatedAtAction(nameof(GetService), new { id = s.Id }, s);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Completely edit your service details.
        /// Send nulls for fields you do not wish to change.
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var s = await _servicesService.UpdateServiceAsync(providerId, id, request);
                return Ok(s);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var providerId = GetCurrentUserId();
            try
            {
                await _servicesService.DeleteServiceAsync(providerId, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }

        /// <summary>
        /// See exactly what Services you have created, including inactive/hidden ones.
        /// Useful for your personal seller dashboard.
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetMyServices()
        {
            var providerId = GetCurrentUserId();
            var services = await _servicesService.GetServicesByProviderAsync(providerId);
            return Ok(services);
        }

        /// <summary>
        /// See all services offered by another specific user/seller.
        /// Used when clicking on someone's profile to see what else they sell.
        /// </summary>
        [HttpGet("seller/{seller_id}")]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        public async Task<IActionResult> GetServicesBySeller(Guid seller_id)
        {
            var services = await _servicesService.GetServicesByProviderAsync(seller_id);
            return Ok(services);
        }

        /// <summary>
        /// Instantly Activate a service to make it visible to the public.
        /// Necessary after creating a brand new service via POST /api/Services.
        /// </summary>
        [HttpPost("{service_id}/activate")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse), 200)]
        public async Task<IActionResult> ActivateService(Guid service_id)
        {
            var providerId = GetCurrentUserId();
            try
            {
                var s = await _servicesService.ActivateServiceAsync(providerId, service_id);
                return Ok(s);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
