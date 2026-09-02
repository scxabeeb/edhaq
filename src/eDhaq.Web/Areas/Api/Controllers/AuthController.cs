using eDhaq.Common.Constants;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Web.Areas.Api.Dtos;
using eDhaq.Web.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new ProblemDetails { Title = "User is not active." });
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var tokenResult = await _tokenService.GenerateTokenAsync(user, roles);

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var userInfo = UserDtos.ToUserInfo(user, roles);
        userInfo.CustomerId = customer?.Id;
        userInfo.WalletBalance = customer?.WalletBalance ?? 0;

        return Ok(new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresOn = tokenResult.ExpiresOn,
            User = userInfo
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new ProblemDetails { Title = "Passwords do not match." });
        }

        // Validate location hierarchy (mirrors Register.cshtml.cs).
        var selectedVillage = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.VillageId && x.CityId == request.CityId && x.IsActive);

        if (selectedVillage is null)
        {
            return BadRequest(new ProblemDetails { Title = "Please select a valid village." });
        }

        if (request.SubVillageId.HasValue)
        {
            var subVillageValid = await _db.SubVillages
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.SubVillageId.Value && x.VillageId == request.VillageId && x.IsActive);

            if (!subVillageValid)
            {
                return BadRequest(new ProblemDetails { Title = "Please select a valid sub-village." });
            }
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unable to create account.",
                Detail = string.Join(" | ", createResult.Errors.Select(e => e.Description))
            });
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Customer);

        var customer = new Customer
        {
            UserId = user.Id,
            ReferralCode = $"EDQ{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        // Save first so the DB-generated Customer.Id is populated
        // (Addresses/Wallets have an FK to Customers.Id).
        await _db.SaveChangesAsync();

        _db.Addresses.Add(new Address
        {
            CustomerId = customer.Id,
            Label = "Home",
            Street = request.Street,
            District = request.District,
            CityId = request.CityId,
            VillageId = request.VillageId,
            SubVillageId = request.SubVillageId,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        _db.Wallets.Add(new Wallet
        {
            CustomerId = customer.Id,
            Balance = 0,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var tokenResult = await _tokenService.GenerateTokenAsync(user, roles);

        await _db.Entry(customer).Reference(x => x.User).LoadAsync();
        var userInfo = UserDtos.ToUserInfo(user, roles);
        userInfo.CustomerId = customer.Id;
        userInfo.WalletBalance = customer.WalletBalance;

        return Ok(new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresOn = tokenResult.ExpiresOn,
            User = userInfo
        });
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<UserInfoDto>> Me()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ProblemDetails { Title = "Authentication is required." });
        }

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return NotFound(new ProblemDetails { Title = "User not found." });
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

        var userInfo = UserDtos.ToUserInfo(user, roles);
        userInfo.CustomerId = customer?.Id;
        userInfo.WalletBalance = customer?.WalletBalance ?? 0;

        return Ok(userInfo);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        // JWT is stateless; nothing to revoke on the server side for now.
        await Task.CompletedTask;
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CityDto>>> GetCities()
    {
        var cities = await _db.Cities
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CityDto { Id = x.Id, Name = x.Name, Country = x.Country, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(cities);
    }

    [HttpGet("cities/{cityId:int}/villages")]
    [AllowAnonymous]
    public async Task<ActionResult<List<VillageDto>>> GetVillages(int cityId)
    {
        var villages = await _db.Villages
            .Where(x => x.CityId == cityId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new VillageDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(villages);
    }

    [HttpGet("villages/{villageId:int}/subvillages")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubVillageDto>>> GetSubVillages(int villageId)
    {
        var subVillages = await _db.SubVillages
            .Where(x => x.VillageId == villageId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SubVillageDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive })
            .ToListAsync();

        return Ok(subVillages);
    }
}
