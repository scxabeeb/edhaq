using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Settings;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<AppSetting> Settings { get; private set; } = [];

    [BindProperty]
    public AppSetting Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Settings = await _db.AppSettings.OrderBy(x => x.Key).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.Key = Input.Key.Trim();
        Input.UpdatedAt = DateTime.UtcNow;
        _db.AppSettings.Add(Input);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Setting added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string value, bool isPublic)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Id == id);
        if (setting is not null)
        {
            setting.Value = value;
            setting.IsPublic = isPublic;
            setting.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Setting updated.";
        }

        return RedirectToPage();
    }
}
