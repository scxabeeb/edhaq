using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class Village
{
    public int Id { get; set; }

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<SubVillage> SubVillages { get; set; } = new List<SubVillage>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
