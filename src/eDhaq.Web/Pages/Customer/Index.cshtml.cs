using eDhaq.Common.DTOs;
using eDhaq.Common.ViewModels;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Customer;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;

    public IndexModel(
        ICustomerRepository customerRepository,
        IOrderService orderService,
        INotificationService notificationService)
    {
        _customerRepository = customerRepository;
        _orderService = orderService;
        _notificationService = notificationService;
    }

    public CustomerDashboardViewModel Dashboard { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var customer = await _customerRepository.GetByUserIdAsync(userId);
        if (customer is null)
        {
            return;
        }

        var orders = await _orderService.GetCustomerOrdersAsync(customer.Id, 1, 5);
        Dashboard = new CustomerDashboardViewModel
        {
            CustomerName = $"{customer.User.FirstName} {customer.User.LastName}",
            ActiveOrders = customer.Orders.Count(x => x.Status != Models.Enums.OrderStatus.Completed && x.Status != Models.Enums.OrderStatus.Cancelled),
            CompletedOrders = customer.Orders.Count(x => x.Status == Models.Enums.OrderStatus.Completed),
            WalletBalance = customer.WalletBalance,
            RecentOrders = orders.ToList(),
            Notifications = await _notificationService.GetUnreadAsync(userId)
        };
    }
}
