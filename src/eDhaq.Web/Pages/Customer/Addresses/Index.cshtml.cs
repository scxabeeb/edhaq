using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer.Addresses;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Address> Addresses { get; private set; } = [];
    public List<SelectListItem> CityOptions { get; private set; } = [];
    public List<SelectListItem> VillageOptions { get; private set; } = [];
    public List<SelectListItem> SubVillageOptions { get; private set; } = [];

    [BindProperty]
    public Address Input { get; set; } = new();

        public bool IsAdmin => User?.IsInRole("Administrator") ?? false;

    public async Task<IActionResult> OnGetAsync()
    {
        if (IsAdmin)
        {
            // Administrators see all addresses across all customers (read-only overview).
            await LoadAllAsync();
            return Page();
        }

        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            TempData["ErrorMessage"] = "Customer profile not found.";
            return RedirectToPage("/Customer/Index");
        }

        await LoadAsync(customer.Id);
        return Page();
    }

        public async Task<IActionResult> OnPostAddAsync()
    {
        if (IsAdmin)
        {
            TempData["ErrorMessage"] = "Address creation is not available from the admin overview. Use the Admin → Customers area to manage customer addresses.";
            return RedirectToPage();
        }

        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return RedirectToPage("/Customer/Index");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(customer.Id);
            return Page();
        }

        var villageValid = await _db.Villages.AnyAsync(x => x.Id == Input.VillageId && x.CityId == Input.CityId && x.IsActive);
        if (!villageValid)
        {
            ModelState.AddModelError(nameof(Input.VillageId), "Please select a valid village.");
            await LoadAsync(customer.Id);
            return Page();
        }

        if (Input.SubVillageId.HasValue)
        {
            var subVillageValid = await _db.SubVillages.AnyAsync(x => x.Id == Input.SubVillageId.Value && x.VillageId == Input.VillageId && x.IsActive);
            if (!subVillageValid)
            {
                ModelState.AddModelError(nameof(Input.SubVillageId), "Please select a valid sub-village.");
                await LoadAsync(customer.Id);
                return Page();
            }
        }

        Input.CustomerId = customer.Id;
        Input.CreatedAt = DateTime.UtcNow;

        if (Input.IsDefault)
        {
            var existing = await _db.Addresses.Where(x => x.CustomerId == customer.Id && x.IsDefault).ToListAsync();
            foreach (var e in existing)
            {
                e.IsDefault = false;
            }
        }

        _db.Addresses.Add(Input);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Address added.";
        return RedirectToPage();
    }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (IsAdmin)
        {
            // Administrators can delete any address.
            var entity = await _db.Addresses.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is not null)
            {
                _db.Addresses.Remove(entity);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Address deleted.";
            }
            return RedirectToPage();
        }

        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return RedirectToPage("/Customer/Index");
        }

        var ownedEntity = await _db.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customer.Id);
        if (ownedEntity is not null)
        {
            _db.Addresses.Remove(ownedEntity);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

        private async Task LoadAsync(int customerId)
    {
        Addresses = await _db.Addresses
            .Where(x => x.CustomerId == customerId)
            .Include(x => x.City)
            .Include(x => x.Village)
            .Include(x => x.SubVillage)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

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

    private async Task LoadAllAsync()
    {
        Addresses = await _db.Addresses
            .Include(x => x.City)
            .Include(x => x.Village)
            .Include(x => x.SubVillage)
            .Include(x => x.Customer)
            .ThenInclude(c => c.User)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        CityOptions = await _db.Cities
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();

        var defaultCityId = CityOptions.Count > 0 && int.TryParse(CityOptions[0].Value, out var firstCityId)
            ? firstCityId
            : 0;

        VillageOptions = await _db.Villages
            .Where(x => x.CityId == defaultCityId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();

        var defaultVillageId = VillageOptions.Count > 0 && int.TryParse(VillageOptions[0].Value, out var firstVillageId)
            ? firstVillageId
            : 0;

        SubVillageOptions = await _db.SubVillages
            .Where(x => x.VillageId == defaultVillageId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();
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

    private async Task<eDhaq.Models.Entities.Customer?> GetCustomerAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _db.Customers.FirstOrDefaultAsync(x => x.UserId == userId);
    }
}
