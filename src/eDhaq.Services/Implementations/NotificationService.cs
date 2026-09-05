using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<Hubs.TrackingHub> _hubContext;

    public NotificationService(AppDbContext db, IHubContext<Hubs.TrackingHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    public async Task CreateAsync(string userId, string title, string message, NotificationType type, string? actionUrl = null, int? orderId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Notifications.AddAsync(notification);
        await _db.SaveChangesAsync();

        await _hubContext.Clients.User(userId).SendAsync("notificationReceived", new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.ActionUrl,
            notification.CreatedAt
        });
    }

        public async Task<List<Notification>> GetUnreadAsync(string userId)
        => await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public async Task<List<Notification>> GetAllAsync()
        => await _db.Notifications
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
