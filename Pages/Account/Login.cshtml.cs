using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using movieRecommender.Data;
using movieRecommender.Identity;

namespace movieRecommender.Pages.Account;

/// <summary>Login — US-002 and US-004, FR-006 through FR-010 and FR-014, FR-015.</summary>
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

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        DecoyPasswordHash decoyHash,
        IAntiforgery antiforgery)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _decoyHash = decoyHash;
        _antiforgery = antiforgery;
    }

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
