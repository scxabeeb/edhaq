using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Services.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, NotificationType type, string? actionUrl = null, int? orderId = null);
        Task<List<Notification>> GetUnreadAsync(string userId);
    Task<List<Notification>> GetAllAsync();
    Task MarkAsReadAsync(int notificationId, string userId);
}
