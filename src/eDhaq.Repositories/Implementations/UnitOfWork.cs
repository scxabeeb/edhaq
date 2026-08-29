using eDhaq.Data;
using eDhaq.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace eDhaq.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Orders = new OrderRepository(db);
        Customers = new CustomerRepository(db);
        Drivers = new DriverRepository(db);
    }

    public IOrderRepository Orders { get; }
    public ICustomerRepository Customers { get; }
    public IDriverRepository Drivers { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await _db.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        await _db.DisposeAsync();
    }
}
