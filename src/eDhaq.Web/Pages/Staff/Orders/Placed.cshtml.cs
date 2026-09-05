using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Staff.Orders;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class PlacedModel : PageModel
{
    private readonly AppDbContext _db;

    public PlacedModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Order> Orders { get; private set; } = [];
    public int PlacedCount { get; private set; }
    public int ScheduledCount { get; private set; }
    public int DriverAssignedCount { get; private set; }
    public int PickedUpCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public OrderStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var stages = new[]
        {
            OrderStatus.OrderPlaced,
            OrderStatus.PickupScheduled,
            OrderStatus.DriverAssigned,
            OrderStatus.DriverOnTheWay,
            OrderStatus.ClothesPickedUp
        };

        var counts = await _db.Orders
            .Where(x => stages.Contains(x.Status))
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        PlacedCount = counts.GetValueOrDefault(OrderStatus.OrderPlaced);
        ScheduledCount = counts.GetValueOrDefault(OrderStatus.PickupScheduled)
            + counts.GetValueOrDefault(OrderStatus.DriverAssigned)
            + counts.GetValueOrDefault(OrderStatus.DriverOnTheWay);
        DriverAssignedCount = counts.GetValueOrDefault(OrderStatus.DriverAssigned)
            + counts.GetValueOrDefault(OrderStatus.DriverOnTheWay);
        PickedUpCount = counts.GetValueOrDefault(OrderStatus.ClothesPickedUp);

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

        Orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}