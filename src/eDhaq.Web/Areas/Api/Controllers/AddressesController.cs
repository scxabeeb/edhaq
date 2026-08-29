using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class AddressesController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public AddressesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> GetAddresses()
    {
        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return Forbid();
        }

        var addresses = await _db.Addresses
            .Where(x => x.CustomerId == customer.Id)
            .Include(x => x.City)
            .Include(x => x.Village)
            .Include(x => x.SubVillage)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Label)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(addresses);
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> CreateAddress([FromBody] CreateAddressRequest request)
    {
        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return Forbid();
        }

        var villageValid = await _db.Villages
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.VillageId && x.CityId == request.CityId && x.IsActive);

        if (!villageValid)
        {
            return BadRequest(new ProblemDetails { Title = "Please select a valid village." });
        }

        if (request.SubVillageId.HasValue)
        {
            var subVillageValid = await _db.SubVillages
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.SubVillageId.Value && x.VillageId == request.VillageId && x.IsActive);

            if (!subVillageValid)
            {
                return BadRequest(new ProblemDetails { Title = "Please select a valid sub-village." });
            }
        }

        if (string.IsNullOrWhiteSpace(request.Street))
        {
            return BadRequest(new ProblemDetails { Title = "Street is required." });
        }

        if (request.IsDefault)
        {
            var existing = await _db.Addresses
                .Where(x => x.CustomerId == customer.Id && x.IsDefault)
                .ToListAsync();

            foreach (var e in existing)
            {
                e.IsDefault = false;
            }
        }

        var address = new Address
        {
            CustomerId = customer.Id,
            Label = string.IsNullOrWhiteSpace(request.Label) ? "Home" : request.Label.Trim(),
            Street = request.Street.Trim(),
            District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
            CityId = request.CityId,
            VillageId = request.VillageId,
            SubVillageId = request.SubVillageId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        await _db.Entry(address)
            .Reference(x => x.City)
            .LoadAsync();
        await _db.Entry(address)
            .Reference(x => x.Village)
            .LoadAsync();

        return CreatedAtAction(nameof(GetAddresses), null, ToDto(address));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return Forbid();
        }

        var address = await _db.Addresses
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customer.Id);

        if (address is null)
        {
            return NotFound(new ProblemDetails { Title = "Address not found." });
        }

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<Customer?> GetCustomerAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return customer;
    }

    private static AddressDto ToDto(Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            Label = address.Label,
            Street = address.Street,
            District = address.District,
            CityId = address.CityId,
            CityName = address.City?.Name,
            VillageId = address.VillageId,
            VillageName = address.Village?.Name,
            SubVillageId = address.SubVillageId,
            SubVillageName = address.SubVillage?.Name,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            IsDefault = address.IsDefault
        };
    }
}
