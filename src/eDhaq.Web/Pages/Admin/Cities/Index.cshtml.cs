using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Cities;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<City> Cities { get; private set; } = [];

    [BindProperty]
    public City Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        var q = _db.Cities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Country ?? string.Empty).Contains(s));
        }

        Cities = await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        _db.Cities.Add(Input);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "City added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(x => x.Id == id);
        if (city is not null)
        {
            city.IsActive = !city.IsActive;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage(new { Search });
    }
}
