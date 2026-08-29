using eDhaq.Common.Constants;
using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DriverEntity = eDhaq.Models.Entities.Driver;

namespace eDhaq.Web.Pages.Staff.Orders;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class ProcessModel : PageModel
{
    private static readonly OrderStatus[] ProcessingStages =
    [
        OrderStatus.LaundryReceived,
        OrderStatus.Sorting,
        OrderStatus.Washing,
        OrderStatus.DryCleaning,
        OrderStatus.Drying,
        OrderStatus.Ironing,
        OrderStatus.Folding,
        OrderStatus.Packaging,
        OrderStatus.ReadyForDelivery
    ];

    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;

    private static readonly Dictionary<OrderStatus, OrderStatus> LaundryStageFlow = new()
    {
        [OrderStatus.LaundryReceived] = OrderStatus.Sorting,
        [OrderStatus.Sorting] = OrderStatus.Washing,
        [OrderStatus.Washing] = OrderStatus.Drying,
        [OrderStatus.Drying] = OrderStatus.Ironing,
        [OrderStatus.Ironing] = OrderStatus.Folding,
        [OrderStatus.Folding] = OrderStatus.Packaging,
        [OrderStatus.Packaging] = OrderStatus.ReadyForDelivery
    };

    public ProcessModel(AppDbContext db, IOrderService orderService)
    {
        _db = db;
        _orderService = orderService;
    }

