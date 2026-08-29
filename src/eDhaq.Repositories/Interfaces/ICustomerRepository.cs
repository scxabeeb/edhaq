using eDhaq.Models.Entities;

namespace eDhaq.Repositories.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(string userId);
    Task<Customer?> GetWithAddressesAsync(int id);
    Task<IEnumerable<Customer>> GetTopCustomersAsync(int count = 10);
}
