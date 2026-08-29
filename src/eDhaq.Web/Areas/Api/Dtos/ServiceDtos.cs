namespace eDhaq.Web.Areas.Api.Dtos;

public class ServiceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal PricePerPiece { get; set; }
    public decimal? PricePerKg { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsExpress { get; set; }
    public bool IsActive { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
}
