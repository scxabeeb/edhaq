using eDhaq.Common.DTOs;
using eDhaq.Models.Enums;

namespace eDhaq.Web.Areas.Api.Dtos;

public class CustomerDashboardDto
{
    public string CustomerName { get; set; } = string.Empty;
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal WalletBalance { get; set; }
    public List<OrderSummaryDto> RecentOrders { get; set; } = new();
    public List<NotificationDto> UnreadNotifications { get; set; } = new();
}

public class DriverDashboardDto
{
    public string DriverName { get; set; } = string.Empty;
    public int ActiveAssignments { get; set; }
    public int ActivePickupAssignments { get; set; }
    public int ActiveDeliveryAssignments { get; set; }
    public decimal TodayEarnings { get; set; }
    public decimal TotalEarnings { get; set; }
    public double Rating { get; set; }
    public bool IsAvailable { get; set; }
    public DriverStatus Status { get; set; }
    public List<OrderSummaryDto> CurrentTasks { get; set; } = new();
    public List<NotificationDto> UnreadNotifications { get; set; } = new();
}

public class AdminDashboardDto
{
    public string AdminName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalDrivers { get; set; }
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();
    public List<OrderSummaryDto> RecentOrders { get; set; } = new();
}
