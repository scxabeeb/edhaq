using eDhaq.Common.DTOs;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int customerId, CreateOrderDto dto, string? actorUserId = null);
    Task<Order?> GetOrderDetailsAsync(int orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<OrderSummaryDto>> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10);
    Task<int> GetCustomerOrderCountAsync(int customerId);
    Task<bool> UpdateStatusAsync(UpdateOrderStatusDto dto, string? actorUserId = null, string? actorName = null);
    Task<string> GenerateNextOrderNumberAsync();
    Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync();
}
