using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Paqueteria.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    SignInManager<IdentityUser> signInManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public string ReturnUrl { get; private set; } = "/";

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = GetLocalReturnUrl(returnUrl);

        if (signInManager.IsSignedIn(User))
        {
            return LocalRedirect(ReturnUrl);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = GetLocalReturnUrl(returnUrl);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User signed in.");
            return LocalRedirect(ReturnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new
            {
                ReturnUrl,
                RememberMe = Input.RememberMe
            });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
        return Page();
    }

    private string GetLocalReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Mantener sesión iniciada")]
        public bool RememberMe { get; set; }
    }
}