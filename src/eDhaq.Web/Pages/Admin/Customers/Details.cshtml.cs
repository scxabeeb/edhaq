using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = eDhaq.Models.Entities.Customer;

namespace eDhaq.Web.Pages.Admin.Customers;

[Authorize(Roles = "Administrator,Manager")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailsModel(AppDbContext db)
    {
        _db = db;
    }

    public CustomerEntity? Customer { get; private set; }
    public List<Order> Orders { get; private set; } = [];

    public int TotalOrders { get; private set; }
    public int CompletedOrders { get; private set; }
    public int CancelledOrders { get; private set; }
    public int PendingOrders { get; private set; }
    public decimal TotalSpent { get; private set; }
    public decimal AvgOrderValue { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Customer = await _db.Customers
            .Include(c => c.User)
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .Include(c => c.Addresses).ThenInclude(a => a.Village)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Customer is null)
        {
            TempData["ErrorMessage"] = "Customer not found.";
            return RedirectToPage("./Index");
        }

        Orders = await _db.Orders
            .Where(o => o.CustomerId == id)
            .Include(o => o.Items).ThenInclude(i => i.Service)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        TotalOrders = Orders.Count;
        CompletedOrders = Orders.Count(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.CustomerConfirmed);
        CancelledOrders = Orders.Count(o => o.Status == OrderStatus.Cancelled);
        PendingOrders = TotalOrders - CompletedOrders - CancelledOrders;
        TotalSpent = Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
        AvgOrderValue = CompletedOrders + PendingOrders > 0 && TotalOrders - CancelledOrders > 0
            ? TotalSpent / Math.Max(1, TotalOrders - CancelledOrders)
            : 0;

        return Page();
    }
}
