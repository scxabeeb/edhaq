using eDhaq.Models.Entities;

namespace eDhaq.Services.Interfaces;

public interface ICustomerProfileService
{
    /// <summary>
    /// Gets the customer profile for a user, creating one (plus a wallet) if
    /// the account was created without a linked customer profile (e.g. accounts
    /// seeded before the register flow created them). Returns null if the user
    /// does not exist.
    /// </summary>
    Task<Customer?> EnsureCustomerAsync(string userId);
}