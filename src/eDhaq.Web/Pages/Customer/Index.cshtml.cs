using eDhaq.Common.DTOs;
using eDhaq.Common.ViewModels;
using eDhaq.Data;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _db;

    public IndexModel(
        ICustomerRepository customerRepository,
        IOrderService orderService,
        INotificationService notificationService,
        AppDbContext db)
    {
        _customerRepository = customerRepository;
        _orderService = orderService;
        _notificationService = notificationService;
        _db = db;
    }

    public CustomerDashboardViewModel Dashboard { get; private set; } = new();
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
            await LoadAdminDashboardAsync();
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

    private async Task LoadAdminDashboardAsync()
    {
        var orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                EstimatedCompletionAt = o.EstimatedCompletionAt
            })
            .ToListAsync();

        var unread = await _notificationService.GetAllAsync();

        Dashboard = new CustomerDashboardViewModel
        {
            CustomerName = "All Customers",
            ActiveOrders = await _db.Orders.CountAsync(o => o.Status != Models.Enums.OrderStatus.Completed && o.Status != Models.Enums.OrderStatus.Cancelled),
            CompletedOrders = await _db.Orders.CountAsync(o => o.Status == Models.Enums.OrderStatus.Completed),
            WalletBalance = await _db.Customers.SumAsync(c => (decimal?)c.WalletBalance) ?? 0,
            RecentOrders = orders,
            Notifications = unread
        };
    }
}
