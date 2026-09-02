using System.ComponentModel.DataAnnotations;
using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Services;

public class ServiceInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Service name is required.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "Please enter a valid price.")]
    public decimal PricePerPiece { get; set; }

    [Range(0, 100000)]
    public int EstimatedHours { get; set; } = 24;

    [MaxLength(100)]
    public string? IconClass { get; set; }

    public int SortOrder { get; set; }
}

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

    // Pagination
    public int PageNumber { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;   // 0 = All
    public int TotalCount { get; private set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageSize > 0 && PageNumber < TotalPages;

    [BindProperty]
    public ServiceInputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Read paging from the query string only. Use "pageNumber" (not the reserved
        // "page" route param) so pagination links keep the value and ModelState is
        // never polluted by the page route value on POST.
        int? page = null, size = null;
        if (int.TryParse(Request.Query["pageNumber"], out var p)) page = p;
        if (int.TryParse(Request.Query["size"], out var s)) size = s;

        Categories = await _db.ServiceCategories
            .Include(c => c.Services)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        CategoryOptions = Categories.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        // Page size: 20/50/100/200 or 0 = All (default 20)
        PageSize = size is null ? 20 : (new[] { 0, 20, 50, 100, 200 }.Contains(size.Value) ? size.Value : 20);
        PageNumber = Math.Max(1, page ?? 1);

        var all = await _db.LaundryServices.Include(x => x.Category).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        TotalCount = all.Count;

        if (PageSize <= 0)
        {
            Services = all;
        }
        else
        {
            var totalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (PageNumber > totalPages && totalPages > 0) PageNumber = totalPages;
            Services = all.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
        }
    }
public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var categoryExists = await _db.ServiceCategories.AnyAsync(c => c.Id == Input.CategoryId);
        if (!categoryExists)
        {
            ModelState.AddModelError("Input.CategoryId", "Please select a valid category.");
            await OnGetAsync();
            return Page();
        }

        try
        {
            var service = new LaundryService
            {
                Name = Input.Name.Trim(),
                CategoryId = Input.CategoryId,
                PricePerPiece = Input.PricePerPiece,
                EstimatedHours = Input.EstimatedHours,
                IconClass = Input.IconClass,
                SortOrder = Input.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            _db.LaundryServices.Add(service);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service added.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Unable to create service: {ex.Message}";
        }

        // Redirect to the last page so the newly added service (default sort) is visible.
        var total = await _db.LaundryServices.CountAsync();
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / 20.0));
        return RedirectToPage(new { pageNumber = lastPage, size = 20 });
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
        item.Name           = Input.Name.Trim();
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
