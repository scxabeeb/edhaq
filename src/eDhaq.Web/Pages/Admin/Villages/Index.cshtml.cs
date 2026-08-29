using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Villages;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<VillageRow> Villages { get; private set; } = [];
    public List<SelectListItem> CityOptions { get; private set; } = [];

    [BindProperty] public int CityId { get; set; }
    [BindProperty] public string VillageName { get; set; } = string.Empty;
    [BindProperty] public int VillageId { get; set; }
    [BindProperty] public string SubVillageName { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int? CityFilter { get; set; }

    public class VillageRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<SubVillageRow> SubVillages { get; set; } = [];
    }

    public class SubVillageRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        CityOptions = await _db.Cities
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = $"{x.Name} ({x.Country})" })
            .ToListAsync();

        var query = _db.Villages.Include(x => x.City).AsQueryable();
        if (CityFilter.HasValue && CityFilter.Value > 0)
        {
            query = query.Where(x => x.CityId == CityFilter.Value);
        }

        Villages = await query
            .OrderBy(x => x.City.Name).ThenBy(x => x.Name)
            .Select(x => new VillageRow
            {
                Id = x.Id,
                Name = x.Name,
                CityId = x.CityId,
                CityName = x.City.Name,
                IsActive = x.IsActive,
                SubVillages = x.SubVillages
                    .OrderBy(s => s.Name)
                    .Select(s => new SubVillageRow { Id = s.Id, Name = s.Name, IsActive = s.IsActive })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateVillageAsync()
    {
        VillageName = VillageName.Trim();
        if (string.IsNullOrWhiteSpace(VillageName) || CityId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a city and enter a village name.";
            await LoadDataAsync();
            return Page();
        }

        var cityExists = await _db.Cities.AnyAsync(x => x.Id == CityId);
        if (!cityExists)
        {
            TempData["ErrorMessage"] = "Selected city was not found.";
            await LoadDataAsync();
            return Page();
        }

        var duplicate = await _db.Villages.AnyAsync(x => x.CityId == CityId && x.Name.ToLower() == VillageName.ToLower());
        if (duplicate)
        {
            TempData["ErrorMessage"] = $"Village \"{VillageName}\" already exists in the selected city.";
            await LoadDataAsync();
            return Page();
        }

        _db.Villages.Add(new Village { CityId = CityId, Name = VillageName, IsActive = true });
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Village \"{VillageName}\" added.";
        return RedirectToPage(new { CityFilter });
    }

    public async Task<IActionResult> OnPostToggleVillageAsync(int id)
    {
        var village = await _db.Villages.FirstOrDefaultAsync(x => x.Id == id);
        if (village is not null)
        {
            village.IsActive = !village.IsActive;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Village \"{village.Name}\" is now {(village.IsActive ? "active" : "disabled")}.";
        }

        return RedirectToPage(new { CityFilter });
    }

    public async Task<IActionResult> OnPostDeleteVillageAsync(int id)
    {
        var village = await _db.Villages.FirstOrDefaultAsync(x => x.Id == id);
        if (village is null)
        {
            return RedirectToPage(new { CityFilter });
        }

        var hasAddresses = await _db.Addresses.AnyAsync(x => x.VillageId == id);
        if (hasAddresses)
        {
            TempData["ErrorMessage"] = "Cannot delete a village that is used by customer addresses. Disable it instead.";
            return RedirectToPage(new { CityFilter });
        }

        var subVillages = await _db.SubVillages.Where(x => x.VillageId == id).ToListAsync();
        _db.SubVillages.RemoveRange(subVillages);
        _db.Villages.Remove(village);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Village \"{village.Name}\" deleted.";
        return RedirectToPage(new { CityFilter });
    }


    public async Task<IActionResult> OnPostAddSubVillageAsync()
    {
        SubVillageName = SubVillageName.Trim();
        if (string.IsNullOrWhiteSpace(SubVillageName) || VillageId <= 0)
        {
            TempData["ErrorMessage"] = "Please enter a sub-village name.";
            await LoadDataAsync();
            return Page();
        }

        var villageExists = await _db.Villages.AnyAsync(x => x.Id == VillageId);
        if (!villageExists)
        {
            TempData["ErrorMessage"] = "Village was not found.";
            await LoadDataAsync();
            return Page();
        }

        var duplicate = await _db.SubVillages.AnyAsync(x => x.VillageId == VillageId && x.Name.ToLower() == SubVillageName.ToLower());
        if (duplicate)
        {
            TempData["ErrorMessage"] = $"Sub-village \"{SubVillageName}\" already exists in this village.";
            await LoadDataAsync();
            return Page();
        }

        _db.SubVillages.Add(new SubVillage { VillageId = VillageId, Name = SubVillageName, IsActive = true });
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Sub-village \"{SubVillageName}\" added.";
        return RedirectToPage(new { CityFilter });
    }


    public async Task<IActionResult> OnPostToggleSubVillageAsync(int id)
    {
        var subVillage = await _db.SubVillages.FirstOrDefaultAsync(x => x.Id == id);
        if (subVillage is not null)
        {
            subVillage.IsActive = !subVillage.IsActive;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Sub-village \"{subVillage.Name}\" is now {(subVillage.IsActive ? "active" : "disabled")}.";
        }

        return RedirectToPage(new { CityFilter });
    }

    public async Task<IActionResult> OnPostDeleteSubVillageAsync(int id)
    {
        var subVillage = await _db.SubVillages.FirstOrDefaultAsync(x => x.Id == id);
        if (subVillage is null)
        {
            return RedirectToPage(new { CityFilter });
        }

        var hasAddresses = await _db.Addresses.AnyAsync(x => x.SubVillageId == id);
        if (hasAddresses)
        {
            TempData["ErrorMessage"] = "Cannot delete a sub-village that is used by customer addresses. Disable it instead.";
            return RedirectToPage(new { CityFilter });
        }

        _db.SubVillages.Remove(subVillage);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Sub-village \"{subVillage.Name}\" deleted.";
        return RedirectToPage(new { CityFilter });
    }
}