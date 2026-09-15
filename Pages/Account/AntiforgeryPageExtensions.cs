using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace movieRecommender.Pages.Account;

/// <summary>
/// Turns a failed anti-forgery check into a boolean the page can render around.
/// </summary>
/// <remarks>
/// FR-016 rejects any sign-up, login or logout without a valid token. Razor Pages does
/// that for free — but by short-circuiting to a bare 400, which EC-10 rules out for the
/// forms: a login page left open in two tabs is an ordinary thing to do, and it should
/// come back as the form plus "please try again", not as a browser error page. Pages
/// that need that behaviour opt out of automatic validation with
/// <c>[IgnoreAntiforgeryToken]</c> and call this instead; the request is still rejected,
/// just legibly.
/// </remarks>
internal static class AntiforgeryPageExtensions
{
    public static async Task<bool> HasValidAntiforgeryTokenAsync(this PageModel page, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(page.HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
