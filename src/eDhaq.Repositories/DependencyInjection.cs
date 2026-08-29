using eDhaq.Repositories.Implementations;
using eDhaq.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eDhaq.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
