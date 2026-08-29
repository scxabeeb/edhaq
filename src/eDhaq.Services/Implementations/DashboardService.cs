using eDhaq.Common.ViewModels;
using eDhaq.Data;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;

        var model = new AdminDashboardViewModel
        {
            TodaysRevenue = await _db.Orders
                .Where(x => x.CreatedAt >= today && x.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0,
            TodaysOrders = await _db.Orders.CountAsync(x => x.CreatedAt >= today),
            PendingOrders = await _db.Orders.CountAsync(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled),
            CompletedOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Completed),
            CancelledOrders = await _db.Orders.CountAsync(x => x.Status == OrderStatus.Cancelled),
            TotalCustomers = await _db.Customers.CountAsync(),
            TotalDrivers = await _db.Drivers.CountAsync(),
            TotalStaff = await _db.Employees.CountAsync()
        };

        model.OrdersByStatus = await _db.Orders
            .GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var weeklyRevenueData = await _db.Orders
            .Where(x => x.CreatedAt >= today.AddDays(-6) && x.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(x => new
            {
                Date = x.Key,
                Value = x.Sum(i => i.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        model.WeeklyRevenue = weeklyRevenueData
            .Select(x => new RevenuePointViewModel
            {
                Label = x.Date.ToString("dd MMM"),
                Value = x.Value
            })
            .ToList();

        var monthlyRevenueData = await _db.Orders
            .Where(x => x.CreatedAt >= new DateTime(today.Year, today.Month, 1).AddMonths(-5) && x.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .Select(x => new
            {
                x.Key.Year,
                x.Key.Month,
                Value = x.Sum(i => i.TotalAmount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        model.MonthlyRevenue = monthlyRevenueData
            .Select(x => new RevenuePointViewModel
            {
                Label = $"{x.Year}-{x.Month:D2}",
                Value = x.Value
            })
            .ToList();

        return model;
    }
}
