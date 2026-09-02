using System;
using System.Linq;
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
                Categories = await _db.ServiceCategories
            .Include(c => c.Services)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        CategoryOptions = Categories.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        Services = await _db.LaundryServices.Include(x => x.Category).OrderBy(x => x.SortOrder).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // Ensure supporting data is loaded so the modal can re-render on error.
        await OnGetAsync();

        // Log ModelState errors for debugging
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => $"{e.ErrorMessage}{(e.Exception != null ? $" | Exception: {e.Exception.Message}" : "")}"));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            await OnGetAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Service name is required.");
        }
        else if (await _db.LaundryServices.AnyAsync(s => s.Name == Input.Name.Trim()))
        {
            ModelState.AddModelError("Input.Name", "A service with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        // Validate category association (prevents silent FK failures).
        var categoryExists = await _db.ServiceCategories.AnyAsync(c => c.Id == Input.CategoryId);
        if (!categoryExists)
        {
            ModelState.AddModelError("Input.CategoryId", "Please select a valid category.");
            await OnGetAsync();
            return Page();
        }

        try
        {
            Input.CreatedAt = DateTime.UtcNow;
            Input.Name = Input.Name.Trim();
            _db.LaundryServices.Add(Input);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service added.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Unable to create service: {ex.Message}";
        }

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

    public async Task<IActionResult> OnPostEditCategoryAsync(int id, string name)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid category.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToPage();
        }

        var item = await _db.ServiceCategories.FindAsync(id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage();
        }

        name = name.Trim();
        if (await _db.ServiceCategories.AnyAsync(c => c.Name == name && c.Id != id))
        {
            TempData["ErrorMessage"] = "Another category with this name already exists.";
            return RedirectToPage();
        }

        item.Name = name;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Category updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid category.";
            return RedirectToPage();
        }

        var item = await _db.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage();
        }

        if (item.Services.Count > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete '{item.Name}' because {item.Services.Count} service(s) are assigned to it. Reassign or delete the services first.";
            return RedirectToPage();
        }

        _db.ServiceCategories.Remove(item);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Category '{item.Name}' deleted.";
        return RedirectToPage();
    }
}
