using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Services.Implementations;

public class CustomerProfileService : ICustomerProfileService
{
    private readonly AppDbContext _db;

    public CustomerProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Customer?> EnsureCustomerAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var customer = await _db.Customers
            .Include(c => c.User)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer is not null) return customer;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        customer = new Customer
        {
            UserId = userId,
            ReferralCode = $"EDQ{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        _db.Wallets.Add(new Wallet
        {
            CustomerId = customer.Id,
            Balance = 0,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return await _db.Customers
            .Include(c => c.User)
            .Include(c => c.Orders)
            .FirstAsync(c => c.UserId == userId);
    }
}