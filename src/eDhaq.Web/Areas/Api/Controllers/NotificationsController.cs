
using eDhaq.Data;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class NotificationsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public NotificationsController(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications([FromQuery] bool unreadOnly = true)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        List<eDhaq.Models.Entities.Notification> notifications;
        if (unreadOnly)
        {
            notifications = (await _notificationService.GetUnreadAsync(userId)).ToList();
        }
        else
        {
            notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        var result = notifications.Select(ToDto).ToList();
        return Ok(result);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        await _notificationService.MarkAsReadAsync(id, userId);
        return NoContent();
    }

    private static NotificationDto ToDto(eDhaq.Models.Entities.Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            ActionUrl = n.ActionUrl,
            OrderId = n.OrderId,
            CreatedAt = n.CreatedAt,
            ReadAt = n.ReadAt
        };
    }
}
