using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepo;
        private readonly IWalletRepository _walletRepo;

        public UsersController(IAuthService authService, IUserRepository userRepo, IWalletRepository walletRepo)
        {
            _authService = authService;
            _userRepo = userRepo;
            _walletRepo = walletRepo;
        }

        /// <summary>
        /// Register a new LocaLe user.
        /// Automatically creates an associated Wallet with ₦0 balance.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                SetAuthCookie(result.Token, rememberMe: false);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
        }

        /// <summary>
        /// Login with email and password.
        /// Returns a JWT token and also stores it safely in an HttpOnly cookie.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                SetAuthCookie(result.Token, request.RememberMe);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { Error = ex.Message }); }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("locale_token");
            return Ok(new { Message = "Logged out successfully." });
        }

        /// <summary>
        /// Check your authenticated session and return your profile data.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> Me()
        {
            var userId = GetCurrentUserId();
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound(new { Error = "User not found." });

            return Ok(new {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Tier = user.Tier.ToString(),
                Role = user.Role.ToString(),
                TrustScore = user.TrustScore,
                Bio = user.Bio,
                JobsCompleted = user.JobsCompleted,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                AreaName = user.AreaName
            });
        }

        public class UpdateProfileRequest { 
            public string? Name { get; set; } 
            public string? Phone { get; set; } 
            public string? AvatarUrl { get; set; } 
            public string? Bio { get; set; } 
            public decimal? Latitude { get; set; } 
            public decimal? Longitude { get; set; } 
            public string? AreaName { get; set; } 
        }

        /// <summary>
        /// Update your own profile details (Name, Phone, or Avatar).
        /// Send only the fields you wish to update; leave others null.
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;
            if (request.Phone != null) user.Phone = request.Phone;
            if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
            if (request.Bio != null) user.Bio = request.Bio;
            if (request.Latitude.HasValue) user.Latitude = request.Latitude;
            if (request.Longitude.HasValue) user.Longitude = request.Longitude;
            if (request.AreaName != null) user.AreaName = request.AreaName;

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
            return Ok(new { Message = "Profile updated." });
        }

        /// <summary>
        /// Permanently delete your user account and all associated data.
        /// This action cannot be undone.
        /// </summary>
        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMe()
        {
            var userId = GetCurrentUserId();
            var user = await _userRepo.GetByIdAsync(userId);
            if (user != null) {
                _userRepo.Remove(user);
                await _userRepo.SaveChangesAsync();
            }
            Response.Cookies.Delete("locale_token");
            return NoContent();
        }

        /// <summary>
        /// Upgrade your account Tier to Platinum!
        /// This strictly deducts ₦5000 from your Wallet balance. Fails if insufficient funds.
        /// </summary>
        [HttpPost("me/purchase-badge")]
        [Authorize]
        public async Task<IActionResult> PurchaseBadge()
        {
            var userId = GetCurrentUserId();
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            var cost = 5000m; 

            if (wallet == null || wallet.Balance < cost)
                return BadRequest(new { Error = "Insufficient funds (₦5000 required) to purchase Elite Badge." });

            wallet.Balance -= cost;
            _walletRepo.Update(wallet);

            var user = await _userRepo.GetByIdAsync(userId);
            if (user != null)
            {
                user.Tier = Models.UserTier.Platinum;
                _userRepo.Update(user);
            }
            
            await _walletRepo.SaveChangesAsync();
            await _userRepo.SaveChangesAsync();

            return Ok(new { Message = "Elite badge purchased successfully! You are now Platinum tier." });
        }

        [HttpDelete("{user_id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteUser(Guid user_id)
        {
            var user = await _userRepo.GetByIdAsync(user_id);
            if (user == null) return NotFound();
            _userRepo.Remove(user);
            await _userRepo.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// View the public profile details of another user (e.g. before hiring them).
        /// Returns Name, Avatar, Tier, and TrustScore.
        /// </summary>
        [HttpGet("{user_id}")]
        public async Task<IActionResult> GetUser(Guid user_id)
        {
            var user = await _userRepo.GetByIdAsync(user_id);
            if (user == null) return NotFound();
            
            return Ok(new {
                UserId = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Tier = user.Tier.ToString(),
                TrustScore = user.TrustScore,
                CreatedAt = user.CreatedAt,
                Bio = user.Bio,
                JobsCompleted = user.JobsCompleted
            });
        }

        private void SetAuthCookie(string token, bool rememberMe)
        {
            var options = new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(24), Path = "/" };
            Response.Cookies.Append("locale_token", token, options);
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
