using eDhaq.Common.Constants;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using CustomerEntity = eDhaq.Models.Entities.Customer;
using DriverEntity = eDhaq.Models.Entities.Driver;
using EmployeeEntity = eDhaq.Models.Entities.Employee;

namespace eDhaq.Web.Pages.Admin.Users;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _db;

    public IndexModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public List<UserRow> Users { get; private set; } = [];
    public List<string> Roles { get; private set; } = [];

    // Read from the query string in OnGetAsync (no [BindProperty]) so POST handlers
    // don't get these values bound from route/form data and invalidate ModelState.
    public int PageNumber { get; private set; } = 1;

    public string? Search { get; private set; }

    public string? RoleFilter { get; private set; }

    public int PageSize { get; } = 20;
    public int TotalCount { get; private set; }

    // Shared user ID for role/toggle/delete operations
    [BindProperty]
    public string? UserId { get; set; }

    [BindProperty]
    public List<string> SelectedRoles { get; set; } = [];

    // Create user fields
    [BindProperty]
    [Required, MaxLength(100)]
    public string NewFirstName { get; set; } = string.Empty;

    [BindProperty]
    [Required, MaxLength(100)]
    public string NewLastName { get; set; } = string.Empty;

    [BindProperty]
    [Required, EmailAddress, MaxLength(256)]
    public string NewEmail { get; set; } = string.Empty;

    [BindProperty]
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string NewRole { get; set; } = string.Empty;

    // Optional profile fields
    [BindProperty]
    [Phone, MaxLength(20)]
    public string? NewPhone { get; set; }

    // Driver-specific
    [BindProperty]
    [MaxLength(20)]
    public string? NewLicensePlate { get; set; }

    [BindProperty]
    [MaxLength(50)]
    public string? NewVehicleModel { get; set; }

    // Staff-specific
    [BindProperty]
    [MaxLength(50)]
    public string? NewPosition { get; set; }

    [BindProperty]
    public string? EditUserId { get; set; }

    [BindProperty]
    public string? EditFirstName { get; set; }

    [BindProperty]
    public string? EditLastName { get; set; }

    [BindProperty]
    public string? EditEmail { get; set; }

    [BindProperty]
    public string? EditPhone { get; set; }

    [BindProperty]
    public string? EditAlternatePhone { get; set; }

    [BindProperty]
    public string? EditLicensePlate { get; set; }

    [BindProperty]
    public string? EditVehicleModel { get; set; }

    [BindProperty]
    public string? EditPosition { get; set; }

    public async Task OnGetAsync()
    {
        // Read paging/search/filter from the query string only.
        PageNumber = int.TryParse(Request.Query["PageNumber"], out var pn) ? Math.Max(1, pn) : 1;
        Search = Request.Query["Search"].ToString();
        RoleFilter = Request.Query["RoleFilter"].ToString();
        if (string.IsNullOrWhiteSpace(RoleFilter)) RoleFilter = null;
        if (string.IsNullOrWhiteSpace(Search)) Search = null;

        Roles = AppRoles.All.ToList();
        var baseQuery = _userManager.Users
            .Include(x => x.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim().ToLower();
            baseQuery = baseQuery.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(RoleFilter))
        {
            var roleName = RoleFilter.Trim();
            // "Drivers" tab should include BOTH pickup and delivery drivers.
            var roleNames = roleName.Equals(AppRoles.PickupDriver, StringComparison.OrdinalIgnoreCase)
                ? new[] { AppRoles.PickupDriver, AppRoles.DeliveryDriver }
                : new[] { roleName };

            var roleUserIds = await (
                from ur in _db.UserRoles
                join role in _db.Roles on ur.RoleId equals role.Id
                where roleNames.Contains(role.Name!)
                select ur.UserId)
                .ToListAsync();

            baseQuery = baseQuery.Where(u => roleUserIds.Contains(u.Id));
        }

        baseQuery = baseQuery.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
        TotalCount = await baseQuery.CountAsync();
        var users = await baseQuery
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            Users.Add(new UserRow
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                AlternatePhone = user.Customer?.AlternatePhone,
                LicensePlate = user.Driver?.LicensePlate,
                VehicleModel = user.Driver?.VehicleModel,
                Position = user.Employee?.Position,
                CurrentRoles = userRoles.ToList(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }
    }

    // ── CREATE USER ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateUserAsync()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join(" ", errors);
            return RedirectToPage();
        }

        if (!AppRoles.All.Contains(NewRole))
        {
            TempData["ErrorMessage"] = "Invalid role selected.";
            return RedirectToPage();
        }

        var existing = await _userManager.FindByEmailAsync(NewEmail);
        if (existing is not null)
        {
            TempData["ErrorMessage"] = "A user with that email already exists.";
            return RedirectToPage();
        }

        var user = new ApplicationUser
        {
            UserName       = NewEmail,
            Email          = NewEmail,
            FirstName      = NewFirstName.Trim(),
            LastName       = NewLastName.Trim(),
            PhoneNumber    = NewPhone?.Trim(),
            EmailConfirmed = true,
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, NewPassword);
        if (!createResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        var roleResult = await _userManager.AddToRoleAsync(user, NewRole);
        if (!roleResult.Succeeded)
        {
            // User created but role failed — clean up
            await _userManager.DeleteAsync(user);
            TempData["ErrorMessage"] = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        EnsureProfilesForRolesAsync(user, [NewRole], null, NewLicensePlate, NewVehicleModel, NewPosition);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User '{user.Email}' created with role '{NewRole}'.";
        return RedirectToPage();
    }

    // ── ASSIGN ROLES ───────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAssignRoleAsync()
    {
        var user = await LoadUserWithProfilesAsync(UserId ?? string.Empty);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        SelectedRoles = SelectedRoles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (SelectedRoles.Count == 0)
        {
            TempData["ErrorMessage"] = "Select at least one role.";
            return RedirectToPage();
        }

        foreach (var role in SelectedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["ErrorMessage"] = $"Role '{role}' does not exist.";
                return RedirectToPage();
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var addResult = await _userManager.AddToRolesAsync(user, SelectedRoles);
        if (!addResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", addResult.Errors.Select(x => x.Description));
            return RedirectToPage();
        }

        EnsureProfilesForRolesAsync(user, SelectedRoles);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Permissions updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(EditUserId))
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(EditFirstName)
            || string.IsNullOrWhiteSpace(EditLastName)
            || string.IsNullOrWhiteSpace(EditEmail))
        {
            TempData["ErrorMessage"] = "First name, last name, and email are required.";
            return RedirectToPage();
        }

        var user = await LoadUserWithProfilesAsync(EditUserId);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        var normalizedEmail = EditEmail.Trim();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            TempData["ErrorMessage"] = "Another user already uses that email address.";
            return RedirectToPage();
        }

        user.FirstName = EditFirstName.Trim();
        user.LastName = EditLastName.Trim();
        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.PhoneNumber = CleanOptional(EditPhone);
        user.UpdatedAt = DateTime.UtcNow;

        var roles = await _userManager.GetRolesAsync(user);
        EnsureProfilesForRolesAsync(user, roles, EditAlternatePhone, EditLicensePlate, EditVehicleModel, EditPosition);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", updateResult.Errors.Select(x => x.Description));
            return RedirectToPage();
        }

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User '{user.Email}' updated.";
        return RedirectToPage();
    }

    // ── TOGGLE ACTIVE / DISABLE ────────────────────────────────────────────
    public async Task<IActionResult> OnPostToggleActiveAsync()
    {
        var user = await LoadUserWithProfilesAsync(UserId ?? string.Empty);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        // Prevent disabling your own account
        if (user.Email == User.Identity?.Name)
        {
            TempData["ErrorMessage"] = "You cannot disable your own account.";
            return RedirectToPage();
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        TempData["SuccessMessage"] = user.IsActive ? $"{user.Email} has been activated." : $"{user.Email} has been deactivated.";
        return RedirectToPage();
    }

    // ── DELETE USER ────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteUserAsync()
    {
        var user = await LoadUserWithProfilesAsync(UserId ?? string.Empty);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        if (user.Email == User.Identity?.Name)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToPage();
        }

        if (user.Customer is not null)
        {
            var hasOrders = await _db.Orders.AnyAsync(x => x.CustomerId == user.Customer.Id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Customers with orders cannot be deleted. Deactivate the account instead.";
                return RedirectToPage();
            }

            _db.Customers.Remove(user.Customer);
        }

        if (user.Driver is not null)
        {
            var hasAssignments = await _db.DriverAssignments.AnyAsync(x => x.DriverId == user.Driver.Id);
            if (hasAssignments)
            {
                TempData["ErrorMessage"] = "Drivers with assignments cannot be deleted. Deactivate the account instead.";
                return RedirectToPage();
            }

            _db.Drivers.Remove(user.Driver);
        }

        if (user.Employee is not null)
        {
            _db.Employees.Remove(user.Employee);
        }

        var notifications = await _db.Notifications.Where(x => x.UserId == user.Id).ToListAsync();
        if (notifications.Count > 0)
        {
            _db.Notifications.RemoveRange(notifications);
        }

        var auditLogs = await _db.AuditLogs.Where(x => x.UserId == user.Id).ToListAsync();
        if (auditLogs.Count > 0)
        {
            _db.AuditLogs.RemoveRange(auditLogs);
        }

        await _db.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"User '{user.Email}' has been deleted.";
        return RedirectToPage();
    }

    // ── RESET PASSWORD ───────────────────────────────────────────────────
    public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            TempData["ErrorMessage"] = "Password must be at least 8 characters.";
            return RedirectToPage();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Password reset for {user.Email}.";
        return RedirectToPage();
    }

    public class UserRow
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternatePhone { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleModel { get; set; }
        public string? Position { get; set; }
        public List<string> CurrentRoles { get; set; } = [];
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private async Task<ApplicationUser?> LoadUserWithProfilesAsync(string userId)
        => await _db.Users
            .Include(x => x.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == userId);

    private void EnsureProfilesForRolesAsync(
        ApplicationUser user,
        IEnumerable<string> roles,
        string? alternatePhone = null,
        string? licensePlate = null,
        string? vehicleModel = null,
        string? position = null)
    {
        var normalizedRoles = roles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRoles.Contains(AppRoles.Customer, StringComparer.OrdinalIgnoreCase))
        {
            if (user.Customer is null)
            {
                user.Customer = new CustomerEntity
                {
                    UserId = user.Id,
                    AlternatePhone = CleanOptional(alternatePhone),
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user.Customer.AlternatePhone = CleanOptional(alternatePhone);
            }
        }

        if (normalizedRoles.Any(IsDriverRole))
        {
            if (user.Driver is null)
            {
                user.Driver = new DriverEntity
                {
                    UserId = user.Id,
                    LicensePlate = CleanOptional(licensePlate),
                    VehicleModel = CleanOptional(vehicleModel),
                    Status = DriverStatus.Offline,
                    IsAvailable = false,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user.Driver.LicensePlate = CleanOptional(licensePlate);
                user.Driver.VehicleModel = CleanOptional(vehicleModel);
            }
        }

        if (normalizedRoles.Any(IsStaffRole))
        {
            var resolvedPosition = !string.IsNullOrWhiteSpace(position)
                ? position.Trim()
                : GetDefaultPosition(normalizedRoles);

            if (user.Employee is null)
            {
                user.Employee = new EmployeeEntity
                {
                    UserId = user.Id,
                    Position = resolvedPosition,
                    HireDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user.Employee.Position = resolvedPosition;
                user.Employee.IsActive = user.IsActive;
            }
        }
    }

    private static bool IsDriverRole(string role)
        => role is AppRoles.PickupDriver or AppRoles.DeliveryDriver;

    private static bool IsStaffRole(string role)
        => role is AppRoles.LaundryStaff or AppRoles.Cashier or AppRoles.Manager;

    private static string GetDefaultPosition(IEnumerable<string> roles)
        => roles.FirstOrDefault(IsStaffRole) ?? AppRoles.LaundryStaff;

    private static string? CleanOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
