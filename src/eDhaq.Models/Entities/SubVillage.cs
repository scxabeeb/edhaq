using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class SubVillage
{
    public int Id { get; set; }

    public int VillageId { get; set; }
    public Village Village { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
