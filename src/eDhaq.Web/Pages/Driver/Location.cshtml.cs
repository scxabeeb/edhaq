using eDhaq.Data;
using eDhaq.Services.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Driver;

[Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
[IgnoreAntiforgeryToken]
public class LocationModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IHubContext<TrackingHub> _hubContext;

    public LocationModel(AppDbContext db, IHubContext<TrackingHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    public IActionResult OnGet()
    {
        return NotFound();
    }

    public async Task<IActionResult> OnPostAsync([FromBody] LocationRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (driver is null)
        {
            return NotFound();
        }

        driver.CurrentLatitude = request.Latitude;
        driver.CurrentLongitude = request.Longitude;
        driver.LastLocationUpdate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            await _hubContext.Clients.Group($"order-{request.OrderNumber}").SendAsync("driverLocationUpdated", new
            {
                orderNumber = request.OrderNumber,
                latitude = request.Latitude,
                longitude = request.Longitude,
                updatedAt = DateTime.UtcNow
            });
        }

        return new JsonResult(new { success = true });
    }

    public class LocationRequest
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? OrderNumber { get; set; }
    }
}
