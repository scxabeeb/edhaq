using eDhaq.Common.Constants;
using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace eDhaq.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public IndexModel(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    // ── Roles list ──────────────────────────────────────────────────
    public List<RoleRow> RoleRows { get; private set; } = [];

    // ── Page permission entries (reference matrix) ──────────────────
    public List<PagePermissionEntry> PagePermissions { get; private set; } = [];

    // ── Create role ─────────────────────────────────────────────────
    [BindProperty]
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._\- ]*$", ErrorMessage = "Role name may only contain letters, numbers, spaces, dots, hyphens, and underscores.")]
    public string NewRoleName { get; set; } = string.Empty;

    // ── Delete role ─────────────────────────────────────────────────
    [BindProperty]
    public string? RoleIdToDelete { get; set; }

    // ── Seeded roles that cannot be deleted ─────────────────────────
    private static readonly HashSet<string> ProtectedRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        AppRoles.Administrator,
        AppRoles.Manager,
        AppRoles.Cashier,
        AppRoles.LaundryStaff,
        AppRoles.PickupDriver,
        AppRoles.DeliveryDriver,
        AppRoles.Customer
    };

    public class RoleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public bool IsProtected { get; set; }
    }

    public class PagePermissionEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] AllowedRoles { get; set; } = [];
    }

    // ── Static definition of page areas and their allowed roles ─────
    private static readonly List<PagePermissionEntry> AllPagePermissions = new()
    {
        new() { Name = "Admin Dashboard", Path = "/Admin", Description = "Overview dashboard for administrators and managers",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Manager] },
        new() { Name = "Order Management", Path = "/Admin/Orders", Description = "Manage all orders, assign drivers, update stages",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Manager] },
        new() { Name = "Finance Portal", Path = "/Finance", Description = "Financial overview, payments, and revenue reports",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier] },
        new() { Name = "Payment Management", Path = "/Admin/Payments", Description = "View and update all payment statuses",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Manager] },
        new() { Name = "Reports", Path = "/Admin/Reports", Description = "Generate order reports (CSV, Excel, PDF)",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Manager] },
        new() { Name = "User Management", Path = "/Admin/Users", Description = "Create, edit, and manage user accounts",
                AllowedRoles = [AppRoles.Administrator] },
        new() { Name = "Role Management", Path = "/Admin/Roles", Description = "Create, edit, and manage user roles and permissions",
                AllowedRoles = [AppRoles.Administrator] },
        new() { Name = "Configuration", Path = "/Admin/Services", Description = "Manage services, coupons, cities, and settings",
                AllowedRoles = [AppRoles.Administrator] },
        new() { Name = "Audit Logs", Path = "/Admin/AuditLogs", Description = "View system audit trail",
                AllowedRoles = [AppRoles.Administrator] },
        new() { Name = "Customer Portal", Path = "/Customer", Description = "Customer dashboard, orders, addresses, payments",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Customer] },
        new() { Name = "Driver Portal", Path = "/Driver", Description = "Driver dashboard, assignments, and earnings",
                AllowedRoles = [AppRoles.Administrator, AppRoles.PickupDriver, AppRoles.DeliveryDriver] },
        new() { Name = "Staff Portal", Path = "/Staff", Description = "Laundry staff dashboard and kanban board",
                AllowedRoles = [AppRoles.Administrator, AppRoles.LaundryStaff] },
        new() { Name = "Cashier Desk", Path = "/Cashier/Payments", Description = "Cashier payment processing",
                AllowedRoles = [AppRoles.Administrator, AppRoles.Cashier] },
    };

    // ──────────────────────────────────────────────────────────────────
    // GET
    // ──────────────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        await LoadRolesAsync();
        PagePermissions = AllPagePermissions;
    }

    // ──────────────────────────────────────────────────────────────────
    // POST: Create Role
    // ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateRoleAsync()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join(" ", errors);
            return RedirectToPage();
        }

        if (ProtectedRoleNames.Contains(NewRoleName))
        {
            TempData["ErrorMessage"] = $"The role '{NewRoleName}' already exists as a system role.";
            return RedirectToPage();
        }

        if (await _roleManager.RoleExistsAsync(NewRoleName))
        {
            TempData["ErrorMessage"] = $"A role named '{NewRoleName}' already exists.";
            return RedirectToPage();
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(NewRoleName.Trim()));
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Role '{NewRoleName}' created successfully. Assign users to this role from the User Management page.";
        return RedirectToPage();
    }

    // ──────────────────────────────────────────────────────────────────
    // POST: Delete Role
    // ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteRoleAsync(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            TempData["ErrorMessage"] = "Role not found.";
            return RedirectToPage();
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            TempData["ErrorMessage"] = "Role not found.";
            return RedirectToPage();
        }

        if (ProtectedRoleNames.Contains(role.Name!))
        {
            TempData["ErrorMessage"] = $"The '{role.Name}' role is a system role and cannot be deleted.";
            return RedirectToPage();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Count > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete role '{role.Name}' — {usersInRole.Count} user(s) are assigned to it. Remove them first.";
            return RedirectToPage();
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Role '{role.Name}' has been deleted.";
        return RedirectToPage();
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────
    private async Task LoadRolesAsync()
    {
        var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        RoleRows = [];

        foreach (var role in allRoles)
        {
            var count = await _userManager.Users
                .CountAsync(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id));

            RoleRows.Add(new RoleRow
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = count,
                IsProtected = ProtectedRoleNames.Contains(role.Name!)
            });
        }
    }

    public static bool IsProtected(string roleName) => ProtectedRoleNames.Contains(roleName);
}
