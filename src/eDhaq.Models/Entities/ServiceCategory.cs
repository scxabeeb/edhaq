using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class ServiceCategory
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? IconClass { get; set; }  // Bootstrap icon class

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<LaundryService> Services { get; set; } = new List<LaundryService>();
}
