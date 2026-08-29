using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eDhaq.Models.Entities;

public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Percentage (0-100) or fixed amount</summary>
    public bool IsPercentage { get; set; } = true;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumOrderAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; } = 0;
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
