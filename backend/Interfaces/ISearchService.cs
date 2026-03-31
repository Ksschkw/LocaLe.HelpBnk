using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface ISearchService
    {
        Task<List<ServiceResponse>> SearchServicesFuzzyAsync(string? query, decimal? lat = null, decimal? lon = null, double? radiusKm = null, bool? isRemote = null);
        Task<List<ServiceResponse>> GetAllCachedServicesAsync();
        Task<List<CategoryResponse>> SearchCategoriesAsync(string query);
        Task<List<string>> AutocompleteServicesAsync(string query);
        Task<List<string>> AutocompleteCategoriesAsync(string query);
    }
}
