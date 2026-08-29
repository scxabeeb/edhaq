using System.ComponentModel.DataAnnotations;
using eDhaq.Common.DTOs;
using eDhaq.Models.Entities;

namespace eDhaq.Common.ViewModels;

public class CustomerDashboardViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal WalletBalance { get; set; }
    public List<OrderSummaryDto> RecentOrders { get; set; } = [];
    public List<Notification> Notifications { get; set; } = [];
}

public class ManageAddressViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Street { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? District { get; set; }

    [Required]
    public int CityId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
}
