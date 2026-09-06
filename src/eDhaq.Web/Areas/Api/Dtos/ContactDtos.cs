namespace eDhaq.Web.Areas.Api.Dtos;

/// <summary>
/// Public support-contact details shown to customers in the mobile app.
/// Backed by the AppSettings table (public keys seeded at startup).
/// </summary>
public class SupportContactsDto
{
    public string Phone { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
}

/// <summary>
/// Payload used by staff to update the public support contacts.
/// </summary>
public class UpdateSupportContactsDto
{
    public string Phone { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
}
