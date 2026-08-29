using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using eDhaq.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace eDhaq.Web.Core.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<TokenResponse> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"]
                     ?? throw new InvalidOperationException("JWT secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresInMinutes = int.TryParse(jwtSettings["ExpiryInMinutes"], out var mins) ? mins : 43200;
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("is_active", user.IsActive ? "true" : "false"),
        };

        foreach (var role in roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
            claims.Add(new("role", role));
        }

        var issuer = jwtSettings["Issuer"] ?? string.Empty;
        var audience = jwtSettings["Audience"] ?? string.Empty;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresOn.DateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Non-async work is wrapped in Task to satisfy the async contract / allow future async work.
        await Task.CompletedTask;

        return new TokenResponse(accessToken, expiresOn, roles.ToList());
    }
}
