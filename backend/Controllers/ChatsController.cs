using System.Security.Claims;
using System.Text.RegularExpressions;
using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ChatsController : ControllerBase
    {
        private readonly EscrowContext _context;
        private readonly INotificationService _notifications;

        public ChatsController(EscrowContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        public class ChatMessageResponse
        {
            public Guid Id { get; set; }
            public Guid JobId { get; set; }
            public Guid SenderId { get; set; }
            public string SenderName { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsEncrypted { get; set; }
            public bool IsPinned { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsSystemMessage { get; set; }
            public Guid? ParentMessageId { get; set; }
            public string? ParentContentPreview { get; set; }
            public string? ParentSenderName { get; set; }
            public DateTime SentAt { get; set; }
            public DateTime? EditedAt { get; set; }
        }

        public class SendMessageRequest
        {
            public string Content { get; set; } = string.Empty;
            public Guid? ParentMessageId { get; set; }
            /// <summary>Set to true if content is AES-GCM encrypted client-side.</summary>
            public bool IsEncrypted { get; set; } = false;
        }

        // ─── Anti-Disintermediation Patterns ─────────────────────────────────
        // Any message matching these patterns will be blocked before persistence.
        private static readonly (Regex Pattern, string Label)[] LeakPatterns =
        [
            (new Regex(@"\b0?[789]\d{9}\b",            RegexOptions.IgnoreCase), "phone number"),
            (new Regex(@"\b[\w.+-]+@[\w-]+\.[\w.]+\b", RegexOptions.IgnoreCase), "email address"),
            (new Regex(@"\bwhatsapp\b",                 RegexOptions.IgnoreCase), "WhatsApp reference"),
            (new Regex(@"\btelegram\b",                 RegexOptions.IgnoreCase), "Telegram reference"),
            (new Regex(@"\bcash\s*(pay|transfer|app)?\b", RegexOptions.IgnoreCase), "cash payment keyword"),
            (new Regex(@"\bbank\s*transfer\b",           RegexOptions.IgnoreCase), "bank transfer keyword"),
            (new Regex(@"\bpay\s*(me|you)\s*(outside|directly|privately)\b", RegexOptions.IgnoreCase), "off-platform payment"),
            (new Regex(@"\bopay\b|\bpalm\s*pay\b|\bkuda\b", RegexOptions.IgnoreCase), "third-party fintech app"),
        ];

        private static string? DetectLeakage(string content)
        {
            foreach (var (pattern, label) in LeakPatterns)
                if (pattern.IsMatch(content)) return label;
            return null;
        }
        // ─────────────────────────────────────────────────────────────────────

        private static ChatMessageResponse MapResponse(Message m) => new()
        {
            Id = m.Id,
            JobId = m.JobId,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Name ?? "Unknown",
            Content = m.IsDeleted ? "[Message Deleted]" : m.Content,
            IsEncrypted = m.IsEncrypted,
            IsPinned = m.IsPinned,
            IsDeleted = m.IsDeleted,
            IsSystemMessage = m.IsSystemMessage,
            ParentMessageId = m.ParentMessageId,
            ParentContentPreview = m.ParentContentPreview,
            ParentSenderName = m.ParentSenderName,
            SentAt = m.SentAt,
            EditedAt = m.EditedAt
        };

        /// <summary>
        /// Get all messages for a Job contract room.
        /// Pinned messages are returned first, then chronological.
        /// </summary>
        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobChat(Guid jobId)
        {
            var userId = GetCurrentUserId();
            if (!await IsUserAuthorizedForJobChat(jobId, userId)) return Forbid();

            // Mark unread as read for this user
            await _context.Messages
                .Where(m => m.JobId == jobId && m.BookingId == null && m.SenderId != userId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.JobId == jobId && m.BookingId == null)
                .OrderBy(m => m.IsPinned ? 0 : 1)
                .ThenBy(m => m.SentAt)
                .Select(m => MapResponse(m))
                .ToListAsync();

            return Ok(messages);
        }

        /// <summary>Get only pinned messages for a Job contract room.</summary>
        [HttpGet("{jobId}/pinned")]
        public async Task<IActionResult> GetPinnedMessages(Guid jobId)
        {
            var userId = GetCurrentUserId();
            if (!await IsUserAuthorizedForJobChat(jobId, userId)) return Forbid();

            var pinned = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.JobId == jobId && m.BookingId == null && m.IsPinned && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .Select(m => MapResponse(m))
                .ToListAsync();

            return Ok(pinned);
        }

        /// <summary>
        /// Post a message. Supports reply threading (set ParentMessageId).
        /// Supports E2E encrypted payloads (set IsEncrypted=true, Content = ciphertext).
        /// </summary>
        [HttpPost("{jobId}")]
        public async Task<IActionResult> SendMessage(Guid jobId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { Error = "Message content cannot be empty." });

            // ─ Anti-disintermediation gate ─
            var leakLabel = DetectLeakage(request.Content);
            if (leakLabel != null)
            {
                var violatorId = GetCurrentUserId();
                var violator = await _context.Users.FindAsync(violatorId);

                // 1. Persist a FlaggedMessage for admin review
                _context.FlaggedMessages.Add(new FlaggedMessage
                {
                    OffenderId = violatorId,
                    OffenderName = violator?.Name ?? "Unknown",
                    JobId = jobId,
                    BlockedContent = request.Content,
                    ViolationType = leakLabel
                });

                // 2. Inject system warning into the chat so BOTH parties see it
                _context.Messages.Add(new Message
                {
                    JobId = jobId,
                    BookingId = null,
                    SenderId = violatorId,
                    Content = $"⚠️ PLATFORM WARNING: {violator?.Name ?? "A user"} attempted to share a {leakLabel}. " +
                              "Transacting outside LocaLe voids all Escrow protection and may result in a permanent ban. " +
                              "This incident has been reported to LocaLe admins.",
                    IsSystemMessage = true,
                    IsRead = false
                });

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    Error = $"⚠️ Message blocked: contains a {leakLabel}. Sharing personal contact or payment details violates LocaLe’s terms and voids all Escrow protection.",
                    IsLeakageViolation = true
                });
            }

            var userId = GetCurrentUserId();
            if (!await IsUserAuthorizedForJobChat(jobId, userId)) return Forbid();

            string? parentPreview = null;
            string? parentSenderName = null;

            if (request.ParentMessageId.HasValue)
            {
                var parent = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.Id == request.ParentMessageId && m.JobId == jobId && m.BookingId == null);
                if (parent != null)
                {
                    parentPreview = parent.IsDeleted
                        ? "[Message Deleted]"
                        : (parent.Content.Length > 100 ? parent.Content[..100] + "..." : parent.Content);
                    parentSenderName = parent.Sender?.Name;
                }
            }

            var sender = await _context.Users.FindAsync(userId);
            var message = new Message
            {
                JobId = jobId,
                BookingId = null,
                SenderId = userId,
                Content = request.Content,
                IsEncrypted = request.IsEncrypted,
                ParentMessageId = request.ParentMessageId,
                ParentContentPreview = parentPreview,
                ParentSenderName = parentSenderName
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Fire notification to the other party
            var job = await _context.Jobs.FindAsync(jobId);
            if (job != null)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.JobId == jobId);
                
                Guid? providerFallback = null;
                if (booking == null && job.ServiceId.HasValue)
                {
                    var service = await _context.Services.FindAsync(job.ServiceId.Value);
                    providerFallback = service?.ProviderId;
                }

                var otherUserId = (job.CreatorId == userId)
                    ? (booking?.ProviderId ?? providerFallback)
                    : (Guid?)job.CreatorId;

                if (otherUserId.HasValue && otherUserId.Value != userId)
                {
                    await _notifications.CreateAsync(
                        otherUserId.Value,
                        NotificationType.NewMessage,
                        $"New message from {sender?.Name ?? "Someone"}",
                        request.IsEncrypted ? "🔒 Encrypted message" : (request.Content.Length > 80 ? request.Content[..80] + "..." : request.Content),
                        jobId, "Job"
                    );
                }
            }

            return Ok(new ChatMessageResponse
            {
                Id = message.Id,
                JobId = message.JobId,
                SenderId = message.SenderId,
                SenderName = sender?.Name ?? "Me",
                Content = message.Content,
                IsEncrypted = message.IsEncrypted,
                IsPinned = false,
                IsDeleted = false,
                ParentMessageId = message.ParentMessageId,
                ParentContentPreview = parentPreview,
                ParentSenderName = parentSenderName,
                SentAt = message.SentAt
            });
        }

        /// <summary>Toggle pin on a message. Only job participants can pin.</summary>
        [HttpPost("{jobId}/messages/{messageId}/pin")]
        public async Task<IActionResult> TogglePin(Guid jobId, Guid messageId)
        {
            var userId = GetCurrentUserId();
            if (!await IsUserAuthorizedForJobChat(jobId, userId)) return Forbid();

            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.JobId == jobId && m.BookingId == null);
            if (msg == null) return NotFound();

            msg.IsPinned = !msg.IsPinned;
            await _context.SaveChangesAsync();

            return Ok(new { isPinned = msg.IsPinned });
        }

        /// <summary>Soft-delete your own message.</summary>
        [HttpDelete("{jobId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid jobId, Guid messageId)
        {
            var userId = GetCurrentUserId();
            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.JobId == jobId && m.BookingId == null);
            if (msg == null) return NotFound();
            if (msg.SenderId != userId) return Forbid();

            msg.IsDeleted = true;
            msg.Content = "[Message Deleted]";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message soft-deleted." });
        }

        /// <summary>
        /// Get all messages for a Pre-Hire (Booking) room.
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetBookingChat(Guid bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings.Include(b => b.Job).FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            if (booking.ProviderId != userId && (booking.Job == null || booking.Job.CreatorId != userId))
                return Forbid();

            // Mark unread as read for this user
            await _context.Messages
                .Where(m => m.BookingId == bookingId && m.SenderId != userId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.BookingId == bookingId)
                .OrderBy(m => m.SentAt)
                .Select(m => MapResponse(m))
                .ToListAsync();

            return Ok(messages);
        }

        /// <summary>
        /// Post a message to a Pre-Hire (Booking) room.
        /// </summary>
        [HttpPost("booking/{bookingId}")]
        public async Task<IActionResult> SendBookingMessage(Guid bookingId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { Error = "Message content cannot be empty." });

            // ─ Anti-disintermediation gate ─
            var leakLabel = DetectLeakage(request.Content);
            if (leakLabel != null)
            {
                var violatorId = GetCurrentUserId();
                var violator = await _context.Users.FindAsync(violatorId);

                // 1. Persist a FlaggedMessage for admin review
                _context.FlaggedMessages.Add(new FlaggedMessage
                {
                    OffenderId = violatorId,
                    OffenderName = violator?.Name ?? "Unknown",
                    BookingId = bookingId,
                    BlockedContent = request.Content,
                    ViolationType = leakLabel
                });

                // 2. Inject system warning into the interview room
                _context.Messages.Add(new Message
                {
                    // Find the job from the booking to keep FK happy
                    JobId = (await _context.Bookings.FindAsync(bookingId))?.JobId ?? Guid.Empty,
                    BookingId = bookingId,
                    SenderId = violatorId,
                    Content = $"⚠️ PLATFORM WARNING: {violator?.Name ?? "A user"} attempted to share a {leakLabel}. " +
                              "Transacting outside LocaLe voids all Escrow protection and may result in a permanent ban. " +
                              "This incident has been reported to LocaLe admins.",
                    IsSystemMessage = true,
                    IsRead = false
                });

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    Error = $"⚠️ Message blocked: contains a {leakLabel}. Sharing personal contact or payment details violates LocaLe's terms and voids all Escrow protection.",
                    IsLeakageViolation = true
                });
            }

            var userId = GetCurrentUserId();
            var booking = await _context.Bookings.Include(b => b.Job).FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            if (booking.ProviderId != userId && (booking.Job == null || booking.Job.CreatorId != userId))
                return Forbid();

            string? parentPreview = null;
            string? parentSenderName = null;

            if (request.ParentMessageId.HasValue)
            {
                var parent = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.Id == request.ParentMessageId && m.BookingId == bookingId);
                if (parent != null)
                {
                    parentPreview = parent.IsDeleted
                        ? "[Message Deleted]"
                        : (parent.Content.Length > 100 ? parent.Content[..100] + "..." : parent.Content);
                    parentSenderName = parent.Sender?.Name;
                }
            }

            var sender = await _context.Users.FindAsync(userId);
            var message = new Message
            {
                JobId = booking.JobId,
                BookingId = bookingId,
                SenderId = userId,
                Content = request.Content,
                IsEncrypted = request.IsEncrypted,
                ParentMessageId = request.ParentMessageId,
                ParentContentPreview = parentPreview,
                ParentSenderName = parentSenderName
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Notify the other party
            var otherUserId = (booking.Job!.CreatorId == userId) ? booking.ProviderId : booking.Job.CreatorId;
            await _notifications.CreateAsync(
                otherUserId,
                NotificationType.NewMessage,
                $"New interview message from {sender?.Name ?? "Someone"}",
                request.IsEncrypted ? "🔒 Encrypted message" : (request.Content.Length > 80 ? request.Content[..80] + "..." : request.Content),
                bookingId, "Booking"
            );

            return Ok(new ChatMessageResponse
            {
                Id = message.Id,
                JobId = message.JobId,
                SenderId = message.SenderId,
                SenderName = sender?.Name ?? "Me",
                Content = message.Content,
                IsEncrypted = message.IsEncrypted,
                IsPinned = false,
                IsDeleted = false,
                ParentMessageId = message.ParentMessageId,
                ParentContentPreview = parentPreview,
                ParentSenderName = parentSenderName,
                SentAt = message.SentAt
            });
        }

        private async Task<bool> IsUserAuthorizedForJobChat(Guid jobId, Guid userId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return false;
            if (job.CreatorId == userId) return true;

            var hasBooking = await _context.Bookings.AnyAsync(b => b.JobId == jobId && b.ProviderId == userId);
            if (hasBooking) return true;

            if (job.ServiceId.HasValue)
            {
                var service = await _context.Services.FindAsync(job.ServiceId.Value);
                if (service != null && service.ProviderId == userId) return true;
            }

            return false;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException();
            return Guid.Parse(claim);
        }
    }
}
