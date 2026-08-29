using eDhaq.Common.ViewModels;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Admin;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public AdminDashboardViewModel Dashboard { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardService.GetAdminDashboardAsync();
    }
}
