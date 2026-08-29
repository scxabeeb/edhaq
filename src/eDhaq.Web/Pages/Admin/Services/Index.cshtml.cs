using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Services;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<LaundryService> Services { get; private set; } = [];
    public List<ServiceCategory> Categories { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];

    [BindProperty]
    public LaundryService Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _db.ServiceCategories.OrderBy(x => x.SortOrder).ToListAsync();
        CategoryOptions = Categories.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        Services = await _db.LaundryServices.Include(x => x.Category).OrderBy(x => x.SortOrder).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.CreatedAt = DateTime.UtcNow;
        _db.LaundryServices.Add(Input);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Service added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var item = await _db.LaundryServices.FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null)
        {
            item.IsActive = !item.IsActive;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (Input.Id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid service.";
            return RedirectToPage();
        }

        var item = await _db.LaundryServices.FindAsync(Input.Id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "Service not found.";
            return RedirectToPage();
        }

        item.Name           = Input.Name;
        item.CategoryId     = Input.CategoryId;
        item.PricePerPiece  = Input.PricePerPiece;
        item.EstimatedHours = Input.EstimatedHours;
        item.IconClass      = Input.IconClass;
        item.SortOrder      = Input.SortOrder;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Service updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await _db.LaundryServices.FindAsync(id);
        if (item is not null)
        {
            _db.LaundryServices.Remove(item);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service deleted.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToPage();
        }

        var exists = await _db.ServiceCategories.AnyAsync(x => x.Name == categoryName.Trim());
        if (exists)
        {
            TempData["ErrorMessage"] = "Category already exists.";
            return RedirectToPage();
        }

        _db.ServiceCategories.Add(new ServiceCategory { Name = categoryName.Trim() });
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Category '{categoryName}' created.";
        return RedirectToPage();
    }
}
