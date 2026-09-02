using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eDhaq.Data;
using eDhaq.Data.Seed;
using eDhaq.Models.Entities;
using eDhaq.Repositories;
using eDhaq.Services;
using eDhaq.Services.Hubs;
using eDhaq.Web.Core.Services;
using eDhaq.Web.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/edhaq-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDataLayer(builder.Configuration);

// Persist Data Protection keys to the database so antiforgery tokens,
// cookies, and auth tickets survive app restarts / redeployments.
builder.Services.AddDataProtection()
    .SetApplicationName("eDhaq")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRepositoryLayer();
builder.Services.AddServiceLayer();

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

// Mobile API: JWT Bearer authentication
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret is not configured.");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

// Cookie auth default for Razor pages (registered by AddDefaultIdentity) +
// JWT Bearer for the mobile API.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings["Issuer"] ?? string.Empty,
        ValidAudience = jwtSettings["Audience"] ?? string.Empty,
        IssuerSigningKey = key,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier,
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdministratorRole");
    options.Conventions.AuthorizeFolder("/Manager", "RequireManagerRole");
    options.Conventions.AuthorizeFolder("/Driver", "RequireDriverRole");
    options.Conventions.AuthorizeFolder("/Customer", "RequireCustomerRole");
    options.Conventions.AuthorizeFolder("/Staff", "RequireLaundryStaffRole");
    options.Conventions.AuthorizeFolder("/Cashier", "RequireCashierRole");
    options.Conventions.AuthorizeFolder("/Finance", "RequireFinanceRole");
    options.Conventions.AllowAnonymousToFolder("/Auth");
    options.Conventions.AllowAnonymousToPage("/Index");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
    options.AddPolicy("RequireCashierRole", policy => policy.RequireRole("Administrator", "Cashier"));
    options.AddPolicy("RequireDriverRole", policy => policy.RequireRole("Administrator", "PickupDriver", "DeliveryDriver"));
    options.AddPolicy("RequireCustomerRole", policy => policy.RequireRole("Administrator", "Customer"));
    options.AddPolicy("RequireLaundryStaffRole", policy => policy.RequireRole("Administrator", "LaundryStaff"));
    options.AddPolicy("RequireFinanceRole", policy => policy.RequireRole("Administrator", "Manager", "Cashier"));
});

var app = builder.Build();

// API-first: JSON on auth failures so mobile clients can read errors cleanly.
app.UseStatusCodePagesWithReExecute("/api/errors/{0}");

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<TrackingHub>("/hubs/tracking");

try
{
    await DbInitializer.SeedAsync(app.Services);
}
catch (Exception ex)
{
    Log.Error(ex, "Database seeding failed during startup.");

    if (!app.Environment.IsDevelopment())
    {
        throw;
    }
}

app.Run();