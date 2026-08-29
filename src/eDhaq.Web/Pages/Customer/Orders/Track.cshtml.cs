using eDhaq.Services.Interfaces;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Customer.Orders;

[Authorize]
public class TrackModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public TrackModel(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    public sealed record ProgressStep(string Code, string Label);
    public sealed record TimelineItem(DateTime CreatedAt, string StatusCode, string StatusLabel, string? Note);
    public sealed record DriverInfo(string FullName, string? PhoneNumber, string? VehicleModel, string? LicensePlate, string AssignmentType, string AssignmentStatus);

    private static readonly List<ProgressStep> OrderedProgressSteps =
    [
        new("OrderPlaced", "Order Placed"),
        new("PickupScheduled", "Pickup Scheduled"),
        new("DriverAssigned", "Driver Assigned"),
        new("DriverOnTheWay", "Driver On The Way"),
        new("ClothesPickedUp", "Clothes Picked"),
        new("LaundryReceived", "Laundry Received"),
        new("Washing", "Washing"),
        new("Drying", "Drying"),
        new("Ironing", "Ironing"),
        new("Packaging", "Packing"),
        new("ReadyForDelivery", "Ready"),
        new("OutForDelivery", "Out For Delivery"),
        new("Delivered", "Delivered"),
        new("Completed", "Completed")
    ];

    public string OrderNumber { get; private set; } = string.Empty;
    public int OrderId { get; private set; }
    public string CurrentStatusCode { get; private set; } = string.Empty;
    public string CurrentStatusLabel { get; private set; } = string.Empty;
    public string GoogleMapsApiKey { get; private set; } = string.Empty;
    public List<TimelineItem> Timeline { get; private set; } = [];
    public IReadOnlyList<ProgressStep> ProgressSteps => OrderedProgressSteps;
    public int CurrentStepIndex { get; private set; }
    public bool CanConfirmDelivery { get; private set; }
    public DriverInfo? PickupDriver { get; private set; }
    public DriverInfo? DeliveryDriver { get; private set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var order = await _orderService.GetOrderDetailsAsync(orderId);
        if (order is null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        OrderNumber = order.OrderNumber;
        OrderId = order.Id;
        CurrentStatusCode = NormalizeToTimelineStatus(order.Status).ToString();
        CurrentStatusLabel = ToDisplayStatus(NormalizeToTimelineStatus(order.Status));
        CurrentStepIndex = GetProgressStepIndex(CurrentStatusCode);
        CanConfirmDelivery = order.Status == OrderStatus.Delivered;
        GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;
        Timeline = order.Trackings
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TimelineItem(
                x.CreatedAt,
                NormalizeToTimelineStatus(x.Status).ToString(),
                ToDisplayStatus(NormalizeToTimelineStatus(x.Status)),
                x.Note))
            .ToList();

        PickupDriver = order.DriverAssignments
            .Where(x => x.IsPickup)
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new DriverInfo(
                $"{x.Driver.User.FirstName} {x.Driver.User.LastName}".Trim(),
                x.Driver.User.PhoneNumber,
                x.Driver.VehicleModel,
                x.Driver.LicensePlate,
                "Pickup",
                x.Status.ToString()))
            .FirstOrDefault();

        DeliveryDriver = order.DriverAssignments
            .Where(x => !x.IsPickup)
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new DriverInfo(
                $"{x.Driver.User.FirstName} {x.Driver.User.LastName}".Trim(),
                x.Driver.User.PhoneNumber,
                x.Driver.VehicleModel,
                x.Driver.LicensePlate,
                "Delivery",
                x.Status.ToString()))
            .FirstOrDefault();

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int orderId)
    {
        var order = await _orderService.GetOrderDetailsAsync(orderId);
        if (order is null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Delivered)
        {
            TempData["ErrorMessage"] = "Order cannot be confirmed yet.";
            return RedirectToPage(new { orderId });
        }

        await _orderService.UpdateStatusAsync(new Common.DTOs.UpdateOrderStatusDto
        {
            OrderId = order.Id,
            Status = OrderStatus.CustomerConfirmed,
            Note = "Customer confirmed delivery"
        }, userId, User.Identity?.Name);

        await _orderService.UpdateStatusAsync(new Common.DTOs.UpdateOrderStatusDto
        {
            OrderId = order.Id,
            Status = OrderStatus.Completed,
            Note = "Order completed"
        }, userId, User.Identity?.Name);

        TempData["SuccessMessage"] = "Thank you. Your delivery was confirmed and order is completed.";
        return RedirectToPage(new { orderId });
    }

    private static int GetProgressStepIndex(string code)
    {
        var index = OrderedProgressSteps.FindIndex(x => x.Code == code);
        return index < 0 ? 0 : index;
    }

    private static OrderStatus NormalizeToTimelineStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.CustomerConfirmed => OrderStatus.Completed,
            OrderStatus.Sorting => OrderStatus.LaundryReceived,
            OrderStatus.DryCleaning => OrderStatus.Washing,
            OrderStatus.Folding => OrderStatus.Ironing,
            _ => status
        };
    }

    private static string ToDisplayStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.OrderPlaced => "Order Placed",
            OrderStatus.PickupScheduled => "Pickup Scheduled",
            OrderStatus.DriverAssigned => "Driver Assigned",
            OrderStatus.DriverOnTheWay => "Driver On The Way",
            OrderStatus.ClothesPickedUp => "Clothes Picked",
            OrderStatus.LaundryReceived => "Laundry Received",
            OrderStatus.Washing => "Washing",
            OrderStatus.Drying => "Drying",
            OrderStatus.Ironing => "Ironing",
            OrderStatus.Packaging => "Packing",
            OrderStatus.ReadyForDelivery => "Ready",
            OrderStatus.OutForDelivery => "Out For Delivery",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Completed => "Completed",
            OrderStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
    }
}
