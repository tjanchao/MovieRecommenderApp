using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using movieRecommender.Data;

namespace movieRecommender.Identity;

/// <summary>
/// Carries the display name in the auth cookie so FR-012 costs no database round trip.
/// </summary>
/// <remarks>
/// Per §5.2 there is no session entity, so a renamed user keeps the old display name
/// until their cookie is re-issued. That is a known and accepted consequence.
/// </remarks>
public sealed class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public const string DisplayNameClaimType = "display_name";

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options)
        : base(userManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(DisplayNameClaimType, user.DisplayName));
        return identity;
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's display name, falling back to their email.</summary>
    public static string DisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AppUserClaimsPrincipalFactory.DisplayNameClaimType)
        ?? principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.Identity?.Name
        ?? string.Empty;
}
