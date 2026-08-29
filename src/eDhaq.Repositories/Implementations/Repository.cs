using eDhaq.Data;
using eDhaq.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eDhaq.Repositories.Implementations;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.Where(predicate).ToListAsync();

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await _set.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities) => await _set.AddRangeAsync(entities);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate is null ? await _set.CountAsync() : await _set.CountAsync(predicate);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _set.AnyAsync(predicate);
}
