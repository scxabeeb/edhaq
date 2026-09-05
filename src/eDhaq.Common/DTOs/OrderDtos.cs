using System.ComponentModel.DataAnnotations;
using eDhaq.Models.Enums;

namespace eDhaq.Common.DTOs;

public class CreateOrderDto
{
    [Required]
    public int PickupAddressId { get; set; }

    [Required]
    public int DeliveryAddressId { get; set; }

    [Required]
    public DateTime PickupScheduledAt { get; set; }

    [Required]
    public DateTime DeliveryScheduledAt { get; set; }

    [Required]
    public List<CreateOrderItemDto> Items { get; set; } = [];

    public string? CouponCode { get; set; }
    public string? SpecialInstructions { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
}

public class CreateOrderItemDto
{
    [Required]
    public int ServiceId { get; set; }

    [Range(1, 500)]
    public int Quantity { get; set; }

    public string? Notes { get; set; }
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
        public DateTime EstimatedCompletionAt { get; set; }
    public string? CustomerName { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public OrderStatus Status { get; set; }

    public string? Note { get; set; }
}
