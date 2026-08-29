using System.ComponentModel.DataAnnotations;

namespace eDhaq.Models.Entities;

public class Customer
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [MaxLength(100)]
    public string? AlternatePhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal WalletBalance { get; set; } = 0;
    public string? ReferralCode { get; set; }
    public string? ReferredByCode { get; set; }
    public int TotalOrders { get; set; } = 0;
    public decimal TotalSpent { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
