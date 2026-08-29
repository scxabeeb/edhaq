using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.AuditLogs;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<AuditLog> Logs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Logs = await _db.AuditLogs
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync();
    }
}
