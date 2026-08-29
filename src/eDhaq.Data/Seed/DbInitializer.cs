using eDhaq.Common.Constants;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eDhaq.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Only relational (MySQL) databases are supported.
            // Apply pending migrations so the schema is always up to date.
            if (db.Database.IsRelational())
            {
                await db.Database.MigrateAsync();
            }

            // Seed identity roles
            await SeedRolesAsync(roleManager, logger);

            // Seed default admin and demo customer users
            await SeedAdminUserAsync(userManager, db, logger);
            await SeedDemoCustomerAsync(userManager, db, logger);

            await db.SaveChangesAsync();
            logger.LogInformation("Database initialization completed. Roles and default users seeded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");

            // Allow the app to start in development even if seeding fails
            // so developers can debug. In production, fail fast.
            if (!env.IsDevelopment())
            {
                throw;
            }
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    // Admin user: admin@edhaq.com / Admin@123!
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILogger logger)
    {
        const string email = "admin@edhaq.com";

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            // Delete existing user to ensure a clean password hash
            logger.LogInformation("Admin user already exists, deleting and recreating: {Email}", email);
            var deleteResult = await userManager.DeleteAsync(existingAdmin);
            if (!deleteResult.Succeeded)
            {
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to delete existing admin user: {Errors}", errors);
                return;
            }
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create admin user: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Administrator);

        // Create Employee record linked to the admin user
        db.Employees.Add(new Employee
        {
            UserId = admin.Id,
            Position = "Administrator",
            Department = "Management",
            HireDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        logger.LogInformation("Seeded admin user: {Email} / Admin@123!", email);
    }

    // Demo customer: customer@edhaq.com / Customer@123!
    private static async Task SeedDemoCustomerAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILogger logger)
    {
        const string email = "customer@edhaq.com";

        var existingCustomer = await userManager.FindByEmailAsync(email);
        if (existingCustomer is not null)
        {
            logger.LogInformation("Demo customer already exists, deleting and recreating: {Email}", email);
            var deleteResult = await userManager.DeleteAsync(existingCustomer);
            if (!deleteResult.Succeeded)
            {
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to delete existing demo customer: {Errors}", errors);
                return;
            }
        }

        var customerUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Demo",
            LastName = "Customer",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(customerUser, "Customer@123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create demo customer: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(customerUser, AppRoles.Customer);

        // Create Customer record linked to the user
        db.Customers.Add(new Customer
        {
            UserId = customerUser.Id,
            ReferralCode = $"EDQ{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CreatedAt = DateTime.UtcNow
        });

        logger.LogInformation("Seeded demo customer: {Email} / Customer@123!", email);
    }
}
