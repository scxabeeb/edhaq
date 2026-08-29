using eDhaq.Common.ViewModels;
using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Administrator") || roles.Contains("Manager"))
            {
                return RedirectToPage("/Admin/Index");
            }

            if (roles.Contains("PickupDriver") || roles.Contains("DeliveryDriver"))
            {
                return RedirectToPage("/Driver/Index");
            }

            if (roles.Contains("LaundryStaff") || roles.Contains("Cashier"))
            {
                return RedirectToPage("/Staff/Index");
            }
        }

        return RedirectToPage("/Customer/Index");
    }
}
