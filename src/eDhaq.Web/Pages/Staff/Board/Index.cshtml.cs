using eDhaq.Data;
using eDhaq.Common.DTOs;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Staff.Board;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;

    public IndexModel(AppDbContext db, IOrderService orderService)
    {
        _db = db;
        _orderService = orderService;
    }

    public Dictionary<OrderStatus, List<Order>> Board { get; private set; } = [];

    [BindProperty]
    public int OrderId { get; set; }

    [BindProperty]
    public OrderStatus CurrentStatus { get; set; }

    public async Task OnGetAsync()
    {
        var statuses = new[]
        {
            OrderStatus.LaundryReceived,
            OrderStatus.Washing,
            OrderStatus.Drying,
            OrderStatus.Ironing,
            OrderStatus.Packaging,
            OrderStatus.ReadyForDelivery
        };

        var orders = await _db.Orders
            .Where(x => statuses.Contains(x.Status))
            .Include(x => x.Customer)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        Board = statuses.ToDictionary(s => s, s => orders.Where(x => x.Status == s).Take(20).ToList());
    }

    public async Task<IActionResult> OnPostMoveNextAsync()
    {
        var next = GetNextStatus(CurrentStatus);
        if (!next.HasValue)
        {
            return RedirectToPage();
        }

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = OrderId,
            Status = next.Value,
            Note = "Advanced from staff board"
        }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);

        TempData["SuccessMessage"] = "Order moved to next stage.";
        return RedirectToPage();
    }

    private static OrderStatus? GetNextStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.LaundryReceived => OrderStatus.Washing,
            OrderStatus.Washing => OrderStatus.Drying,
            OrderStatus.Drying => OrderStatus.Ironing,
            OrderStatus.Ironing => OrderStatus.Packaging,
            OrderStatus.Packaging => OrderStatus.ReadyForDelivery,
            OrderStatus.ReadyForDelivery => OrderStatus.DriverAssigned,
            _ => null
        };
    }
}
