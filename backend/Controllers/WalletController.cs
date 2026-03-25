using System.Security.Claims;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get the current user's wallet balance.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(WalletResponse), 200)]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = GetCurrentUserId();
            var wallet = await _walletService.GetOrCreateWalletAsync(userId);
            return Ok(wallet);
        }

        /// <summary>
        /// Top up the current user's wallet with test funds.
        /// In production, this would be replaced by a payment gateway webhook.
        /// </summary>
        [HttpPost("topup")]
        [ProducesResponseType(typeof(WalletResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
        {
            var userId = GetCurrentUserId();
            try
            {
                var wallet = await _walletService.TopUpAsync(userId, request.Amount);
                return Ok(wallet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return int.Parse(claim);
        }
    }
}
