using System.Net;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace eDhaq.Web.Core.Services;

public interface IPasswordResetService
{
    /// <summary>
    /// Emails a 6-digit reset code if an active account exists for the address.
    /// Always completes silently so callers can't probe which emails are registered.
    /// </summary>
    Task RequestCodeAsync(string email);

    /// <summary>Verifies the emailed code and sets the new password.</summary>
    Task<PasswordResetResult> ResetAsync(string email, string code, string newPassword);
}

public sealed record PasswordResetResult(bool Succeeded, string? Error = null);

/// <summary>
/// Code-based password reset. Codes come from Identity's email token provider
/// (time-based, valid for a few minutes, tied to the user's security stamp so
/// they stop working once the password changes). Wrong codes count towards
/// account lockout to stop brute-forcing the 6 digits.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private const string Purpose = "ResetPasswordCode";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IWebHostEnvironment environment,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _environment = environment;
        _logger = logger;
    }

    public async Task RequestCodeAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var code = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, Purpose);

        var body = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:auto;padding:24px;color:#1f2330">
              <h2 style="color:#d71920;margin:0 0 16px">e-Dhaq password reset</h2>
              <p>Hi {WebUtility.HtmlEncode(user.FirstName)},</p>
              <p>Use this code to reset your password. It expires in a few minutes.</p>
              <p style="font-size:32px;font-weight:700;letter-spacing:8px;background:#f5f6f8;border-radius:12px;padding:16px;text-align:center">{code}</p>
              <p style="color:#6b7280;font-size:13px">If you didn't ask for this, you can ignore this email — your password won't change.</p>
            </div>
            """;

        var sent = await _emailService.SendAsync(user.Email, "Your e-Dhaq password reset code", body);
        if (!sent && _environment.IsDevelopment())
        {
            // Lets you test the flow locally before SMTP credentials are set.
            _logger.LogWarning("DEV ONLY — password reset code for {Email}: {Code}", user.Email, code);
        }
    }

    public async Task<PasswordResetResult> ResetAsync(string email, string code, string newPassword)
    {
        const string invalid = "The code is invalid or has expired. Request a new one.";

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
        {
            return new PasswordResetResult(false, invalid);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new PasswordResetResult(false, "Too many wrong attempts. Try again in a few minutes.");
        }

        var valid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, Purpose, code.Trim());
        if (!valid)
        {
            await _userManager.AccessFailedAsync(user);
            return new PasswordResetResult(false, invalid);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!result.Succeeded)
        {
            return new PasswordResetResult(false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return new PasswordResetResult(true);
    }
}
