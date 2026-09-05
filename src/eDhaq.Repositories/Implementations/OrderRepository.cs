using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Repositories.Implementations;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext db) : base(db) { }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        => await _db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Items).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<Order?> GetWithDetailsAsync(int id)
        => await _db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Items).ThenInclude(i => i.Service)
            .Include(o => o.Trackings)
            .Include(o => o.DriverAssignments).ThenInclude(da => da.Driver).ThenInclude(d => d.User)
            .Include(o => o.PickupAddress).ThenInclude(a => a.City)
            .Include(o => o.DeliveryAddress).ThenInclude(a => a.City)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order>> GetByCustomerAsync(int customerId, int page = 1, int pageSize = 10)
        => await _db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items).ThenInclude(i => i.Service).ThenInclude(s => s.Category)
            .ToListAsync();

    public async Task<int> GetCustomerOrderCountAsync(int customerId)
        => await _db.Orders.CountAsync(o => o.CustomerId == customerId);

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        => await _db.Orders
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetTodaysOrdersAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Orders
            .Where(o => o.CreatedAt >= today)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetNextSequenceNumberAsync()
        => await _db.Orders.CountAsync() + 1;

    public async Task<decimal> GetTodaysRevenueAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Orders
            .Where(o => o.CreatedAt >= today && o.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
    }

        public async Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync()
    {
        return await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(int page = 1, int pageSize = 20)
        => await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .ToListAsync();

    public async Task<int> GetAllOrderCountAsync()
        => await _db.Orders.CountAsync();
}
