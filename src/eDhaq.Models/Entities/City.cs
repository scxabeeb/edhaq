using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class City
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Country { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Village> Villages { get; set; } = new List<Village>();
}
