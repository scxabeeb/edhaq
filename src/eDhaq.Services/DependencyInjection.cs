using eDhaq.Services.Implementations;
using eDhaq.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eDhaq.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
