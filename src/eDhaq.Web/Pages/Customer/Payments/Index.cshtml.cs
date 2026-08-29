using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer.Payments;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentService _paymentService;

    public IndexModel(AppDbContext db, IPaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    public List<Payment> Payments { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? OrderId { get; set; }

    [BindProperty]
    public int PaymentId { get; set; }

    [BindProperty]
    public eDhaq.Models.Enums.PaymentMethod Method { get; set; }

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

        var all = await _paymentService.GetPaymentsAsync();
        Payments = all.Where(x => x.Order.CustomerId == customer.Id).ToList();

        if (OrderId.HasValue)
        {
            await _paymentService.EnsurePaymentForOrderAsync(OrderId.Value);
            all = await _paymentService.GetPaymentsAsync();
            Payments = all.Where(x => x.Order.CustomerId == customer.Id).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPayAsync()
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

        var payment = await _db.Payments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == PaymentId && x.Order.CustomerId == customer.Id);

        if (payment is null)
        {
            TempData["ErrorMessage"] = "Payment record not found.";
            return RedirectToPage();
        }

        if (payment.Status == eDhaq.Models.Enums.PaymentStatus.Paid)
        {
            TempData["SuccessMessage"] = "Payment is already completed.";
            return RedirectToPage(new { orderId = payment.OrderId });
        }

        await _paymentService.UpdatePaymentMethodAsync(payment.Id, Method);
        await _paymentService.UpdatePaymentStatusAsync(payment.Id, eDhaq.Models.Enums.PaymentStatus.Paid, userId, "Customer self-payment");

        TempData["SuccessMessage"] = $"Payment for order {payment.Order.OrderNumber} completed successfully.";
        return RedirectToPage(new { orderId = payment.OrderId });
    }
}
