using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Cashier.Payments;

[Authorize(Roles = "Administrator,Cashier")]
public class IndexModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public IndexModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public List<Payment> Payments { get; private set; } = [];

    [BindProperty]
    public int PaymentId { get; set; }

    [BindProperty]
    public PaymentMethod Method { get; set; }

    [BindProperty]
    public PaymentStatus Status { get; set; }

    public async Task OnGetAsync()
    {
        Payments = await _paymentService.GetPaymentsAsync(PaymentStatus.Pending);
    }

    public async Task<IActionResult> OnPostProcessAsync()
    {
        await _paymentService.UpdatePaymentMethodAsync(PaymentId, Method);
        var actorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _paymentService.UpdatePaymentStatusAsync(PaymentId, Status, actorUserId, "Updated by cashier");
        TempData["SuccessMessage"] = "Payment processed.";
        return RedirectToPage();
    }
}
