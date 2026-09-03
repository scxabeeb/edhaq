using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverEntity = eDhaq.Models.Entities.Driver;

namespace eDhaq.Web.Pages.Admin.Drivers;

[Authorize(Roles = "Administrator,Manager")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailsModel(AppDbContext db)
    {
        _db = db;
    }

    public DriverEntity? Driver { get; private set; }
    public List<DriverAssignment> Assignments { get; private set; } = [];

    public int TotalAssignments { get; private set; }
    public int CompletedCount { get; private set; }
    public int PendingCount { get; private set; }
    public int AcceptedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public double CompletionRate { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Driver = await _db.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (Driver is null)
        {
            TempData["ErrorMessage"] = "Driver not found.";
            return RedirectToPage("./Index");
        }

        Assignments = await _db.DriverAssignments
            .Where(a => a.DriverId == id)
            .Include(a => a.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.User)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        TotalAssignments = Assignments.Count;
        CompletedCount = Assignments.Count(a => a.Status == DriverJobAction.Completed);
        PendingCount = Assignments.Count(a => a.Status == DriverJobAction.Pending);
        AcceptedCount = Assignments.Count(a => a.Status == DriverJobAction.Accepted);
        RejectedCount = Assignments.Count(a => a.Status == DriverJobAction.Rejected);
        CompletionRate = TotalAssignments > 0 ? (double)CompletedCount / TotalAssignments * 100 : 0;

        return Page();
    }

    // ── MANAGE ASSIGNMENT STATUS ─────────────────────────────────────────
    public async Task<IActionResult> OnPostSetAssignmentStatusAsync(int assignmentId, DriverJobAction status, int driverId)
    {
        var assignment = await _db.DriverAssignments
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.DriverId == driverId);

        if (assignment is null)
        {
            TempData["ErrorMessage"] = "Assignment not found.";
            return RedirectToPage(new { id = driverId });
        }

        assignment.Status = status;
        if (status == DriverJobAction.Accepted) assignment.AcceptedAt = DateTime.UtcNow;
        if (status == DriverJobAction.Completed) assignment.CompletedAt = DateTime.UtcNow;

        // Keep driver completed-deliveries counter in sync.
        var driver = await _db.Drivers.FindAsync(driverId);
        if (driver is not null)
        {
            driver.CompletedDeliveries = await _db.DriverAssignments
                .CountAsync(a => a.DriverId == driverId && a.Status == DriverJobAction.Completed);
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Assignment for {assignment.Order.OrderNumber} marked {status}.";
        return RedirectToPage(new { id = driverId });
    }
}
