using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

/// <summary>
/// Exposes the public support-contact details (stored as public AppSettings)
/// so the mobile apps can render the "Contact Us" screen dynamically.
/// </summary>
public class ContactsController : ApiControllerBase
{
    public const string PhoneKey = "Support.Phone";
    public const string WhatsappKey = "Support.Whatsapp";
    public const string EmailKey = "Support.Email";
    public const string WorkingHoursKey = "Support.WorkingHours";

    private readonly AppDbContext _db;

    public ContactsController(AppDbContext db)
    {
        _db = db;
    }

    // Public: any customer (even logged out) can see how to reach staff.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<SupportContactsDto>> GetContacts()
    {
        var settings = await _db.AppSettings
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return Ok(new SupportContactsDto
        {
            Phone = GetValue(settings, PhoneKey, "+967771234567"),
            Whatsapp = GetValue(settings, WhatsappKey, "+967771234567"),
            Email = GetValue(settings, EmailKey, "support@edhaq.com"),
            WorkingHours = GetValue(settings, WorkingHoursKey, "Sat - Thu, 8:00 AM - 10:00 PM"),
        });
    }

    // Staff can update the contact details shown to customers.
    [Authorize(Roles = "Administrator,Manager")]
    [HttpPut]
    public async Task<IActionResult> UpdateContacts(UpdateSupportContactsDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Whatsapp) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ProblemDetails { Title = "Phone, WhatsApp and Email are required." });
        }

        await UpsertSettingAsync(PhoneKey, request.Phone, "Support phone number shown in the mobile app.");
        await UpsertSettingAsync(WhatsappKey, request.Whatsapp, "Support WhatsApp number shown in the mobile app.");
        await UpsertSettingAsync(EmailKey, request.Email, "Support email shown in the mobile app.");
        await UpsertSettingAsync(WorkingHoursKey, request.WorkingHours, "Support working hours shown in the mobile app.");

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task UpsertSettingAsync(string key, string value, string description)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description,
                IsPublic = true,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = value;
            setting.IsPublic = true;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string GetValue(IReadOnlyDictionary<string, string?> settings, string key, string fallback) =>
        !string.IsNullOrWhiteSpace(settings.TryGetValue(key, out var value) ? value : null)
            ? settings[key]!
            : fallback;
}
