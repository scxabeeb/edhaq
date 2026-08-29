using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Repositories.Interfaces;

public interface IDriverRepository : IRepository<Driver>
{
    Task<Driver?> GetByUserIdAsync(string userId);
    Task<IEnumerable<Driver>> GetAvailableDriversAsync();
    Task<Driver?> GetWithAssignmentsAsync(int id);
    Task UpdateLocationAsync(int driverId, decimal latitude, decimal longitude);
}
