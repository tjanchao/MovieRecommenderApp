using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace movieRecommender.Identity;

/// <summary>
/// Clears an auth cookie that no longer authenticates anyone.
/// </summary>
/// <remarks>
/// EC-8, EC-9 and EC-16 all end the same way: "treated as anonymous, cookie cleared, no
/// error page". The cookie handler gives us the first and the last for free — a ticket it
/// cannot decrypt, or one that has expired, simply yields no principal — but it leaves
/// the dead cookie in the browser to be re-sent on every subsequent request. This sweeps
/// it up.
/// <para>
/// Deletion is conditional on authentication having actually been attempted and failed,
/// so an ordinary anonymous request is untouched and a logout is left to
/// <see cref="SignInManager{TUser}.SignOutAsync"/>, which is already deleting the cookie
/// itself at that point.
/// </para>
/// </remarks>
public static class StaleAuthCookieMiddleware
{
    public static IApplicationBuilder UseStaleAuthCookieCleanup(this IApplicationBuilder app)
    {
        var cookieOptions = app.ApplicationServices
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var cookieName = cookieOptions.Cookie.Name!;

        return app.Use(async (context, next) =>
        {
            if (context.Request.Cookies.ContainsKey(cookieName))
            {
                // Cached by the authentication middleware that ran before us, so this
                // costs a dictionary lookup rather than a second decrypt.
                var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                if (!result.Succeeded)
                {
                    context.Response.Cookies.Delete(cookieName, cookieOptions.Cookie.Build(context));
                }
            }

            await next(context);
        });
    }
}
