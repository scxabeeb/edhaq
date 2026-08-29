using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eDhaq.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Only relational (MySQL) databases are supported now.
            // Apply pending migrations so the schema is always up to date.
            if (db.Database.IsRelational())
            {
                await db.Database.MigrateAsync();
            }

            // Seed essential identity roles only — no demo users, cities,
            // service categories, or any other reference data.
            // Every piece of data must be created through the application.
            await SeedRolesAsync(roleManager, logger);

            await db.SaveChangesAsync();
            logger.LogInformation("Database initialization completed. All seeded data has been removed — only roles are ensured.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["Administrator", "Manager", "Cashier", "LaundryStaff", "PickupDriver", "DeliveryDriver", "Customer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }
}
