using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;
using movieRecommender.Identity;

namespace movieRecommender.Pages.Account;

/// <summary>Sign-up — US-001, FR-001 through FR-005.</summary>
[IgnoreAntiforgeryToken] // validated by hand instead; see AntiforgeryPageExtensions.
public class RegisterModel : PageModel
{
    public const string DuplicateEmailMessage = "That email is already registered.";
    public const string StaleFormMessage = "That form had expired. Please try again.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAntiforgery _antiforgery;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _antiforgery = antiforgery;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        private string _email = string.Empty;
        private string _displayName = string.Empty;

        /// <summary>
        /// Normalized as it binds, so validation, the uniqueness check and the stored
        /// value all see one form (FR-008, SC-004).
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Email must be 256 characters or fewer.")]
        [Display(Name = "Email")]
        public string Email
        {
            get => _email;
            set => _email = EmailNormalizer.Normalize(value);
        }

        /// <summary>Trimmed on bind; a whitespace-only name fails <c>[Required]</c> (§5.4).</summary>
        [Required(ErrorMessage = "Display name is required.")]
        [TextElementLength(1, 50, ErrorMessage = "Display name must be between 1 and 50 characters.")]
        [Display(Name = "Display name")]
        public string DisplayName
        {
            get => _displayName;
            set => _displayName = (value ?? string.Empty).Trim();
        }

        /// <summary>
        /// Never trimmed — leading and trailing spaces are part of the secret (EC-4).
        /// The 128-character ceiling is EC-3: an unbounded password is unbounded CPU.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
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
            // Nothing was validated and nothing was persisted (SC-020). Drop whatever
            // model errors exist so the user sees one honest message, not a pile.
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, StaleFormMessage);
            return Page();
        }

        // FR-002. All failures surface together (EC-1), each against its own field,
        // server-side and independent of client scripting (NFR-007).
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = Input.DisplayName,
        };

        IdentityResult result;
        try
        {
            // FR-004: the password goes in, a salted one-way hash comes out, and the
            // plaintext is never stored, logged or rendered anywhere after this line.
            result = await _userManager.CreateAsync(user, Input.Password);
        }
        catch (DbUpdateException)
        {
            // EC-6/EC-7: a double-clicked or replayed sign-up that slips past Identity's
            // own duplicate check still hits the unique index. The loser of the race
            // gets the ordinary error, not a crash.
            return DuplicateEmail();
        }

        if (!result.Succeeded)
        {
            return FromIdentityErrors(result);
        }

        // FR-005. Session-scoped: "Remember me" is a choice made at login (FR-009).
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// FR-003. Deliberately specific, unlike the login failure message — and knowingly
    /// asymmetric with it, because a vague sign-up error is unusable. See §3.1.
    /// </summary>
    private PageResult DuplicateEmail()
    {
        ModelState.AddModelError($"{nameof(Input)}.{nameof(InputModel.Email)}", DuplicateEmailMessage);
        return Page();
    }

    private PageResult FromIdentityErrors(IdentityResult result)
    {
        var duplicates = new[]
        {
            nameof(IdentityErrorDescriber.DuplicateEmail),
            nameof(IdentityErrorDescriber.DuplicateUserName),
        };

        // Identity reports the same collision twice, once per column. One message.
        if (result.Errors.Any(error => duplicates.Contains(error.Code)))
        {
            return DuplicateEmail();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
