using eDhaq.Common.Constants;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

/// <summary>
/// Admin API for listing users (used by the mobile admin screens).
/// </summary>
public class UsersController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// Lists users, optionally filtered by role and a free-text search.
    /// Pass role=Driver to get all pickup and delivery drivers.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Manager}", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<UserInfoDto>>> GetUsers(
        [FromQuery] string? role,
        [FromQuery] string? search)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(500)
            .ToListAsync();

        var result = new List<UserInfoDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(role))
            {
                var matches = role == "Driver"
                    ? roles.Contains(AppRoles.PickupDriver) || roles.Contains(AppRoles.DeliveryDriver)
                    : roles.Contains(role);

                if (!matches) continue;
            }

            result.Add(UserDtos.ToUserInfo(user, roles.ToList()));
        }

        return result;
    }
}
