using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class Review
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int Rating { get; set; }  // 1-5

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
