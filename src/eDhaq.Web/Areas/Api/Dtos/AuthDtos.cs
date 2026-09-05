using eDhaq.Models.Entities;

namespace eDhaq.Web.Areas.Api.Dtos;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public int CityId { get; set; }
    public int VillageId { get; set; }
    public int? SubVillageId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string? District { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public int? CustomerId { get; set; }
    public decimal WalletBalance { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public UserInfoDto User { get; set; } = new();
}

public static class UserDtos
{
    public static UserInfoDto ToUserInfo(ApplicationUser user, List<string> roles)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles,
        };
    }
}
