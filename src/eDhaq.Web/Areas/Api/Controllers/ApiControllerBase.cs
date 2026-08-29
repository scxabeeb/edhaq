using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eDhaq.Web.Areas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string? GetCurrentUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    protected string? GetCurrentUserRole(string role)
    {
        var roleClaim = User.FindFirst("role");
        return roleClaim?.Value;
    }
}
