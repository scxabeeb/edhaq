using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Repositories.Implementations;

public class DriverRepository : Repository<Driver>, IDriverRepository
{
    public DriverRepository(AppDbContext db) : base(db) { }

    public async Task<Driver?> GetByUserIdAsync(string userId)
        => await _db.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

    public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        => await _db.Drivers
            .Where(d => d.IsAvailable && d.Status == DriverStatus.Available)
            .Include(d => d.User)
            .ToListAsync();

    public async Task<Driver?> GetWithAssignmentsAsync(int id)
        => await _db.Drivers
            .Include(d => d.User)
            .Include(d => d.Assignments)
                .ThenInclude(a => a.Order)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task UpdateLocationAsync(int driverId, decimal latitude, decimal longitude)
    {
        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
        if (driver is null)
        {
            return;
        }

        driver.CurrentLatitude = latitude;
        driver.CurrentLongitude = longitude;
        driver.LastLocationUpdate = DateTime.UtcNow;
    }
}
