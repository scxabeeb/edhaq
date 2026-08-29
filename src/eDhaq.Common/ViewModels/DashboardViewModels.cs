using eDhaq.Models.Enums;

namespace eDhaq.Common.ViewModels;

public class AdminDashboardViewModel
{
    public decimal TodaysRevenue { get; set; }
    public int TodaysOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalDrivers { get; set; }
    public int TotalStaff { get; set; }

    public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = [];
    public List<RevenuePointViewModel> WeeklyRevenue { get; set; } = [];
    public List<RevenuePointViewModel> MonthlyRevenue { get; set; } = [];
}

public class RevenuePointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
