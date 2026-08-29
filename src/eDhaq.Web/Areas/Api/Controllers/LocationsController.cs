using eDhaq.Data;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

[AllowAnonymous]
public class LocationsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public LocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("cities")]
    public async Task<ActionResult<List<CityDto>>> GetCities()
    {
        var cities = await _db.Cities
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CityDto { Id = x.Id, Name = x.Name, Country = x.Country, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(cities);
    }

    [HttpGet("cities/{cityId:int}/villages")]
    public async Task<ActionResult<List<VillageDto>>> GetVillages(int cityId)
    {
        var exists = await _db.Cities.AnyAsync(x => x.Id == cityId && x.IsActive);
        if (!exists)
        {
            return NotFound(new ProblemDetails { Title = "City not found." });
        }

        var villages = await _db.Villages
            .Where(x => x.CityId == cityId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new VillageDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(villages);
    }

    [HttpGet("villages/{villageId:int}/subvillages")]
    public async Task<ActionResult<List<SubVillageDto>>> GetSubVillages(int villageId)
    {
        var exists = await _db.Villages.AnyAsync(x => x.Id == villageId && x.IsActive);
        if (!exists)
        {
            return NotFound(new ProblemDetails { Title = "Village not found." });
        }

        var subVillages = await _db.SubVillages
            .Where(x => x.VillageId == villageId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SubVillageDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(subVillages);
    }
}
