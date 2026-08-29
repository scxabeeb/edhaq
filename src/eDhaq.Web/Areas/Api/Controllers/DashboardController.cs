using eDhaq.Common.Constants;
using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class DashboardController : ApiControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _db;

    public DashboardController(
        ICustomerRepository customerRepository,
        IDriverRepository driverRepository,
        IOrderService orderService,
        INotificationService notificationService,
        AppDbContext db)
    {
        _customerRepository = customerRepository;
        _driverRepository = driverRepository;
        _orderService = orderService;
        _notificationService = notificationService;
        _db = db;
    }

    [HttpGet("customer")]
    public async Task<ActionResult<CustomerDashboardDto>> GetCustomerDashboard()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var customer = await _customerRepository.GetByUserIdAsync(userId);
        if (customer is null)
        {
            return NotFound(new ProblemDetails { Title = "Customer profile not found." });
        }

        var recentOrders = (await _orderService.GetCustomerOrdersAsync(customer.Id, 1, 5)).ToList();
        var unread = await _notificationService.GetUnreadAsync(userId);

        var model = new CustomerDashboardDto
        {
            CustomerName = $"{customer.User.FirstName} {customer.User.LastName}".Trim(),
            ActiveOrders = customer.Orders.Count(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled),
            CompletedOrders = customer.Orders.Count(x => x.Status == OrderStatus.Completed),
            WalletBalance = customer.WalletBalance,
            RecentOrders = recentOrders,
            UnreadNotifications = unread.Select(ToDto).ToList()
        };

        return Ok(model);
    }

    [HttpGet("driver")]
    public async Task<ActionResult<DriverDashboardDto>> GetDriverDashboard()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var driver = await _driverRepository.GetByUserIdAsync(userId);
        if (driver is null && (User.IsInRole("PickupDriver") || User.IsInRole("DeliveryDriver") || User.IsInRole("Administrator")))
        {
            // Auto-create a Driver profile if the user has a driver/admin role
            // but no Driver entity exists yet. This ensures the driver portal
            // shows data instead of returning 404.
            driver = new Driver
            {
                UserId = userId,
                Status = DriverStatus.Offline,
                CreatedAt = DateTime.UtcNow
            };
            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync();
        }

        if (driver is null)
        {
            return NotFound(new ProblemDetails { Title = "Driver profile not found." });
        }

        var activeAssignments = await _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id && x.Status != DriverJobAction.Completed)
            .Include(x => x.Order)
            .Include(x => x.Order.Customer)
            .ThenInclude(c => c.User)
            .ToListAsync();

        var activePickup = activeAssignments.Count(x => x.IsPickup);
        var activeDelivery = activeAssignments.Count(x => !x.IsPickup);

        var today = DateTime.UtcNow.Date;
        var todayEarnings = await _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id && x.Status == DriverJobAction.Completed && x.CompletedAt >= today)
            .SumAsync(x => (decimal?)x.Order.TotalAmount) ?? 0;

        var unread = await _notificationService.GetUnreadAsync(userId);

        var model = new DriverDashboardDto
        {
            DriverName = $"{driver.User.FirstName} {driver.User.LastName}".Trim(),
            ActiveAssignments = activeAssignments.Count,
            ActivePickupAssignments = activePickup,
            ActiveDeliveryAssignments = activeDelivery,
            TodayEarnings = todayEarnings,
            TotalEarnings = driver.TotalEarnings,
            Rating = driver.Rating,
            IsAvailable = driver.IsAvailable,
            Status = driver.Status,
            CurrentTasks = activeAssignments.Select(a => new OrderSummaryDto
            {
                Id = a.Order.Id,
                OrderNumber = a.Order.OrderNumber,
                Status = a.Order.Status,
                TotalAmount = a.Order.TotalAmount,
                CreatedAt = a.Order.CreatedAt,
                EstimatedCompletionAt = a.Order.EstimatedCompletionAt
            }).ToList(),
            UnreadNotifications = unread.Select(ToDto).ToList()
        };

        return Ok(model);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        var statusCounts = await _orderService.GetOrderStatusCountsAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
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

        var totalOrders = await _db.Orders.CountAsync();
        var activeOrders = await _db.Orders.CountAsync(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled);
        var completedOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Completed);
        var totalRevenue = await _db.Orders.Where(x => x.Status == OrderStatus.Completed).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
        var totalCustomers = await _db.Customers.CountAsync();
        var totalDrivers = await _db.Drivers.CountAsync();

        var model = new AdminDashboardDto
        {
            AdminName = $"{user?.FirstName} {user?.LastName}".Trim(),
            TotalOrders = totalOrders,
            ActiveOrders = activeOrders,
            CompletedOrders = completedOrders,
            TotalRevenue = totalRevenue,
            TotalCustomers = totalCustomers,
            TotalDrivers = totalDrivers,
            StatusCounts = statusCounts,
            RecentOrders = recentOrders
        };

        return Ok(model);
    }

    private static NotificationDto ToDto(eDhaq.Models.Entities.Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            ActionUrl = n.ActionUrl,
            OrderId = n.OrderId,
            CreatedAt = n.CreatedAt,
            ReadAt = n.ReadAt
        };
    }
}
