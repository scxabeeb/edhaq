using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Staff.Orders;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class PickupModel : PageModel
{
    private readonly AppDbContext _db;

    public PickupModel(AppDbContext db)
    {
        _db = db;
    }

    public List<DriverAssignment> PickupAssignments { get; private set; } = [];
    public int PendingCount { get; private set; }
    public int AcceptedCount { get; private set; }
    public int CompletedCount { get; private set; }
    public int RejectedCount { get; private set; }

    public async Task OnGetAsync()
    {
        var assignments = await _db.DriverAssignments
            .Where(x => x.IsPickup)
            .Include(x => x.Order)
                .ThenInclude(o => o.Customer)
                .ThenInclude(c => c.User)
            .Include(x => x.Order)
                .ThenInclude(o => o.PickupAddress)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .ToListAsync();

        PendingCount = assignments.Count(x => x.Status == DriverJobAction.Pending);
        AcceptedCount = assignments.Count(x => x.Status == DriverJobAction.Accepted);
        CompletedCount = assignments.Count(x => x.Status == DriverJobAction.Completed);
        RejectedCount = assignments.Count(x => x.Status == DriverJobAction.Rejected);

        PickupAssignments = assignments
            .OrderBy(x => x.Status == DriverJobAction.Completed)
            .ThenBy(x => x.Status == DriverJobAction.Rejected)
            .ThenByDescending(x => x.AssignedAt)
            .ToList();
    }
}