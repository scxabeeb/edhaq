using eDhaq.Common.Constants;
using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using DriverEntity = eDhaq.Models.Entities.Driver;

namespace eDhaq.Web.Pages.Admin.Orders;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
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

    public IndexModel(AppDbContext db, IOrderService orderService)
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

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DatePreset { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "created_desc";

    [BindProperty]
    public int OrderId { get; set; }

    [BindProperty]
    public OrderStatus NewStatus { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    [BindProperty]
    public DateTime PickupScheduledAt { get; set; }

    [BindProperty]
    public int DriverId { get; set; }

    [BindProperty]
    public int ServiceId { get; set; }

    [BindProperty]
    public OrderStatus ProcessingStatus { get; set; }

    private IQueryable<Order> BuildFilteredQuery()
    {
        var effectiveDateFrom = DateFrom;
        var effectiveDateTo = DateTo;

        if (!string.IsNullOrWhiteSpace(DatePreset))
        {
            var today = DateTime.UtcNow.Date;
            switch (DatePreset)
            {
                case "today":
                    effectiveDateFrom = today;
                    effectiveDateTo = today;
                    break;
                case "last7":
                    effectiveDateFrom = today.AddDays(-6);
                    effectiveDateTo = today;
                    break;
                case "month":
                    effectiveDateFrom = new DateTime(today.Year, today.Month, 1);
                    effectiveDateTo = today;
                    break;
            }
        }

        var query = _db.Orders
            .Include(x => x.Customer).ThenInclude(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(s)
                                     || x.Customer.User.FirstName.Contains(s)
                                     || x.Customer.User.LastName.Contains(s)
                                     || x.Customer.User.Email!.Contains(s));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == StatusFilter.Value);
        }

        if (effectiveDateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= effectiveDateFrom.Value.Date);
        }

        if (effectiveDateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= effectiveDateTo.Value.Date.AddDays(1).AddTicks(-1));
        }

        return SortBy switch
        {
            "created_asc" => query.OrderBy(x => x.CreatedAt),
            "total_desc" => query.OrderByDescending(x => x.TotalAmount),
            "total_asc" => query.OrderBy(x => x.TotalAmount),
            "status_asc" => query.OrderBy(x => x.Status),
            "status_desc" => query.OrderByDescending(x => x.Status),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
    }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        var query = BuildFilteredQuery();

        TotalCount = await query.CountAsync();

        Orders = await query
            .Include(x => x.Items).ThenInclude(i => i.Service)
            .Include(x => x.DriverAssignments).ThenInclude(a => a.Driver).ThenInclude(d => d.User)
            .Include(x => x.Trackings)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var rows = await BuildFilteredQuery()
            .Take(5000)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderNumber,CustomerName,CustomerEmail,Status,TotalAmount,CreatedAt");

        foreach (var order in rows)
        {
            var customerName = $"{order.Customer.User.FirstName} {order.Customer.User.LastName}";
            var email = order.Customer.User.Email ?? string.Empty;
            sb.AppendLine(string.Join(',',
                EscapeCsv(order.OrderNumber),
                EscapeCsv(customerName),
                EscapeCsv(email),
                EscapeCsv(order.Status.ToString()),
                order.TotalAmount.ToString("0.00"),
                order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"orders-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorName = User.Identity?.Name;

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = OrderId,
            Status = NewStatus,
            Note = Note
        }, actorId, actorName);

        TempData["SuccessMessage"] = "Order status updated.";
        return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
    }

    public async Task<IActionResult> OnPostSchedulePickupAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == OrderId);
        if (order is null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        order.PickupScheduledAt = PickupScheduledAt;
        if (order.Status == OrderStatus.OrderPlaced)
        {
            order.Status = OrderStatus.PickupScheduled;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Pickup scheduled for {order.OrderNumber}.";
        return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
    }

    public async Task<IActionResult> OnPostAssignDriverAsync()
    {
        var order = await _db.Orders
            .Include(x => x.DriverAssignments)
            .FirstOrDefaultAsync(x => x.Id == OrderId);

        if (order is null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        var driverExists = await _db.Drivers.AnyAsync(x => x.Id == DriverId);
        if (!driverExists)
        {
            TempData["ErrorMessage"] = "Driver not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        var isPickupAssignment = order.Status != OrderStatus.ReadyForDelivery;

        var assignment = order.DriverAssignments
            .FirstOrDefault(x => x.IsPickup == isPickupAssignment && x.Status != DriverJobAction.Completed);

        if (assignment is null)
        {
            _db.DriverAssignments.Add(new DriverAssignment
            {
                OrderId = order.Id,
                DriverId = DriverId,
                IsPickup = isPickupAssignment,
                Status = DriverJobAction.Pending,
                AssignedAt = DateTime.UtcNow,
                Notes = "Assigned from admin/staff intake workflow"
            });
        }
        else
        {
            assignment.DriverId = DriverId;
            assignment.AssignedAt = DateTime.UtcNow;
            assignment.Status = DriverJobAction.Pending;
        }

        if (isPickupAssignment)
        {
            order.Status = OrderStatus.DriverAssigned;
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Driver assigned for {order.OrderNumber}.";
        return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
    }

    public async Task<IActionResult> OnPostSetProcessingStageAsync()
    {
        if (!ProcessingStages.Contains(ProcessingStatus))
        {
            TempData["ErrorMessage"] = "Invalid laundry stage selected.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == OrderId);
        if (order is null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var actorName = User.Identity?.Name;
        var note = $"Order moved to {ProcessingStatus} by admin.";

        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = OrderId,
            Status = ProcessingStatus,
            Note = note
        }, actorId, actorName);

        TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
            ? $"Laundry stage updated to {ProcessingStatus}."
            : "Could not update the laundry stage for this order.";

        return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
    }

    public async Task<IActionResult> OnPostAssignServiceAsync()
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == OrderId);

        if (order is null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
        }

        var service = await _db.LaundryServices.FirstOrDefaultAsync(x => x.Id == ServiceId && x.IsActive);
        if (service is null)
        {
            TempData["ErrorMessage"] = "Service not found.";
            return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
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

        TempData["SuccessMessage"] = $"Service assigned for {order.OrderNumber}.";
        return RedirectToPage(new { Search, StatusFilter, DateFrom, DateTo, DatePreset, SortBy, PageNumber });
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

    public DriverAssignment? GetActiveAssignment(Order order)
        => IsDeliveryWorkflow(order.Status) ? GetDeliveryAssignment(order) : GetPickupAssignment(order);

    public string GetDriverSlotLabel(Order order)
        => IsDeliveryWorkflow(order.Status) ? "Delivery Driver" : "Pickup Driver";

    public IEnumerable<OrderTracking> GetRecentActivity(Order order)
        => order.Trackings
            .OrderByDescending(x => x.CreatedAt)
            .Take(4);

    public static bool IsDeliveryWorkflow(OrderStatus status)
        => status is OrderStatus.ReadyForDelivery or OrderStatus.OutForDelivery or OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.CustomerConfirmed;

    /// <summary>
    /// Ensures every user in a driver role has a Driver record.
    /// Handles users created before the auto-create fix.
    /// </summary>
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
