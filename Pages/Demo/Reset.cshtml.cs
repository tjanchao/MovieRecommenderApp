using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using movieRecommender.Seeding;

namespace movieRecommender.Pages.Demo;

/// <summary>
/// Run the demo again — spec 003-004 US-008, FR-021, SC-021.
/// </summary>
/// <remarks>
/// Restores the demo accounts to their seeded state, so the ratings and dismissals from
/// the last run do not leak into the next one.
/// <para>
/// Development only, gated by the same server-side check as one-click sign-in (FR-019,
/// NFR-003): outside Development every handler here is a plain not-found. Anonymous by
/// design — resetting the demo should not require first signing into the demo — and it is
/// still a POST carrying an anti-forgery token, so it cannot be triggered by a link.
/// </para>
/// <para>
/// EC-10: this deletes and re-creates owned records but leaves the accounts themselves
/// alone, so a demo account signed in elsewhere keeps its cookie and simply sees seeded
/// state on its next page load.
/// </para>
/// </remarks>
[AllowAnonymous]
public class ResetModel : PageModel
{
    private readonly DemoSignIn _demoSignIn;
    private readonly DemoDataSeeder _seeder;

    public ResetModel(DemoSignIn demoSignIn, DemoDataSeeder seeder)
    {
        _demoSignIn = demoSignIn;
        _seeder = seeder;
    }

    public IReadOnlyList<DemoSignInOption> Accounts => _demoSignIn.Options;

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet() => _demoSignIn.IsAvailable ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!_demoSignIn.IsAvailable)
        {
            return NotFound();
        }

        var restored = await _seeder.ResetAsync(cancellationToken);
        StatusMessage = $"{restored} demo account(s) restored to their seeded state.";
        return RedirectToPage();
    }
}
