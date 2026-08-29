using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eDhaq.Models.Enums;

namespace eDhaq.Models.Entities;

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;  // EDQ-2026-000001

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Pickup
    public int PickupAddressId { get; set; }
    public Address PickupAddress { get; set; } = null!;
    public DateTime PickupScheduledAt { get; set; }
    public DateTime? PickupActualAt { get; set; }

    // Delivery
    public int DeliveryAddressId { get; set; }
    public Address DeliveryAddress { get; set; } = null!;
    public DateTime DeliveryScheduledAt { get; set; }
    public DateTime? DeliveryActualAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.OrderPlaced;

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DeliveryFee { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    public string? QrCodeBase64 { get; set; }
    public string? BarcodeValue { get; set; }

    public DateTime EstimatedCompletionAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderTracking> Trackings { get; set; } = new List<OrderTracking>();
    public ICollection<DriverAssignment> DriverAssignments { get; set; } = new List<DriverAssignment>();
    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }
    public Review? Review { get; set; }
}
