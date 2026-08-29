using eDhaq.Common.ViewModels;
using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    public List<SelectListItem> CityOptions { get; private set; } = [];
    public List<SelectListItem> VillageOptions { get; private set; } = [];
    public List<SelectListItem> SubVillageOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadLocationOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLocationOptionsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var selectedVillage = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == Input.VillageId && x.CityId == Input.CityId && x.IsActive);

        if (selectedVillage is null)
        {
            ModelState.AddModelError(nameof(Input.VillageId), "Please select a valid village.");
            return Page();
        }

        if (Input.SubVillageId.HasValue)
        {
            var subVillageValid = await _db.SubVillages
                .AsNoTracking()
                .AnyAsync(x => x.Id == Input.SubVillageId.Value && x.VillageId == Input.VillageId && x.IsActive);

            if (!subVillageValid)
            {
                ModelState.AddModelError(nameof(Input.SubVillageId), "Please select a valid sub-village.");
                return Page();
            }
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            PhoneNumber = Input.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        var customer = new eDhaq.Models.Entities.Customer
        {
            UserId = user.Id,
            ReferralCode = $"EDQ{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        _db.Addresses.Add(new Address
        {
            CustomerId = customer.Id,
            Label = "Home",
            Street = Input.Street,
            CityId = Input.CityId,
            VillageId = Input.VillageId,
            SubVillageId = Input.SubVillageId,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        _db.Wallets.Add(new Wallet
        {
            CustomerId = customer.Id,
            Balance = 0,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["SuccessMessage"] = "Account created successfully.";
        return RedirectToPage("/Customer/Index");
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
            .Where(x => x.IsActive && x.Name == "Garowe")
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
}
