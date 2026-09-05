using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Driver.Assignments;

[Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orderService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public IndexModel(AppDbContext db, IOrderService orderService, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _db = db;
        _orderService = orderService;
        _environment = environment;
        _configuration = configuration;
    }

        public List<DriverAssignment> Assignments { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageSize { get; } = 15;
    public bool IsAdmin => User?.IsInRole("Administrator") ?? false;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public DriverJobAction? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsPickupFilter { get; set; }

    [BindProperty]
    public int AssignmentId { get; set; }

    [BindProperty]
    public IFormFile? DeliveryPhoto { get; set; }

        public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var isAdmin = User.IsInRole("Administrator");
        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);

        if (driver is null)
        {
            if (isAdmin)
            {
                // Administrators see ALL assignments across all drivers.
                // No Driver profile is needed (and should not be auto-created).
            }
            else if (User.IsInRole("PickupDriver") || User.IsInRole("DeliveryDriver"))
            {
                // Auto-create a Driver profile if the user has a driver role
                // but no Driver entity exists yet.
                driver = new eDhaq.Models.Entities.Driver
                {
                    UserId = userId,
                    Status = DriverStatus.Offline,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Drivers.Add(driver);
                await _db.SaveChangesAsync();
            }
            else
            {
                return;
            }
        }

        if (driver is null && !isAdmin)
        {
            return;
        }

                var query = _db.DriverAssignments
            .Include(x => x.Order)
            .ThenInclude(x => x.Customer)
            .ThenInclude(x => x.User)
            .Include(x => x.Order)
            .ThenInclude(x => x.Items)
            .ThenInclude(i => i.Service)
            .Include(x => x.Driver)
            .ThenInclude(x => x.User)
            .AsQueryable();

        // Administrators see all assignments; regular drivers see only their own.
        if (!isAdmin)
        {
            query = query.Where(x => x.DriverId == driver!.Id);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(x => x.Order.OrderNumber.Contains(search)
                                     || x.Order.Customer.User.FirstName.Contains(search)
                                     || x.Order.Customer.User.LastName.Contains(search)
                                     || x.Order.Customer.User.Email!.Contains(search));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == StatusFilter.Value);
        }

        if (IsPickupFilter.HasValue)
        {
            query = query.Where(x => x.IsPickup == IsPickupFilter.Value);
        }

        TotalCount = await query.CountAsync();

        Assignments = await query
            .OrderByDescending(x => x.AssignedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAcceptAsync()
    {
        return await UpdateAssignmentStatusAsync(DriverJobAction.Accepted, addTrackingNote: true);
    }

    public async Task<IActionResult> OnPostStartTripAsync()
    {
        var assignment = await LoadAssignmentForCurrentUserAsync();
        if (assignment is null)
        {
            return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
        }

        if (!assignment.IsPickup && !assignment.Order.PickupActualAt.HasValue)
        {
            TempData["ErrorMessage"] = "Pickup must be completed before delivery can start.";
            return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
        }

        var note = assignment.IsPickup ? "Driver is on the way to pickup." : "Driver is on the way to deliver.";
        var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = assignment.OrderId,
            Status = assignment.IsPickup ? OrderStatus.DriverOnTheWay : OrderStatus.OutForDelivery,
            Note = note
        }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);

        if (!updated)
        {
            TempData["ErrorMessage"] = "Could not move order to the selected trip stage.";
            return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
        }

        assignment.Status = DriverJobAction.Accepted;
        assignment.AcceptedAt ??= DateTime.UtcNow;
        assignment.Notes = note;

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = assignment.IsPickup ? "Pickup marked as on the way." : "Delivery marked as on the way.";
        return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        return await UpdateAssignmentStatusAsync(DriverJobAction.Rejected, addTrackingNote: true);
    }

    public async Task<IActionResult> OnPostCompleteAsync()
    {
        return await UpdateAssignmentStatusAsync(DriverJobAction.Completed);
    }

    public bool CanStartTrip(DriverAssignment assignment)
        => assignment.Status == DriverJobAction.Accepted || assignment.Status == DriverJobAction.Pending;

    public string GetCompletionLabel(DriverAssignment assignment)
        => assignment.IsPickup ? "Picked Up" : "Delivered";

    /// <summary>
    /// A delivery can only be completed once the pickup phase is done.
    /// Pickup is considered complete when PickupActualAt is set, or when the
    /// order has already advanced past the pickup stage in the workflow
    /// (LaundryReceived onward) — which implies the clothes were picked up.
    /// </summary>
    private static bool IsPickupComplete(eDhaq.Models.Entities.Order order)
    {
        if (order.PickupActualAt.HasValue)
        {
            return true;
        }

        return order.Status switch
        {
            OrderStatus.LaundryReceived or OrderStatus.Sorting or OrderStatus.Washing or
            OrderStatus.DryCleaning or OrderStatus.Drying or OrderStatus.Ironing or
            OrderStatus.Folding or OrderStatus.Packaging or OrderStatus.ReadyForDelivery or
            OrderStatus.OutForDelivery or OrderStatus.Delivered or OrderStatus.Completed or
            OrderStatus.CustomerConfirmed => true,
            _ => false
        };
    }

    private async Task<IActionResult> UpdateAssignmentStatusAsync(DriverJobAction action, bool addTrackingNote = false)
    {
        var assignment = await LoadAssignmentForCurrentUserAsync();

        if (assignment is null)
        {
            return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
        }

        assignment.Status = action;
        if (action == DriverJobAction.Accepted)
        {
            assignment.AcceptedAt = DateTime.UtcNow;
            assignment.Notes = assignment.IsPickup ? "Pickup driver accepted assignment." : "Delivery driver accepted assignment.";
            if (addTrackingNote)
            {
                await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
                {
                    OrderId = assignment.OrderId,
                    Status = assignment.Order.Status,
                    Note = assignment.Notes
                }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);
            }
        }

        if (action == DriverJobAction.Rejected)
        {
            assignment.Notes = assignment.IsPickup ? "Pickup driver rejected assignment." : "Delivery driver rejected assignment.";
            if (addTrackingNote)
            {
                await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
                {
                    OrderId = assignment.OrderId,
                    Status = assignment.Order.Status,
                    Note = assignment.Notes
                }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);
            }
        }

        if (action == DriverJobAction.Completed)
        {
            if (!assignment.IsPickup && !IsPickupComplete(assignment.Order))
            {
                TempData["ErrorMessage"] = $"Pickup for order {assignment.Order.OrderNumber} must be completed before delivery can be marked delivered.";
                return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
            }

            var note = assignment.IsPickup ? "Pickup completed by driver." : "Delivery completed by driver.";
            var updated = await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = assignment.IsPickup ? OrderStatus.ClothesPickedUp : OrderStatus.Delivered,
                Note = note
            }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name);

            if (!updated)
            {
                TempData["ErrorMessage"] = "Could not complete this task due to current order stage.";
                return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
            }

            assignment.CompletedAt = DateTime.UtcNow;
            assignment.Notes = note;

            if (!assignment.IsPickup && DeliveryPhoto is not null && DeliveryPhoto.Length > 0)
            {
                var maxMb = _configuration.GetValue<int?>("FileUpload:DeliveryProofMaxMb") ?? 3;
                var maxBytes = maxMb * 1024 * 1024;
                var allowedExtensions = _configuration
                    .GetSection("FileUpload:AllowedImageExtensions")
                    .Get<string[]>() ?? [".jpg", ".jpeg", ".png", ".webp"];

                var extension = Path.GetExtension(DeliveryPhoto.FileName).ToLowerInvariant();
                var isValidExtension = allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
                var isValidMime = !string.IsNullOrWhiteSpace(DeliveryPhoto.ContentType)
                                  && DeliveryPhoto.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

                if (!isValidExtension || !isValidMime)
                {
                    TempData["ErrorMessage"] = "Only image files (.jpg, .jpeg, .png, .webp) are allowed.";
                    return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
                }

                if (DeliveryPhoto.Length > maxBytes)
                {
                    TempData["ErrorMessage"] = $"Delivery proof exceeds maximum size of {maxMb} MB.";
                    return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
                }

                var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "delivery-proof");
                Directory.CreateDirectory(uploadFolder);

                var fileName = $"proof-{assignment.Id}-{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadFolder, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await DeliveryPhoto.CopyToAsync(stream);

                assignment.DeliveryPhotoUrl = $"/uploads/delivery-proof/{fileName}";
            }

            if (assignment.IsPickup)
            {
                assignment.Order.PickupActualAt = DateTime.UtcNow;
            }
            else
            {
                assignment.Order.DeliveryActualAt = DateTime.UtcNow;
                // If pickup completed implicitly (order already advanced past
                // the pickup stage), back-fill the pickup timestamp.
                assignment.Order.PickupActualAt ??= DateTime.UtcNow;
            }

        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Assignment marked as {action}.";
        return RedirectToPage(new { Search, StatusFilter, IsPickupFilter, PageNumber });
    }

    private async Task<DriverAssignment?> LoadAssignmentForCurrentUserAsync()
    {
        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == AssignmentId);

        if (assignment is null)
        {
            return null;
        }

        if (User.IsInRole("Administrator"))
        {
            return assignment;
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (driver is null || driver.Id != assignment.DriverId)
        {
            return null;
        }

        return assignment;
    }
}
