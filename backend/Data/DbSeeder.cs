using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceScope scope)
        {
            var context = scope.ServiceProvider.GetRequiredService<EscrowContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await SeedSuperAdminAsync(context, config);
        }

        private static async Task SeedSuperAdminAsync(EscrowContext context, IConfiguration config)
        {
            var email = config["SuperAdmin:Email"] ?? "admin@locale.ng";
            var password = config["SuperAdmin:Password"] ?? "SuperAdmin2026!";
            var name = config["SuperAdmin:Name"] ?? "LocaLe SuperAdmin";

            if (await context.Users.AnyAsync(u => u.Email == email)) return;

            var admin = new User
            {
                Name = name,
                Email = email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            context.Wallets.Add(new Wallet { UserId = admin.Id, Balance = 0m });
            await context.SaveChangesAsync();

            Console.WriteLine($"[Seeder] SuperAdmin created: {email}");
        }
    }
}
