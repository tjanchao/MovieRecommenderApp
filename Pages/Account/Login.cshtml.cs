using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using movieRecommender.Data;
using movieRecommender.Identity;
using movieRecommender.Seeding;

namespace movieRecommender.Pages.Account;

/// <summary>
/// Login — 001-002 US-002 and US-004, FR-006 through FR-010 and FR-014, FR-015. Also
/// 003-004 US-005: one-click demo sign-in, in Development only (FR-018, FR-019).
/// </summary>
/// <remarks>
/// The explicit opt-out FR-005's deny-by-default expects. You cannot be required to be
/// signed in to sign in.
/// </remarks>
[AllowAnonymous]
[IgnoreAntiforgeryToken] // validated by hand instead; see AntiforgeryPageExtensions.
public class LoginModel : PageModel
{
    /// <summary>
    /// FR-007. One message for a wrong password and for an email that was never
    /// registered — SC-009 and SC-010 must be indistinguishable in wording, as NFR-003
    /// makes them indistinguishable in timing.
    /// </summary>
    public const string FailureMessage = "Email or password is incorrect";

    public const string LockedOutMessage = "Too many failed attempts. Please try again shortly.";
    public const string StaleFormMessage = "That form had expired. Please try again.";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly DecoyPasswordHash _decoyHash;
    private readonly IAntiforgery _antiforgery;
    private readonly DemoSignIn _demoSignIn;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        DecoyPasswordHash decoyHash,
        IAntiforgery antiforgery,
        DemoSignIn demoSignIn)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _decoyHash = decoyHash;
        _antiforgery = antiforgery;
        _demoSignIn = demoSignIn;
    }

    /// <summary>
    /// FR-019 / NFR-003: asked server-side on every render, and asked again in the handler.
    /// Outside Development there is nothing here to hide, because there is nothing here.
    /// </summary>
    public DemoSignIn DemoSignIn => _demoSignIn;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        private string _email = string.Empty;

        /// <summary>Normalized on bind, so "  AVA@EXAMPLE.COM  " logs in (FR-008, SC-011).</summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Email must be 256 characters or fewer.")]
        [Display(Name = "Email")]
        public string Email
        {
            get => _email;
            set => _email = EmailNormalizer.Normalize(value);
        }

        /// <summary>Never trimmed (EC-4), and capped for the same reason as sign-up (EC-3).</summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, ErrorMessage = "Password must be 128 characters or fewer.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    // FR-013.
    public IActionResult OnGet() =>
        _signInManager.IsSignedIn(User) ? RedirectToPage("/Index") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToPage("/Index");
        }

        if (!await this.HasValidAntiforgeryTokenAsync(_antiforgery))
        {
            // EC-10: the form comes back with a fresh token, never a raw 400. No
            // sign-in state changed (SC-020).
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, StaleFormMessage);
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            // NFR-003: pay the KDF cost anyway. Skipping it here would answer an
            // unknown email in single-digit milliseconds and give away, through timing,
            // precisely what FailureMessage declines to say.
            _passwordHasher.VerifyHashedPassword(new ApplicationUser(), _decoyHash.Value, Input.Password);
            return Failed();
        }

        // FR-015, EC-11: PasswordSignInAsync checks the lockout before it checks the
        // password, so a locked account is refused even when the password is right —
        // and the window is neither reset nor extended by the attempt.
        var result = await _signInManager.PasswordSignInAsync(
            user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, LockedOutMessage);
            return Page();
        }

        if (!result.Succeeded)
        {
            return Failed();
        }

        // FR-009 / FR-010 are already settled: isPersistent above decides between a
        // session cookie and the 14-day sliding one configured in Program.cs.
        return RedirectToLocal();
    }

    /// <summary>
    /// One-click demo sign-in — 003-004 FR-018, SC-016. Signs a user in without verifying
    /// a password, so that a mistyped password in front of an audience cannot derail the
    /// opening (US-005).
    /// </summary>
    /// <remarks>
    /// Every gate here is server-side (NFR-003). In a non-Development environment the
    /// availability check fails first and a forged request gets a plain not-found (EC-17)
    /// — the same answer as an email that is not one of the fixture's.
    /// </remarks>
    public async Task<IActionResult> OnPostDemoAsync(string? email)
    {
        if (!_demoSignIn.IsAvailable)
        {
            return NotFound();
        }

        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToPage("/Index");
        }

        if (!await this.HasValidAntiforgeryTokenAsync(_antiforgery))
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, StaleFormMessage);
            return Page();
        }

        // The submitted email selects from the fixture and nothing else — it never
        // reaches the store as a free-form lookup.
        var option = _demoSignIn.Find(email);
        if (option is null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByEmailAsync(option.Email);
        if (user is null)
        {
            // Seeding is skipped when the app runs outside Development, and an account can
            // be deleted between runs. Nothing to sign in as.
            return NotFound();
        }

        // EC-15: the demo account locks like any other after five mistyped passwords
        // (FR-020) — and this path verifies no password, so it still works and clears the
        // count on the way through.
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        // §5.2: the same cookie a password login issues, and indistinguishable from one
        // afterwards. Session-scoped, as sign-up is — "Remember me" is a typed-login choice.
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal();
    }

    private PageResult Failed()
    {
        ModelState.AddModelError(string.Empty, FailureMessage);
        return Page();
    }

    /// <summary>
    /// FR-014. <see cref="IUrlHelper.IsLocalUrl"/> rather than a "starts with /" test,
    /// which EC-13 points out would wave through the protocol-relative "//evil.example".
    /// </summary>
    private IActionResult RedirectToLocal() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? Redirect(ReturnUrl)
            : RedirectToPage("/Index");
}
