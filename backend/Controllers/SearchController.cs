using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Fuzzy search that caches services and falls back intuitively.
        /// Throws 400 if search string is empty (requested behavior).
        /// </summary>
        [HttpGet("services")]
        [ProducesResponseType(typeof(List<ServiceResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SearchServices([FromQuery] string? query, [FromQuery] decimal? lat, [FromQuery] decimal? lon, [FromQuery] double? radiusKm, [FromQuery] bool? isRemote)
        {
            try
            {
                var results = await _searchService.SearchServicesFuzzyAsync(query, lat, lon, radiusKm, isRemote);
                return Ok(results);
            }
            catch (ArgumentException ex)
            {
                // Returns nice message if query is empty
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Fuzzy search service categories by name or description.
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryResponse>), 200)]
        public async Task<IActionResult> SearchCategories([FromQuery] string? query = "")
        {
            var results = await _searchService.SearchCategoriesAsync(query ?? "");
            return Ok(results);
        }

        /// <summary>
        /// Get instant matching active service titles for a search bar dropdown.
        /// Extremely fast in-memory execution.
        /// </summary>
        [HttpGet("autocomplete/services")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> AutocompleteServices([FromQuery] string query)
        {
            var results = await _searchService.AutocompleteServicesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// Get instant matching category names for a search bar dropdown.
        /// </summary>
        [HttpGet("autocomplete/categories")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> AutocompleteCategories([FromQuery] string query)
        {
            var results = await _searchService.AutocompleteCategoriesAsync(query);
            return Ok(results);
        }
    }
}
