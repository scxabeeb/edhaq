using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Admin.Payments;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public IndexModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public List<Payment> Payments { get; private set; } = [];
    public int TotalCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 20;

    [BindProperty(SupportsGet = true)]
    public PaymentStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty]
    public int PaymentId { get; set; }

    [BindProperty]
    public PaymentStatus NewStatus { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    public async Task OnGetAsync()
    {
        var all = await _paymentService.GetPaymentsAsync(Status, From, To);
        TotalCount = all.Count;
        Payments = all.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        var actorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _paymentService.UpdatePaymentStatusAsync(PaymentId, NewStatus, actorUserId, Note);
        TempData["SuccessMessage"] = "Payment status updated.";
        return RedirectToPage(new { Status, From, To });
    }
}
