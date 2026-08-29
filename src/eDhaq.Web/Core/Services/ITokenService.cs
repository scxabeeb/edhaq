using eDhaq.Models.Entities;

namespace eDhaq.Web.Core.Services;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
}

public sealed record TokenResponse(string Token, DateTimeOffset ExpiresOn, IList<string> Roles);
