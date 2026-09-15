using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;

namespace movieRecommender.Security;

/// <summary>
/// Tells browsers not to store pages that render personal data — spec 003-004 FR-009.
/// </summary>
/// <remarks>
/// SC-009: Ava signs in on a shared browser, opens her watchlist, signs out, and the next
/// person presses Back. Without this the browser is entitled to redisplay the cached page
/// from its own store, having never asked the server whether that is still Ava.
/// <para>
/// The set of pages to cover is derived from the same fact that protects them — an
/// endpoint that has not opted out of authorization via <see cref="IAllowAnonymous"/> is
/// by definition one that may render personal data (FR-005). So this inherits
/// deny-by-default too: a new page is no-store unless somebody says otherwise, rather
/// than because somebody remembered.
/// </para>
/// </remarks>
public static class PersonalDataCacheControlMiddleware
{
    private const string NoStore = "no-cache, no-store, must-revalidate, max-age=0";

    public static IApplicationBuilder UsePersonalDataCacheControl(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var httpContext = (HttpContext)state;
                var endpoint = httpContext.GetEndpoint();

                // No endpoint means nothing was routed — a 404, or the static file
                // pipeline. Neither renders personal data.
                if (endpoint is not null && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
                {
                    var headers = httpContext.Response.Headers;
                    headers[HeaderNames.CacheControl] = NoStore;
                    headers[HeaderNames.Pragma] = "no-cache";
                    headers[HeaderNames.Expires] = "0";
                }

                return Task.CompletedTask;
            }, context);

            await next(context);
        });
}
