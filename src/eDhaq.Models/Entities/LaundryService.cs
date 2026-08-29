using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eDhaq.Models.Entities;

public class LaundryService
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePerPiece { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PricePerKg { get; set; }

    public int EstimatedHours { get; set; } = 24;   // Standard turnaround
    public bool IsExpress { get; set; } = false;
    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? IconClass { get; set; }

    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
