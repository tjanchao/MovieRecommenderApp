using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using movieRecommender.Data;

namespace movieRecommender.Pages.Account;

/// <summary>
/// US-005 — the account, as the app understands it.
/// </summary>
/// <remarks>
/// Also the first page in the app that requires an account, which is what SC-012 and
/// SC-015 need to have something to bounce off. Profile management is a non-goal (§1.3),
/// so this reads and never writes.
/// </remarks>
[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public ApplicationUser Account { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            // EC-8: a valid-looking cookie whose user is gone. Anonymous, not an error.
            return RedirectToPage("/Index");
        }

        Account = user;
        return Page();
    }
}