    public List<Order> Orders { get; private set; } = [];
    public List<SelectListItem> DriverOptions { get; private set; } = [];
    public List<SelectListItem> ServiceOptions { get; private set; } = [];
    public List<SelectListItem> ProcessingStageOptions { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageSize { get; } = 20;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public OrderStatus? StatusFilter { get; set; }

    [BindProperty]
    public int OrderId { get; set; }

    [BindProperty]
    public List<int> SelectedOrderIds { get; set; } = [];

    [BindProperty]
    public DateTime PickupScheduledAt { get; set; }

    [BindProperty]
    public int DriverId { get; set; }

    [BindProperty]
    public int ServiceId { get; set; }

    [BindProperty]
    public OrderStatus ProcessingStatus { get; set; }

    public bool CanAdvance(OrderStatus status) => LaundryStageFlow.ContainsKey(status);

    public OrderStatus? GetNextStage(OrderStatus status)
        => LaundryStageFlow.TryGetValue(status, out var next) ? next : null;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        var stages = new[]
        {
            OrderStatus.OrderPlaced,
            OrderStatus.PickupScheduled,
            OrderStatus.DriverAssigned,
            OrderStatus.DriverOnTheWay,
            OrderStatus.ClothesPickedUp,
            OrderStatus.LaundryReceived,
            OrderStatus.Sorting,
            OrderStatus.Washing,
            OrderStatus.DryCleaning,
            OrderStatus.Drying,
            OrderStatus.Ironing,
            OrderStatus.Folding,
            OrderStatus.Packaging,
            OrderStatus.ReadyForDelivery
        };

        var query = _db.Orders
            .Where(x => stages.Contains(x.Status))
            .Include(x => x.Customer).ThenInclude(x => x.User)
            .Include(x => x.Items).ThenInclude(i => i.Service)
            .Include(x => x.DriverAssignments).ThenInclude(a => a.Driver).ThenInclude(d => d.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(search)
                                     || x.Customer.User.FirstName.Contains(search)
                                     || x.Customer.User.LastName.Contains(search)
                                     || x.Customer.User.Email!.Contains(search));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == StatusFilter.Value);
        }

        TotalCount = await query.CountAsync();

        Orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAdvanceAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == OrderId);
        if (order is null)
        {
            TempData["ErrorMessage"] = "Order was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        if (!LaundryStageFlow.TryGetValue(order.Status, out var expectedNext))
        {
            TempData["ErrorMessage"] = $"Order {order.OrderNumber} cannot be advanced from {order.Status}.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorName = User.Identity?.Name;

        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = OrderId,
            Status = expectedNext,
            Note = "Updated by laundry staff"
        }, actorId, actorName);

        if (!updated)
        {
            TempData["ErrorMessage"] = "Could not update this order. It may have moved to a different stage.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        TempData["SuccessMessage"] = "Processing stage updated.";
        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostSchedulePickupAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == OrderId);
        if (order is null)
        {
            TempData["ErrorMessage"] = "Order was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        order.PickupScheduledAt = PickupScheduledAt;
        if (order.Status == OrderStatus.OrderPlaced)
        {
            order.Status = OrderStatus.PickupScheduled;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Pickup time scheduled.";
        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostAssignDriverAsync(bool isPickup)
    {
        var order = await _db.Orders
            .Include(x => x.DriverAssignments)
            .FirstOrDefaultAsync(x => x.Id == OrderId);

        if (order is null)
        {
            TempData["ErrorMessage"] = "Order was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var driverExists = await _db.Drivers.AnyAsync(x => x.Id == DriverId);
        if (!driverExists)
        {
            TempData["ErrorMessage"] = "Driver was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var assignment = order.DriverAssignments
            .FirstOrDefault(x => x.IsPickup == isPickup && x.Status != DriverJobAction.Completed);

        if (assignment is null)
        {
            _db.DriverAssignments.Add(new DriverAssignment
            {
                OrderId = order.Id,
                DriverId = DriverId,
                IsPickup = isPickup,
                Status = DriverJobAction.Pending,
                AssignedAt = DateTime.UtcNow,
                Notes = isPickup ? "Pickup driver assigned from intake workflow" : "Delivery driver assigned from intake workflow"
            });
        }
        else
        {
            assignment.DriverId = DriverId;
            assignment.AssignedAt = DateTime.UtcNow;
            assignment.Status = DriverJobAction.Pending;
            assignment.Notes = isPickup ? "Pickup driver reassigned from intake workflow" : "Delivery driver reassigned from intake workflow";
        }

        if (isPickup && order.Status is OrderStatus.OrderPlaced or OrderStatus.PickupScheduled)
        {
            order.Status = OrderStatus.DriverAssigned;
        }

        if (!isPickup && order.Status == OrderStatus.ReadyForDelivery)
        {
            order.Status = OrderStatus.OutForDelivery;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = isPickup ? "Pickup driver assigned." : "Delivery driver assigned.";
        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostSetProcessingStageAsync()
    {
        if (!ProcessingStages.Contains(ProcessingStatus))
        {
            TempData["ErrorMessage"] = "Invalid laundry stage selected.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorName = User.Identity?.Name;
        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = OrderId,
            Status = ProcessingStatus,
            Note = $"Moved to {ProcessingStatus} by laundry staff"
        }, actorId, actorName);

        TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
            ? $"Order moved to {ProcessingStatus}."
            : "Could not update this order stage.";

        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostAssignServiceAsync()
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == OrderId);

        if (order is null)
        {
            TempData["ErrorMessage"] = "Order was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var service = await _db.LaundryServices
            .FirstOrDefaultAsync(x => x.Id == ServiceId && x.IsActive);

        if (service is null)
        {
            TempData["ErrorMessage"] = "Service was not found.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var item = order.Items.FirstOrDefault();
        if (item is null)
        {
            item = new OrderItem
            {
                OrderId = order.Id,
                Quantity = 1
            };
            _db.OrderItems.Add(item);
            order.Items.Add(item);
        }

        item.ServiceId = service.Id;
        item.UnitPrice = service.PricePerPiece;
        item.TotalPrice = item.UnitPrice * item.Quantity;

        order.SubTotal = order.Items.Sum(x => x.TotalPrice);
        order.TotalAmount = order.SubTotal + order.DeliveryFee - order.Discount;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Service assigned.";
        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostBulkAdvanceAsync()
    {
        if (SelectedOrderIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Select at least one order to update.";
            return RedirectToPage(new { Search, StatusFilter, PageNumber });
        }

        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorName = User.Identity?.Name;

        var orders = await _db.Orders
            .Where(x => SelectedOrderIds.Contains(x.Id))
            .ToListAsync();

        var updated = 0;
        var skipped = 0;

        foreach (var order in orders)
        {
            if (!LaundryStageFlow.TryGetValue(order.Status, out var expectedNext))
            {
                skipped++;
                continue;
            }

            var ok = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = order.Id,
                Status = expectedNext,
                Note = "Bulk updated by laundry staff"
            }, actorId, actorName);

            if (ok)
            {
                updated++;
            }
            else
            {
                skipped++;
            }
        }

        TempData["SuccessMessage"] = skipped > 0
            ? $"Updated {updated} orders. Skipped {skipped} due to invalid stage transition."
            : $"Updated {updated} orders.";

        return RedirectToPage(new { Search, StatusFilter, PageNumber });
    }

    private async Task LoadOptionsAsync()
    {
        await EnsureDriverProfilesAsync();

        DriverOptions = await _db.Drivers
            .Include(x => x.User)
            .OrderBy(x => x.User.FirstName)
            .ThenBy(x => x.User.LastName)
            .Select(x => new SelectListItem(
                $"{x.User.FirstName} {x.User.LastName}".Trim() + (x.IsAvailable ? " ✓" : string.Empty),
                x.Id.ToString()))
            .ToListAsync();

        ServiceOptions = await _db.LaundryServices
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new SelectListItem($"{x.Name} ({x.PricePerPiece:C})", x.Id.ToString()))
            .ToListAsync();

        ProcessingStageOptions = ProcessingStages
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
    }

    public DriverAssignment? GetPickupAssignment(Order order)
        => order.DriverAssignments
            .Where(x => x.IsPickup)
            .OrderByDescending(x => x.AssignedAt)
            .FirstOrDefault();

    public DriverAssignment? GetDeliveryAssignment(Order order)
        => order.DriverAssignments
            .Where(x => !x.IsPickup)
            .OrderByDescending(x => x.AssignedAt)
            .FirstOrDefault();

    private async Task EnsureDriverProfilesAsync()
    {
        var driverRoles = new[] { AppRoles.PickupDriver, AppRoles.DeliveryDriver };
        var existingDriverUserIds = await _db.Drivers.Select(d => d.UserId).ToListAsync();

        var driverUsers = await _db.Users
            .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
            .Join(_db.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, RoleName = r.Name })
            .Where(x => driverRoles.Contains(x.RoleName!) && !existingDriverUserIds.Contains(x.u.Id))
            .Select(x => x.u)
            .Distinct()
            .ToListAsync();

        if (driverUsers.Count > 0)
        {
            foreach (var u in driverUsers)
            {
                _db.Drivers.Add(new DriverEntity
                {
                    UserId      = u.Id,
                    Status      = DriverStatus.Offline,
                    IsAvailable = false,
                    CreatedAt   = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
