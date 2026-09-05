using eDhaq.Common.DTOs;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Customer.Orders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderService _orderService;

    public IndexModel(ICustomerRepository customerRepository, IOrderService orderService)
    {
        _customerRepository = customerRepository;
        _orderService = orderService;
    }

    public List<OrderSummaryDto> Orders { get; private set; } = [];
    public bool IsAdmin => User?.IsInRole("Administrator") ?? false;

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        if (IsAdmin)
        {
            // Administrators see all orders across all customers.
            Orders = (await _orderService.GetAllOrdersAsync(1, 25)).ToList();
            return;
        }

        var customer = await _customerRepository.GetByUserIdAsync(userId);
        if (customer is null)
        {
            return;
        }

        Orders = (await _orderService.GetCustomerOrdersAsync(customer.Id, 1, 25)).ToList();
    }
}
