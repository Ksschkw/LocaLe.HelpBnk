using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, NotificationType type, string title, string body, Guid? referenceId = null, string? referenceType = null);
        Task<List<Notification>> GetForUserAsync(Guid userId, int limit = 30);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAllReadAsync(Guid userId);
        Task MarkReadAsync(Guid userId, Guid notificationId);
    }
}
