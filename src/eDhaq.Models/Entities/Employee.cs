using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class Employee
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Department { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
