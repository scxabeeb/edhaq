using System.ComponentModel.DataAnnotations;
using eDhaq.Models.Enums;

namespace eDhaq.Models.Entities;

public class DriverAssignment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    /// <summary>Pickup = true, Delivery = false</summary>
    public bool IsPickup { get; set; }

    public DriverJobAction Status { get; set; } = DriverJobAction.Pending;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(255)]
    public string? DeliveryPhotoUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
