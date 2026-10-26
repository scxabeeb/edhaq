using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer.Payments;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Payment> Payments { get; private set; } = [];
    public List<eDhaq.Models.Entities.Order> PendingOrders { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? OrderId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (customer is null)
        {
            TempData["ErrorMessage"] = "Customer profile not found.";
            return RedirectToPage("/Customer/Index");
        }

        // Read-only: payments are collected by the delivery driver, never by the customer.
        Payments = await _db.Payments
            .Include(x => x.Order)
            .Where(x => x.Order.CustomerId == customer.Id)
            .Where(x => !OrderId.HasValue || x.OrderId == OrderId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        PendingOrders = await _db.Orders
            .Where(x => x.CustomerId == customer.Id
                        && x.PaymentStatus != eDhaq.Models.Enums.PaymentStatus.Paid
                        && x.Status != eDhaq.Models.Enums.OrderStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Page();
    }
}
