using eDhaq.Data;
using eDhaq.Common.DTOs;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Driver;

[Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;

    public IndexModel(AppDbContext db, IOrderService orderService)
    {
        _db = db;
        _orderService = orderService;
    }

    public int ActiveAssignments { get; private set; }
    public int ActivePickupAssignments { get; private set; }
    public int ActiveDeliveryAssignments { get; private set; }
    public List<DriverAssignment> CurrentTasks { get; private set; } = [];

    public bool CanStartTrip(DriverAssignment assignment)
        => assignment.Status is DriverJobAction.Pending or DriverJobAction.Accepted;

    public bool CanCompleteTask(DriverAssignment assignment)
        => assignment.Status != DriverJobAction.Completed;

    public string GetCompleteLabel(DriverAssignment assignment)
        => assignment.IsPickup ? "Picked Up" : "Delivered";

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (driver is null && (User.IsInRole("PickupDriver") || User.IsInRole("DeliveryDriver") || User.IsInRole("Administrator")))
        {
            // Auto-create a Driver profile if the user has a driver/admin role
            // but no Driver entity exists yet. This ensures the driver portal
            // shows data instead of an empty dashboard.
            driver = new eDhaq.Models.Entities.Driver
            {
                UserId = userId,
                Status = DriverStatus.Offline,
                CreatedAt = DateTime.UtcNow
            };
            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync();
        }

        if (driver is null)
        {
            return;
        }

        var activeQuery = _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id && x.Status != DriverJobAction.Completed);

        ActiveAssignments = await activeQuery.CountAsync();
        ActivePickupAssignments = await activeQuery.CountAsync(x => x.IsPickup);
        ActiveDeliveryAssignments = await activeQuery.CountAsync(x => !x.IsPickup);

        CurrentTasks = await _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id && x.Status != DriverJobAction.Completed)
            .Include(x => x.Order)
            .ThenInclude(x => x.Customer)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.AssignedAt)
            .Take(10)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostStartTripAsync(int assignmentId)
    {
        var assignment = await LoadAssignmentForCurrentUserAsync(assignmentId);
        if (assignment is null)
        {
            TempData["ErrorMessage"] = "Assignment not found.";
            return RedirectToPage();
        }

        if (!assignment.IsPickup && !assignment.Order.PickupActualAt.HasValue)
        {
            TempData["ErrorMessage"] = "Pickup must be completed before delivery can start.";
            return RedirectToPage();
        }

        var note = assignment.IsPickup ? "Driver is on the way to pickup." : "Driver is on the way to deliver.";
        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = assignment.OrderId,
            Status = assignment.IsPickup ? OrderStatus.DriverOnTheWay : OrderStatus.OutForDelivery,
            Note = note
        }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);

        if (!updated)
        {
            TempData["ErrorMessage"] = "Could not move order to the selected trip stage.";
            return RedirectToPage();
        }

        assignment.Status = DriverJobAction.Accepted;
        assignment.AcceptedAt ??= DateTime.UtcNow;
        assignment.Notes = note;

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = assignment.IsPickup ? "Pickup marked as on the way." : "Delivery marked as on the way.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteTaskAsync(int assignmentId)
    {
        var assignment = await LoadAssignmentForCurrentUserAsync(assignmentId);
        if (assignment is null)
        {
            TempData["ErrorMessage"] = "Assignment not found.";
            return RedirectToPage();
        }

        if (!assignment.IsPickup && !assignment.Order.PickupActualAt.HasValue)
        {
            TempData["ErrorMessage"] = "Pickup must be completed before delivery can be marked delivered.";
            return RedirectToPage();
        }

        var note = assignment.IsPickup ? "Pickup completed by driver." : "Delivery completed by driver.";
        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = assignment.OrderId,
            Status = assignment.IsPickup ? OrderStatus.ClothesPickedUp : OrderStatus.Delivered,
            Note = note
        }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);

        if (!updated)
        {
            TempData["ErrorMessage"] = "Could not complete this task due to current order stage.";
            return RedirectToPage();
        }

        assignment.Status = DriverJobAction.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.Notes = note;

        if (assignment.IsPickup)
        {
            assignment.Order.PickupActualAt = DateTime.UtcNow;
        }
        else
        {
            assignment.Order.DeliveryActualAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = assignment.IsPickup ? "Pickup completed." : "Delivery completed.";
        return RedirectToPage();
    }

    private async Task<DriverAssignment?> LoadAssignmentForCurrentUserAsync(int assignmentId)
    {
        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment is null)
        {
            return null;
        }

        if (User.IsInRole("Administrator"))
        {
            return assignment;
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (driver is null || assignment.DriverId != driver.Id)
        {
            return null;
        }

        return assignment;
    }
}
