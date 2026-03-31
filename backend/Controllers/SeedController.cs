using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SeedController : ControllerBase
    {
        private readonly EscrowContext _context;
        private readonly IMemoryCache _cache;

        public SeedController(EscrowContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> SeedAll()
        {
            // First, wipe existing so we cleanly seed fresh ones
            var existingServices = await _context.Services.ToListAsync();
            _context.Services.RemoveRange(existingServices);
            var existingCategories = await _context.ServiceCategories.ToListAsync();
            _context.ServiceCategories.RemoveRange(existingCategories);
            await _context.SaveChangesAsync();

            var users = new List<User>();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("string");

            // 1. Create 5 users (user1 to user5)
            for (int i = 1; i <= 5; i++)
            {
                var email = $"user{i}@example.com";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        Name = $"Test User {i}",
                        Email = email,
                        PasswordHash = passwordHash,
                        Role = UserRole.User,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _context.Wallets.Add(new Wallet { UserId = user.Id, Balance = 50000m });
                    await _context.SaveChangesAsync();
                }
                users.Add(user);
            }

            // 2. Create 6 unique categories
            var categoryNames = new[] { "Plumbing", "Electrical", "Carpentry", "IT Support", "Cleaning", "Tutoring" };
            var cats = new List<ServiceCategory>();

            foreach (var cName in categoryNames)
            {
                var cat = await _context.ServiceCategories.FirstOrDefaultAsync(c => c.Name == cName);
                if (cat == null)
                {
                    cat = new ServiceCategory
                    {
                        Name = cName,
                        Description = $"{cName} services available.",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ServiceCategories.Add(cat);
                    await _context.SaveChangesAsync();
                }
                cats.Add(cat);
            }

            // 3. Create 1 service for EACH user under EACH category (5 users * 6 categories = 30 services)
            int servicesCreated = 0;
            foreach (var user in users)
            {
                foreach (var cat in cats)
                {
                    var title = $"{user.Name}'s {cat.Name} Service";
                    if (!await _context.Services.AnyAsync(s => s.ProviderId == user.Id && s.CategoryId == cat.Id))
                    {
                        _context.Services.Add(new Service
                        {
                            ProviderId = user.Id,
                            CategoryId = cat.Id,
                            Title = title,
                            Description = $"Professional {cat.Name.ToLower()} offered by {user.Name}. Quick and reliable.",
                            BasePrice = new Random().Next((int)1000m, (int)20000m), // Random price
                            Status = "Active",
                            IsDiscoveryEnabled = true, // CRITICAL FIX: Make it discoverable!
                            TrustPoints = 50,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                        servicesCreated++;
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Clear Cache so search works instantly
            _cache.Remove("AllActiveServices");
            _cache.Remove("AllCategories");

            return Ok(new { Message = $"Seeded successfully. Users: {users.Count}, Categories: {cats.Count}, Services Created: {servicesCreated}." });
        }
    }
}
