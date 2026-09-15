using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;

namespace movieRecommender.Pages;

/// <summary>
/// The home page. Anonymous visitors get the pitch; signed-in ones get a summary of their
/// own taste.
/// </summary>
/// <remarks>
/// 003-004 FR-005: an explicit opt-out from deny-by-default — a signed-out visitor has to
/// be able to land somewhere. The page still renders personal data when there is a signed-in
/// user, which is fine and is the point: the data is scoped by the store, not by the page.
/// <para>
/// FR-007 in miniature. Every count below is an aggregate over one account's records, and
/// not one of these queries mentions an owner — the global filter in
/// <see cref="ApplicationDbContext"/> has already narrowed them. Story 025 owns the real
/// stats page; this exists so that 003-004's guarantee is something you can look at.
/// </para>
/// </remarks>
[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public TasteProfile? Taste { get; private set; }

    public int RatingCount { get; private set; }

    public int WatchedCount { get; private set; }

    public int WatchlistCount { get; private set; }

    public IReadOnlyList<string> TopGenres { get; private set; } = [];

    public IReadOnlyList<Film> RecentlyWatched { get; private set; } = [];

    public async Task OnGetAsync()
    {
        if (!User.Identity!.IsAuthenticated)
        {
            return;
        }

        Taste = await _db.TasteProfiles.SingleOrDefaultAsync();
        RatingCount = await _db.Ratings.CountAsync();
        WatchedCount = await _db.WatchHistory.CountAsync();
        WatchlistCount = await _db.Watchlist.CountAsync();

        RecentlyWatched = await _db.WatchHistory
            .OrderByDescending(entry => entry.WatchedAt)
            .Select(entry => entry.Film)
            .Take(5)
            .ToListAsync();

        // SC-007: the genres reflect this account's films and no one else's, without the
        // page having said so. Tallied in memory — NFR-006 sizes this at ~25 ratings, and
        // a genre list is a JSON column rather than a table to group by.
        var genresOfLikedFilms = await _db.Ratings
            .Where(rating => rating.Sentiment != Sentiment.Meh)
            .Select(rating => rating.Film.Genres)
            .ToListAsync();

        TopGenres = [.. genresOfLikedFilms
            .SelectMany(genres => genres)
            .GroupBy(genre => genre)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .Take(4)];
    }
}
