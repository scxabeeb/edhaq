using eDhaq.Models.Entities;
using eDhaq.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace eDhaq.Web.Pages.Auth;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileModel(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> CityOptions { get; private set; } = [];
    public List<SelectListItem> VillageOptions { get; private set; } = [];
    public List<SelectListItem> SubVillageOptions { get; private set; } = [];

    public class InputModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string AddressLabel { get; set; } = "Home";

        [MaxLength(255)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        public int CityId { get; set; }

        public int VillageId { get; set; }

        public int? SubVillageId { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password), Compare(nameof(NewPassword))]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await _db.Users
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return Challenge();
        }

        var address = await GetDefaultAddressAsync(userId);

        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AddressLabel = address?.Label ?? "Home",
            Street = address?.Street,
            District = address?.District,
            CityId = address?.CityId ?? 0,
            VillageId = address?.VillageId ?? 0,
            SubVillageId = address?.SubVillageId
        };

        await LoadLocationOptionsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLocationOptionsAsync();

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existingAddress = await GetDefaultAddressAsync(user.Id);
        if (existingAddress is not null || !string.IsNullOrWhiteSpace(Input.Street))
        {
            if (string.IsNullOrWhiteSpace(Input.Street))
            {
                ModelState.AddModelError(nameof(Input.Street), "Street is required when saving an address.");
                return Page();
            }

            var villageValid = await _db.Villages.AnyAsync(x => x.Id == Input.VillageId && x.CityId == Input.CityId && x.IsActive);
            if (!villageValid)
            {
                ModelState.AddModelError(nameof(Input.VillageId), "Please select a valid village.");
                return Page();
            }

            if (Input.SubVillageId.HasValue)
            {
                var subVillageValid = await _db.SubVillages.AnyAsync(x => x.Id == Input.SubVillageId.Value && x.VillageId == Input.VillageId && x.IsActive);
                if (!subVillageValid)
                {
                    ModelState.AddModelError(nameof(Input.SubVillageId), "Please select a valid sub-village.");
                    return Page();
                }
            }
        }

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.PhoneNumber = Input.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors)
            {
                ModelState.AddModelError(string.Empty, e.Description);
            }
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword ?? string.Empty, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, e.Description);
                }
                return Page();
            }
        }

        if (existingAddress is not null || !string.IsNullOrWhiteSpace(Input.Street))
        {
            var customer = await EnsureCustomerProfileAsync(user.Id);
            var address = existingAddress;

            if (address is null)
            {
                address = new Address
                {
                    CustomerId = customer.Id,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Addresses.Add(address);
            }

            address.Label = string.IsNullOrWhiteSpace(Input.AddressLabel) ? "Home" : Input.AddressLabel.Trim();
            address.Street = Input.Street!.Trim();
            address.District = CleanOptional(Input.District);
            address.CityId = Input.CityId;
            address.VillageId = Input.VillageId;
            address.SubVillageId = Input.SubVillageId;

            await _db.SaveChangesAsync();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetVillagesAsync(int cityId)
    {
        var villages = await _db.Villages
            .Where(x => x.CityId == cityId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return new JsonResult(villages);
    }

    public async Task<IActionResult> OnGetSubVillagesAsync(int villageId)
    {
        var subVillages = await _db.SubVillages
            .Where(x => x.VillageId == villageId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return new JsonResult(subVillages);
    }

    private async Task LoadLocationOptionsAsync()
    {
        CityOptions = await _db.Cities
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();

        if (Input.CityId == 0 && CityOptions.Count > 0)
        {
            Input.CityId = int.Parse(CityOptions[0].Value!, System.Globalization.CultureInfo.InvariantCulture);
        }

        VillageOptions = await _db.Villages
            .Where(x => x.CityId == Input.CityId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();

        if (Input.VillageId == 0 && VillageOptions.Count > 0)
        {
            Input.VillageId = int.Parse(VillageOptions[0].Value!, System.Globalization.CultureInfo.InvariantCulture);
        }

        SubVillageOptions = await _db.SubVillages
            .Where(x => x.VillageId == Input.VillageId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();
    }

    private async Task<Address?> GetDefaultAddressAsync(string userId)
    {
        return await _db.Addresses
            .Include(x => x.Customer)
            .Where(x => x.Customer.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<eDhaq.Models.Entities.Customer> EnsureCustomerProfileAsync(string userId)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (customer is not null)
        {
            return customer;
        }

        customer = new eDhaq.Models.Entities.Customer
        {
            UserId = userId,
            ReferralCode = $"EDQ{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    private static string? CleanOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
