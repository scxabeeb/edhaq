using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Finance;

[Authorize(Roles = "Administrator,Manager,Cashier")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    // ── Summary metrics ──────────────────────────────────────────────
    public decimal TotalRevenue { get; private set; }
    public decimal TodaysRevenue { get; private set; }
    public decimal PendingPaymentsTotal { get; private set; }
    public decimal PaidTotal { get; private set; }
    public decimal RefundedTotal { get; private set; }
    public decimal TotalWalletBalance { get; private set; }
    public int PaidOrdersCount { get; private set; }
    public int PendingOrdersCount { get; private set; }
    public int FailedOrdersCount { get; private set; }
    public int TotalTransactions { get; private set; }

    // ── Breakdowns ───────────────────────────────────────────────────
    public Dictionary<string, decimal> RevenueByPaymentMethod { get; private set; } = new();
    public Dictionary<PaymentStatus, int> PaymentsByStatus { get; private set; } = new();

    // ── Recent data ──────────────────────────────────────────────────
    public List<Payment> RecentPayments { get; private set; } = new();

    // ── Filters ──────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;

        var paymentsQuery = _db.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Customer)
            .ThenInclude(c => c.User)
            .AsQueryable();

        if (From.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= From.Value.Date);
        if (To.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= To.Value.Date.AddDays(1).AddTicks(-1));

        var allPayments = await paymentsQuery.ToListAsync();
        var paidPayments = allPayments.Where(p => p.Status == PaymentStatus.Paid).ToList();
        var pendingPayments = allPayments.Where(p => p.Status == PaymentStatus.Pending).ToList();
        var failedPayments = allPayments.Where(p => p.Status == PaymentStatus.Failed).ToList();
        var refundedPayments = allPayments.Where(p => p.Status == PaymentStatus.Refunded).ToList();

        TotalRevenue = paidPayments.Sum(p => p.Amount);
        TodaysRevenue = paidPayments
            .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Date == today)
            .Sum(p => p.Amount);
        PendingPaymentsTotal = pendingPayments.Sum(p => p.Amount);
        PaidTotal = paidPayments.Sum(p => p.Amount);
        RefundedTotal = refundedPayments.Sum(p => p.Amount);
        FailedOrdersCount = failedPayments.Count;

        TotalWalletBalance = await _db.Wallets.SumAsync(w => w.Balance);

        RevenueByPaymentMethod = paidPayments
            .GroupBy(p => p.Method.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        PaymentsByStatus = allPayments
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        PaidOrdersCount = paidPayments.Count;
        PendingOrdersCount = pendingPayments.Count;
        TotalTransactions = allPayments.Count;

        RecentPayments = allPayments
            .OrderByDescending(p => p.CreatedAt)
            .Take(15)
            .ToList();
    }

    // ── Post handlers for cashier payment updates ────────────────────
    [BindProperty]
    public int PaymentId { get; set; }

    [BindProperty]
    public PaymentStatus NewStatus { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        var actorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == PaymentId);

        if (payment is null)
        {
            TempData["ErrorMessage"] = "Payment not found.";
            return RedirectToPage(new { From, To });
        }

        payment.Status = NewStatus;
        if (NewStatus == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;
        }
        else if (NewStatus == PaymentStatus.Refunded)
        {
            payment.Order.PaymentStatus = PaymentStatus.Refunded;
        }
        else if (NewStatus == PaymentStatus.Failed)
        {
            payment.Order.PaymentStatus = PaymentStatus.Failed;
        }

        payment.Order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment #{payment.Id} for order {payment.Order.OrderNumber} updated to {NewStatus}.";
        return RedirectToPage(new { From, To });
    }

    public async Task<IActionResult> OnPostClearPaymentAsync(int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null)
        {
            TempData["ErrorMessage"] = "Payment not found.";
            return RedirectToPage(new { From, To });
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.Order.PaymentStatus = PaymentStatus.Paid;
        payment.Order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment cleared for order {payment.Order.OrderNumber}.";
        return RedirectToPage(new { From, To });
    }
}
