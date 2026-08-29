using eDhaq.Common.DTOs;
using eDhaq.Models.Enums;

namespace eDhaq.Web.Areas.Api.Dtos;

public class PayOrderRequest
{
    /// <summary>
    /// Optional USSD transaction reference returned by the mobile-money
    /// operator after the customer dials *884*442628*amount#.
    /// </summary>
    public string? TransactionReference { get; set; }
}

public class OrderItemDto
{
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class OrderTrackingDto
{
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public string? UpdatedByName { get; set; }
    public decimal? DriverLatitude { get; set; }
    public decimal? DriverLongitude { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DriverAssignmentDto
{
    public int DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? VehicleModel { get; set; }
    public string? LicensePlate { get; set; }
    public bool IsPickup { get; set; }
    public DriverJobAction Status { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class AddressSummaryDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? CityName { get; set; }
    public string? VillageName { get; set; }
    public string? SubVillageName { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public DateTime PickupScheduledAt { get; set; }
    public DateTime? PickupActualAt { get; set; }
    public DateTime DeliveryScheduledAt { get; set; }
    public DateTime? DeliveryActualAt { get; set; }
    public DateTime EstimatedCompletionAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AddressSummaryDto? PickupAddress { get; set; }
    public AddressSummaryDto? DeliveryAddress { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderTrackingDto> Trackings { get; set; } = new();
    public List<DriverAssignmentDto> DriverAssignments { get; set; } = new();
    public string? QrCodeBase64 { get; set; }
    public string? BarcodeValue { get; set; }
}

public class PagedOrdersResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<OrderSummaryDto> Orders { get; set; } = new();
}

public class DriverAssignmentDetailDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPickup { get; set; }
    public DriverJobAction Action { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime? PickupScheduledAt { get; set; }
    public DateTime? DeliveryScheduledAt { get; set; }
    public DateTime? PickupActualAt { get; set; }
    public DateTime? DeliveryActualAt { get; set; }
    public string? PickupStreet { get; set; }
    public string? PickupCityName { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryCityName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public List<string> ServiceNames { get; set; } = new();
}

public class AssignDriverRequest
{
    public int OrderId { get; set; }
    public int DriverId { get; set; }
}
