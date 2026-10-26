using eDhaq.Web.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace eDhaq.Web.Pages.Auth;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;

    public ResetPasswordModel(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\s*\d{6}\s*$", ErrorMessage = "Enter the 6-digit code from the email."), Display(Name = "Reset code")]
        public string Code { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8), Display(Name = "New password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match."), Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? email)
    {
        Input.Email = email ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _passwordResetService.ResetAsync(Input.Email, Input.Code, Input.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to reset password.");
            return Page();
        }

        TempData["SuccessMessage"] = "Password reset successfully. Sign in with your new password.";
        return RedirectToPage("Login");
    }
}
