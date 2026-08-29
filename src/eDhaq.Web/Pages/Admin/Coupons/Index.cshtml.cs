using eDhaq.Data;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Coupons;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Coupon> Coupons { get; private set; } = [];

    [BindProperty]
    public Coupon Input { get; set; } = new()
    {
        ValidFrom = DateTime.UtcNow,
        ValidTo = DateTime.UtcNow.AddMonths(1),
        IsActive = true,
        IsPercentage = true
    };

    public async Task OnGetAsync()
    {
        Coupons = await _db.Coupons.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.Code = Input.Code.Trim().ToUpperInvariant();
        Input.CreatedAt = DateTime.UtcNow;
        _db.Coupons.Add(Input);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Coupon created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id);
        if (coupon is not null)
        {
            coupon.IsActive = !coupon.IsActive;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
