using System.Security.Claims;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaLe.EscrowApi.Controllers
{
    /// <summary>
    /// In-app notification bell. Triggered automatically by key system events.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        /// <summary>Get your 30 most recent notifications, newest first.</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetCurrentUserId();
            var notes = await _notifications.GetForUserAsync(userId);
            return Ok(notes);
        }

        /// <summary>Get count of unread notifications (for the badge).</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notifications.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        /// <summary>Mark all notifications as read.</summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();
            await _notifications.MarkAllReadAsync(userId);
            return Ok(new { message = "All notifications marked read." });
        }

        /// <summary>Mark a specific notification as read.</summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = GetCurrentUserId();
            await _notifications.MarkReadAsync(userId, id);
            return Ok();
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
