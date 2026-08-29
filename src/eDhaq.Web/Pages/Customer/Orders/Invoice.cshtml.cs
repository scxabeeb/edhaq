using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Customer.Orders;

[Authorize]
public class InvoiceModel : PageModel
{
    private readonly IOrderService _orderService;

    public InvoiceModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public eDhaq.Models.Entities.Order? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        Order = await _orderService.GetOrderDetailsAsync(orderId);
        if (Order is null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        return Page();
    }
}
