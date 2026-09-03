using eDhaq.Common.Constants;
using eDhaq.Data;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverEntity = eDhaq.Models.Entities.Driver;

namespace eDhaq.Web.Pages.Admin.Drivers;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<DriverRow> Drivers { get; private set; } = [];

    public int TotalDrivers { get; private set; }
    public int AvailableDrivers { get; private set; }
    public int TotalDeliveries { get; private set; }
    public decimal TotalEarnings { get; private set; }

    public string SearchTerm { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        SearchTerm = (Request.Query["q"].ToString() ?? string.Empty).Trim();

        await EnsureDriverProfilesAsync();

        var query = _db.Drivers
            .Include(d => d.User)
            .Include(d => d.Assignments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var s = SearchTerm.ToLower();
            query = query.Where(d =>
                d.User.FirstName.ToLower().Contains(s) ||
                d.User.LastName.ToLower().Contains(s) ||
                (d.User.Email != null && d.User.Email.ToLower().Contains(s)) ||
                (d.LicensePlate != null && d.LicensePlate.ToLower().Contains(s)));
        }

        var all = await query.ToListAsync();

        TotalDrivers = all.Count;
        AvailableDrivers = all.Count(d => d.IsAvailable);
        TotalDeliveries = all.Sum(d => d.Assignments.Count(a => a.Status == DriverJobAction.Completed));
        TotalEarnings = all.Sum(d => d.TotalEarnings);

        Drivers = all
            .OrderByDescending(d => d.Assignments.Count(a => a.Status == DriverJobAction.Completed))
            .ThenBy(d => d.User.FirstName)
            .Select(d => new DriverRow
            {
                DriverId = d.Id,
                FullName = $"{d.User.FirstName} {d.User.LastName}".Trim(),
                Email = d.User.Email ?? string.Empty,
                Phone = d.User.PhoneNumber,
                LicensePlate = d.LicensePlate,
                VehicleModel = d.VehicleModel,
                IsAvailable = d.IsAvailable,
                IsActive = d.User.IsActive,
                Status = d.Status,
                Rating = d.Rating,
                TotalEarnings = d.TotalEarnings,
                TotalAssignments = d.Assignments.Count,
                CompletedDeliveries = d.Assignments.Count(a => a.Status == DriverJobAction.Completed),
                ActiveAssignments = d.Assignments.Count(a => a.Status == DriverJobAction.Pending || a.Status == DriverJobAction.Accepted),
                JoinedAt = d.CreatedAt
            })
            .ToList();
    }

    private async Task EnsureDriverProfilesAsync()
    {
        var driverRoles = new[] { AppRoles.PickupDriver, AppRoles.DeliveryDriver };

        var driverRoleIds = await _db.Roles
            .Where(r => r.Name != null && driverRoles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        var driverRoleUserIds = await _db.UserRoles
            .Where(ur => driverRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        // Remove orphaned driver rows (UserId not a current driver-role user).
        var orphaned = await _db.Drivers.Where(d => !driverRoleUserIds.Contains(d.UserId)).ToListAsync();
        if (orphaned.Count > 0)
        {
            _db.Drivers.RemoveRange(orphaned);
            await _db.SaveChangesAsync();
        }

        // Create profiles for driver-role users missing one.
        var existing = await _db.Drivers.Select(d => d.UserId).ToListAsync();
        var missing = driverRoleUserIds.Except(existing).ToList();
        if (missing.Count > 0)
        {
            foreach (var userId in missing)
            {
                _db.Drivers.Add(new DriverEntity
                {
                    UserId = userId,
                    Status = DriverStatus.Offline,
                    IsAvailable = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }
    }

    public class DriverRow
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleModel { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public DriverStatus Status { get; set; }
        public double Rating { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedDeliveries { get; set; }
        public int ActiveAssignments { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
