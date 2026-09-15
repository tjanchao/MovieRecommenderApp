using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace movieRecommender.Security;

/// <summary>
/// <see cref="ICurrentUser"/> over the authentication cookie's principal.
/// </summary>
/// <remarks>
/// Deliberately has no access to <see cref="HttpRequest.RouteValues"/>,
/// <see cref="HttpRequest.Query"/> or <see cref="HttpRequest.Form"/> — the class cannot
/// honour a supplied <c>userId</c> even by accident, which is how EC-3 stays true as the
/// application grows.
/// <para>
/// Outside a request — startup seeding, for instance — there is no principal and
/// <see cref="Id"/> is <c>null</c>. That is the safe direction: the ownership filter in
/// <see cref="Data.ApplicationDbContext"/> then matches nothing at all.
/// </para>
/// </remarks>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _userIdClaimType;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, IOptions<IdentityOptions> identityOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _userIdClaimType = identityOptions.Value.ClaimsIdentity.UserIdClaimType;
    }

    public Guid? Id
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity is not { IsAuthenticated: true })
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirstValue(_userIdClaimType), out var id) ? id : null;
        }
    }

    public bool IsSignedIn => Id is not null;
}
