using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace movieRecommender.Pages;

/// <summary>
/// 003-004 FR-005: an explicit opt-out from deny-by-default. Static copy, no personal data.
/// </summary>
[AllowAnonymous]
public class PrivacyModel : PageModel
{
    public void OnGet()
    {
    }
}
