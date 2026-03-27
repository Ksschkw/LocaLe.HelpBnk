using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace LocaLe.EscrowApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepo, IWalletRepository walletRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _walletRepo = walletRepo;
            _config = config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            var exists = await _userRepo.ExistsByEmailAsync(request.Email.ToLowerInvariant());
            if (exists)
                throw new InvalidOperationException("A user with this email already exists.");

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User  // new users always start as regular users
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            // Auto-create a wallet for the new user (balance starts at 0)
            await _walletRepo.AddAsync(new Wallet { UserId = user.Id, Balance = 0m });
            await _walletRepo.SaveChangesAsync();

            var token = GenerateJwt(user, rememberMe: false);

            return new AuthResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            var token = GenerateJwt(user, request.RememberMe);

            return new AuthResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token
            };
        }

        private string GenerateJwt(User user, bool rememberMe)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                // Role claim enables [Authorize(Roles = "Admin,SuperAdmin")] on controllers
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var expiry = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(24);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

