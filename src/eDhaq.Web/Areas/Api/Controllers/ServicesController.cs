using eDhaq.Data;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class ServicesController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetCategories()
    {
        var categories = await _db.ServiceCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ServiceCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IconClass = x.IconClass,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetServices([FromQuery] int? categoryId = null)
    {
        var query = _db.LaundryServices
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var services = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ServiceDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                PricePerPiece = x.PricePerPiece,
                PricePerKg = x.PricePerKg,
                EstimatedHours = x.EstimatedHours,
                IsExpress = x.IsExpress,
                IsActive = x.IsActive,
                IconClass = x.IconClass,
                SortOrder = x.SortOrder
            })
            .ToListAsync();

        return Ok(services);
    }
}
