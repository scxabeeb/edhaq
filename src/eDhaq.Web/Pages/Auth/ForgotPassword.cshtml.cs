using eDhaq.Web.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace eDhaq.Web.Pages.Auth;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;

    public ForgotPasswordModel(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
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

        await _passwordResetService.RequestCodeAsync(Input.Email);

        TempData["SuccessMessage"] = "If an account exists for that email, we've sent a 6-digit reset code. Check your inbox (and spam folder).";
        return RedirectToPage("ResetPassword", new { email = Input.Email.Trim() });
    }
}
