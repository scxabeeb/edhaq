using eDhaq.Data;
using eDhaq.Web.Areas.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages;

/// <summary>
/// Public support page (App Store / Play Store "Support URL"). Shows the same
/// contact details staff manage for the mobile app's Contact screen.
/// </summary>
[AllowAnonymous]
public class SupportModel : PageModel
{
    private readonly AppDbContext _db;

    public SupportModel(AppDbContext db)
    {
        _db = db;
    }

    public string Phone { get; private set; } = "+967771234567";
    public string Whatsapp { get; private set; } = "+967771234567";
    public string Email { get; private set; } = "support@edhaq.com";
    public string WorkingHours { get; private set; } = "Sat - Thu, 8:00 AM - 10:00 PM";

    public string WhatsappLink => "https://wa.me/" + new string(Whatsapp.Where(char.IsDigit).ToArray());

    public async Task OnGetAsync()
    {
        var settings = await _db.AppSettings
            .AsNoTracking()
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        Phone = Pick(settings, ContactsController.PhoneKey, Phone);
        Whatsapp = Pick(settings, ContactsController.WhatsappKey, Whatsapp);
        Email = Pick(settings, ContactsController.EmailKey, Email);
        WorkingHours = Pick(settings, ContactsController.WorkingHoursKey, WorkingHours);
    }

    private static string Pick(Dictionary<string, string?> settings, string key, string fallback) =>
        settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}
