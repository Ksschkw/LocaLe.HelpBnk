using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new LocaLe user. Automatically creates a wallet (balance ₦0).
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                SetAuthCookie(result.Token, rememberMe: false);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Login with email and password. Returns a JWT also stored as an HttpOnly cookie.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                SetAuthCookie(result.Token, request.RememberMe);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Logout by clearing the auth cookie.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("locale_token");
            return Ok(new { Message = "Logged out successfully." });
        }

        /// <summary>
        /// Check who is currently authenticated (based on the JWT cookie or header).
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Error = "Not authenticated." });

            return Ok(new
            {
                UserId = int.Parse(userId),
                Name = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email)
            });
        }

        private void SetAuthCookie(string token, bool rememberMe)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,     // JS cannot read this cookie (XSS protection)
                Secure = false,      // Set to true in production (HTTPS only)
                SameSite = SameSiteMode.Lax,
                Expires = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(24),
                Path = "/"
            };

            Response.Cookies.Append("locale_token", token, options);
        }
    }
}
