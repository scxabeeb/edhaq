using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<Order?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Order>> GetByCustomerAsync(int customerId, int page = 1, int pageSize = 10);
    Task<int> GetCustomerOrderCountAsync(int customerId);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetTodaysOrdersAsync();
    Task<int> GetNextSequenceNumberAsync();
        Task<decimal> GetTodaysRevenueAsync();
    Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync();
    Task<IEnumerable<Order>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<int> GetAllOrderCountAsync();
}
