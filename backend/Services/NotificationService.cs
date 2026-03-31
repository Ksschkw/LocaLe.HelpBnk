using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly EscrowContext _context;

        public NotificationService(EscrowContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Guid userId, NotificationType type, string title, string body,
            Guid? referenceId = null, string? referenceType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                ReferenceId = referenceId,
                ReferenceType = referenceType
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetForUserAsync(Guid userId, int limit = 30)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAllReadAsync(Guid userId)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task MarkReadAsync(Guid userId, Guid notificationId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (n != null) { n.IsRead = true; await _context.SaveChangesAsync(); }
        }
    }
}
