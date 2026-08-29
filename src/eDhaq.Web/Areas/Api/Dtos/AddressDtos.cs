namespace eDhaq.Web.Areas.Api.Dtos;

public class AddressDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? District { get; set; }
    public int CityId { get; set; }
    public string? CityName { get; set; }
    public int VillageId { get; set; }
    public string? VillageName { get; set; }
    public int? SubVillageId { get; set; }
    public string? SubVillageName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateAddressRequest
{
    public string Label { get; set; } = "Home";
    public string Street { get; set; } = string.Empty;
    public string? District { get; set; }
    public int CityId { get; set; }
    public int VillageId { get; set; }
    public int? SubVillageId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; } = false;
}
