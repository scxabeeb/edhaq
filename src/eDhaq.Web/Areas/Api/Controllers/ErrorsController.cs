using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eDhaq.Web.Areas.Api.Controllers;

/// <summary>
/// Renders a JSON problem details response for status-code errors
/// (e.g. 401/403 from failed JWT challenges) so mobile clients can read the message.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class ErrorsController : ControllerBase
{
    [HttpGet("{statusCode:int}")]
    public IActionResult Error(int statusCode)
    {
        var problem = statusCode switch
        {
            401 => new ProblemDetails { Status = 401, Title = "Unauthorized", Detail = "Authentication is required to access this resource." },
            403 => new ProblemDetails { Status = 403, Title = "Forbidden", Detail = "You do not have permission to access this resource." },
            404 => new ProblemDetails { Status = 404, Title = "Not Found", Detail = "The requested resource was not found." },
            _ => new ProblemDetails { Status = statusCode, Title = "Error", Detail = "An unexpected error occurred." }
        };

        return StatusCode(statusCode, problem);
    }
}
