using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Repositories.Implementations;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext db) : base(db) { }

    public async Task<Customer?> GetByUserIdAsync(string userId)
        => await _db.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Customer?> GetWithAddressesAsync(int id)
        => await _db.Customers
            .Include(c => c.User)
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Customer>> GetTopCustomersAsync(int count = 10)
        => await _db.Customers
            .OrderByDescending(c => c.TotalSpent)
            .Take(count)
            .Include(c => c.User)
            .ToListAsync();
}
