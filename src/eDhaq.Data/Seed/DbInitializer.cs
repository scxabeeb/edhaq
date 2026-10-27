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

            // Seed default support-contact settings (public AppSettings)
            await SeedSupportContactsAsync(db, logger);

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

    // Admin user: admin@edhaq.com (password from SEED_ADMIN_PASSWORD env var on first creation)
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILogger logger)
    {
        const string email = "admin@edhaq.com";

        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            // Do NOT delete/recreate: AuditLogs (and other tables) reference Users.Id,
            // so deleting the user violates the FK and crashes startup. Update in place.
            logger.LogInformation("Admin user already exists, ensuring password/role/profile: {Email}", email);

            // Never reset an existing admin's password on startup.

            existingAdmin.IsActive = true;
            existingAdmin.EmailConfirmed = true;
            await userManager.UpdateAsync(existingAdmin);

            if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.Administrator))
            {
                await userManager.AddToRoleAsync(existingAdmin, AppRoles.Administrator);
            }

            // Ensure an Employee profile exists.
            var hasEmployee = await db.Employees.AnyAsync(e => e.UserId == existingAdmin.Id);
            if (!hasEmployee)
            {
                db.Employees.Add(new Employee
                {
                    UserId = existingAdmin.Id,
                    Position = "Administrator",
                    Department = "Management",
                    HireDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            logger.LogInformation("Seeded admin user: {Email}", email);
            return;
        }

        // The first admin's password comes from the SEED_ADMIN_PASSWORD environment variable.
        // Without it, no admin is created (no default passwords in source code).
        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("SEED_ADMIN_PASSWORD is not set; skipping admin user creation.");
            return;
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

        var result = await userManager.CreateAsync(admin, adminPassword);
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

        logger.LogInformation("Seeded admin user: {Email}", email);
    }

    // Demo customer for app-store reviewers: customer@edhaq.com.
    // Password comes from the SEED_DEMO_CUSTOMER_PASSWORD environment variable; if it is
    // not set, the demo customer is neither created nor reset.
    private static async Task SeedDemoCustomerAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILogger logger)
    {
        const string email = "customer@edhaq.com";
        var demoPassword = Environment.GetEnvironmentVariable("SEED_DEMO_CUSTOMER_PASSWORD");
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogInformation("SEED_DEMO_CUSTOMER_PASSWORD is not set; skipping demo customer.");
            return;
        }

        var existingCustomer = await userManager.FindByEmailAsync(email);
        if (existingCustomer is not null)
        {
            // Do NOT delete/recreate: AuditLogs (and other tables) reference Users.Id.
            // Update in place instead.
            logger.LogInformation("Demo customer already exists, ensuring password/role/profile: {Email}", email);

            var token = await userManager.GeneratePasswordResetTokenAsync(existingCustomer);
            await userManager.ResetPasswordAsync(existingCustomer, token, demoPassword);

            existingCustomer.IsActive = true;
            existingCustomer.EmailConfirmed = true;
            await userManager.UpdateAsync(existingCustomer);

            if (!await userManager.IsInRoleAsync(existingCustomer, AppRoles.Customer))
            {
                await userManager.AddToRoleAsync(existingCustomer, AppRoles.Customer);
            }

            var hasCustomer = await db.Customers.AnyAsync(c => c.UserId == existingCustomer.Id);
            if (!hasCustomer)
            {
                db.Customers.Add(new Customer
                {
                    UserId = existingCustomer.Id,
                    ReferralCode = $"EDQ{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            logger.LogInformation("Seeded demo customer: {Email}", email);
            return;
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

        var result = await userManager.CreateAsync(customerUser, demoPassword);
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

        logger.LogInformation("Seeded demo customer: {Email}", email);
    }

    // Default public support contacts shown in the mobile apps.
    private static async Task SeedSupportContactsAsync(AppDbContext db, ILogger logger)
    {
        var defaults = new (string Key, string Value, string Description)[]
        {
            ("Support.Phone", "+967771234567", "Support phone number shown in the mobile app."),
            ("Support.Whatsapp", "+967771234567", "Support WhatsApp number shown in the mobile app."),
            ("Support.Email", "support@edhaq.com", "Support email shown in the mobile app."),
            ("Support.WorkingHours", "Sat - Thu, 8:00 AM - 10:00 PM", "Support working hours shown in the mobile app."),
        };

        foreach (var (key, value, description) in defaults)
        {
            if (!await db.AppSettings.AnyAsync(s => s.Key == key))
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    IsPublic = true,
                    UpdatedAt = DateTime.UtcNow
                });
                logger.LogInformation("Seeded support contact setting: {Key}", key);
            }
        }
    }
}
