using System.Net;
using System.Net.Mail;

namespace eDhaq.Web.Core.Services;

public interface IEmailService
{
    /// <summary>Sends an HTML email. Returns false when SMTP is not configured or sending fails.</summary>
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
}

/// <summary>
/// SMTP sender driven by the "Smtp" section of appsettings. For Gmail use
/// Host=smtp.gmail.com, Port=587, UserName=your Gmail address and
/// Password=a Google "App password" (not your normal Gmail password).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var userName = smtp["UserName"];
        var password = smtp["Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP is not configured; email '{Subject}' to {Email} was not sent.", subject, toEmail);
            return false;
        }

        var fromEmail = string.IsNullOrWhiteSpace(smtp["FromEmail"]) ? userName : smtp["FromEmail"]!;
        var fromName = smtp["FromName"] ?? "e-Dhaq";

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, smtp.GetValue<int?>("Port") ?? 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(userName, password)
        };

        try
        {
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email '{Subject}' to {Email}.", subject, toEmail);
            return false;
        }
    }
}
