using System.ComponentModel.DataAnnotations;
using eDhaq.Models.Enums;

namespace eDhaq.Models.Entities;

public class Driver
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [MaxLength(20)]
    public string? LicensePlate { get; set; }

    [MaxLength(50)]
    public string? VehicleModel { get; set; }

    public DriverStatus Status { get; set; } = DriverStatus.Offline;
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public decimal TotalEarnings { get; set; } = 0;
    public int CompletedDeliveries { get; set; } = 0;
    public double Rating { get; set; } = 0;
    public bool IsAvailable { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<DriverAssignment> Assignments { get; set; } = new List<DriverAssignment>();
}
