using System.ComponentModel.DataAnnotations;
using eDhaq.Models.Enums;

namespace eDhaq.Models.Entities;

public class OrderTracking
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus Status { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public string? UpdatedByUserId { get; set; }

    [MaxLength(100)]
    public string? UpdatedByName { get; set; }

    public decimal? DriverLatitude { get; set; }
    public decimal? DriverLongitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
