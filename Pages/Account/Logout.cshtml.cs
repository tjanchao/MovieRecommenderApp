using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using movieRecommender.Data;

namespace movieRecommender.Pages.Account;

/// <summary>Logout — US-003, FR-011.</summary>
/// <remarks>
/// Keeps Razor Pages' automatic anti-forgery validation (FR-016): unlike the login form,
/// there is no half-filled state worth re-rendering, so a rejected request may simply be
/// a 400.
/// <para>
/// 003-004 FR-005: an explicit opt-out from deny-by-default. EC-14 requires signing out
/// when already signed out to be a no-op rather than a bounce to the login form.
/// </para>
/// </remarks>
[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LogoutModel(SignInManager<ApplicationUser> signInManager) =>
        _signInManager = signInManager;

    /// <summary>Logging out is a POST; arriving here by link is not an error.</summary>
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        // EC-14: signing out when already signed out is a no-op, not a failure.
        // §5.2: this ends the sign-in in *this* browser only — there is no server-side
        // session to end elsewhere.
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
