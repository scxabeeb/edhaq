using eDhaq.Data;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<eDhaq.Models.Entities.Notification> Notifications { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageSize { get; } = 20;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public NotificationType? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? UnreadOnly { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var query = _db.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (TypeFilter.HasValue)
        {
            query = query.Where(n => n.Type == TypeFilter.Value);
        }

        if (UnreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .AsQueryable();

        TotalCount = await query.CountAsync();

        Notifications = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int notificationId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage(new { PageNumber, TypeFilter, UnreadOnly });
        }

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null)
        {
            return RedirectToPage(new { PageNumber, TypeFilter, UnreadOnly });
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Notification marked as read.";
        return RedirectToPage(new { PageNumber, TypeFilter, UnreadOnly });
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage(new { PageNumber, TypeFilter, UnreadOnly });
        }

        var query = _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .AsQueryable();

        if (TypeFilter.HasValue)
        {
            query = query.Where(n => n.Type == TypeFilter.Value);
        }

        if (UnreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query.ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (notifications.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = notifications.Count > 0
            ? $"Marked {notifications.Count} notifications as read."
            : "No unread notifications matched your current filter.";

        return RedirectToPage(new { PageNumber, TypeFilter, UnreadOnly });
    }
}
