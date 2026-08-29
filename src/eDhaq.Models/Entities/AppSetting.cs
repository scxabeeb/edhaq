using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class AppSetting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Value { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
