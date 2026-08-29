using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class Address
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;   // Home, Office, etc.

    [Required, MaxLength(255)]
    public string Street { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? District { get; set; }

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;

    public int? SubVillageId { get; set; }
    public SubVillage? SubVillage { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
