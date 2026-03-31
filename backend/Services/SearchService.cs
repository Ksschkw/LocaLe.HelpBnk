using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LocaLe.EscrowApi.Services
{
    public class SearchService : ISearchService
    {
        private readonly IServiceRepository _serviceRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "AllActiveServices";
        private const string CatCacheKey = "AllCategories";

        public SearchService(IServiceRepository serviceRepo, ICategoryRepository categoryRepo, IMemoryCache cache)
        {
            _serviceRepo = serviceRepo;
            _categoryRepo = categoryRepo;
            _cache = cache;
        }

        public async Task<List<ServiceResponse>> GetAllCachedServicesAsync()
        {
            if (!_cache.TryGetValue(CacheKey, out List<ServiceResponse>? cachedServices) || cachedServices == null)
            {
                // Note: The previous ServiceRepo search method actually pulled active ones. 
                // Since this isn't directly on IServiceRepo yet, we just grab everything with IsDiscoveryEnabled
                // Because we're rewriting it, we'll hit the repository:
                var activeServices = await _serviceRepo.SearchServicesAsync(null, null, null, null, null); 
                
                cachedServices = activeServices.Select(s => new ServiceResponse
                {
                    Id = s.Id,
                    ProviderId = s.ProviderId,
                    ProviderName = s.Provider?.Name ?? "Unknown",
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category?.Name ?? "Unknown",
                    Title = s.Title,
                    Description = s.Description,
                    BasePrice = s.BasePrice,
                    HourlyRate = s.HourlyRate,
                    Status = s.Status,
                    TrustPoints = s.TrustPoints,
                    IsDiscoveryEnabled = s.IsDiscoveryEnabled,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    AreaName = s.AreaName
                }).ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(CacheKey, cachedServices, cacheOptions);
            }

            return cachedServices;
        }

        public async Task<List<ServiceResponse>> SearchServicesFuzzyAsync(string? query, decimal? lat = null, decimal? lon = null, double? radiusKm = null, bool? isRemote = null)
        {
            var all = await GetAllCachedServicesAsync();
            
            // 1. Initial Filtering (IsRemote / Location)
            if (isRemote.HasValue)
                all = all.Where(s => s.IsRemote == isRemote.Value).ToList();
            
            if (lat.HasValue && lon.HasValue && radiusKm.HasValue)
            {
                // Haversine / simple distance filtering
                all = all.Where(s => 
                {
                    if (s.IsRemote) return true; // remote services match everywhere
                    if (!s.Latitude.HasValue || !s.Longitude.HasValue) return false;
                    
                    var dLat = (double)(s.Latitude.Value - lat.Value) * Math.PI / 180.0;
                    var dLon = (double)(s.Longitude.Value - lon.Value) * Math.PI / 180.0;
                    var a = Math.Sin(dLat/2)*Math.Sin(dLat/2) + 
                            Math.Cos((double)lat.Value*Math.PI/180.0) * Math.Cos((double)s.Latitude.Value*Math.PI/180.0) * 
                            Math.Sin(dLon/2)*Math.Sin(dLon/2);
                    var d = 6371.0 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
                    return d <= radiusKm.Value;
                }).ToList();
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return all.OrderByDescending(s => s.TrustPoints).ToList();
            }

            var q = query.ToLowerInvariant().Trim();


            // 1. Strict exact substring match
            var exactMatches = all.Where(s => 
                (s.Title?.ToLowerInvariant().Contains(q) == true) || 
                (s.Description?.ToLowerInvariant().Contains(q) == true) ||
                (s.CategoryName?.ToLowerInvariant().Contains(q) == true) ||
                (s.ProviderName?.ToLowerInvariant().Contains(q) == true)
            ).ToList();

            if (exactMatches.Any())
            {
                return exactMatches.OrderByDescending(s => s.TrustPoints).ToList();
            }

            // 2. Fuzzy match: Check if all words are present somewhere in the service
            var words = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var fuzzyMatches = all.Where(s => 
            {
                var combinedText = $"{s.Title} {s.Description} {s.CategoryName} {s.ProviderName} {s.AreaName}".ToLowerInvariant();
                return words.All(w => combinedText.Contains(w));
            }).ToList();

            if (fuzzyMatches.Any())
            {
                return fuzzyMatches.OrderByDescending(s => s.TrustPoints).ToList();
            }

            // 3. Ultra Fuzzy: Check if ANY word matches
            var ultraFuzzyMatches = all.Where(s => 
            {
                var combinedText = $"{s.Title} {s.Description} {s.CategoryName} {s.ProviderName} {s.AreaName}".ToLowerInvariant();
                return words.Any(w => combinedText.Contains(w));
            }).ToList();

            if (ultraFuzzyMatches.Any())
            {
                return ultraFuzzyMatches.OrderByDescending(s => s.TrustPoints).ToList();
            }

            // 4. Fallback if absolutely nothing matches: Return top trusted generic services 
            // "so that no matter what a person entert something is returned"
            return all.OrderByDescending(s => s.TrustPoints).Take(5).ToList();
        }

        public async Task<List<CategoryResponse>> GetAllCachedCategoriesAsync()
        {
            if (!_cache.TryGetValue(CatCacheKey, out List<CategoryResponse>? cachedCats) || cachedCats == null)
            {
                var roots = await _categoryRepo.GetRootCategoriesTreeAsync();
                cachedCats = new List<CategoryResponse>();

                void Flatten(IEnumerable<LocaLe.EscrowApi.Models.ServiceCategory> cats)
                {
                    foreach (var c in cats)
                    {
                        cachedCats.Add(new CategoryResponse
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description,
                            IconUrl = c.IconUrl,
                            ParentId = c.ParentId
                        });
                        Flatten(c.SubCategories);
                    }
                }
                Flatten(roots);

                _cache.Set(CatCacheKey, cachedCats, TimeSpan.FromHours(1));
            }
            return cachedCats;
        }

        public async Task<List<CategoryResponse>> SearchCategoriesAsync(string query)
        {
            var all = await GetAllCachedCategoriesAsync();
            if (string.IsNullOrWhiteSpace(query)) return all;

            var q = query.ToLowerInvariant().Trim();
            var matches = all.Where(c => c.Name.ToLowerInvariant().Contains(q) || (c.Description != null && c.Description.ToLowerInvariant().Contains(q))).ToList();
            
            return matches.Any() ? matches : all.Take(5).ToList();
        }

        public async Task<List<string>> AutocompleteServicesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();
            var results = await SearchServicesFuzzyAsync(query, null, null, null, null);
            return results.Select(s => s.Title).Distinct().Take(5).ToList();
        }

        public async Task<List<string>> AutocompleteCategoriesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();
            var results = await SearchCategoriesAsync(query);
            return results.Select(c => c.Name).Distinct().Take(5).ToList();
        }
    }
}
