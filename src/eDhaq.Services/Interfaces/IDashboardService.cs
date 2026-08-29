using eDhaq.Common.ViewModels;

namespace eDhaq.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
}
