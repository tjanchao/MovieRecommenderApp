using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace movieRecommender.Pages;

/// <summary>
/// 003-004 FR-005: an explicit opt-out from deny-by-default. An error page that bounced an
/// anonymous visitor to the login form would hide every failure that happens before a
/// login — including a failure of the login itself.
/// </summary>
[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        // FR-010: a correlation id, never the contents of whatever failed.
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
