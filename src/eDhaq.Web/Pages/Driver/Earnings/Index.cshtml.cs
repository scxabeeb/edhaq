using eDhaq.Data;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Driver.Earnings;

[Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public decimal TotalEarnings { get; private set; }
    public int CompletedAssignments { get; private set; }
    public List<(string OrderNumber, DateTime Date, decimal Amount)> Latest { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (driver is null)
        {
            return;
        }

        var completed = await _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id && x.Status == DriverJobAction.Completed)
            .Include(x => x.Order)
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync();

        CompletedAssignments = completed.Count;
        Latest = completed.Take(20).Select(x =>
        {
            var amount = Math.Round(x.Order.TotalAmount * 0.15m, 2);
            return (x.Order.OrderNumber, x.CompletedAt ?? x.AssignedAt, amount);
        }).ToList();

        TotalEarnings = Latest.Sum(x => x.Amount);
    }
}
