using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Admin.DashboardApi;

[Authorize(Roles = "Administrator,Manager")]
[IgnoreAntiforgeryToken]
public class RevenueModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public RevenueModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public IActionResult OnGet()
    {
        return NotFound();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync();
        var payload = new
        {
            weekly = dashboard.WeeklyRevenue.Select(x => new { x.Label, x.Value }),
            monthly = dashboard.MonthlyRevenue.Select(x => new { x.Label, x.Value }),
            statuses = dashboard.OrdersByStatus.Select(x => new { status = x.Key.ToString(), count = x.Value })
        };

        return new JsonResult(payload);
    }
}
