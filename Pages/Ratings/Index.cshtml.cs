using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;

namespace movieRecommender.Pages.Ratings;

/// <summary>
/// Your ratings — spec 003-004 SC-001.
/// </summary>
/// <remarks>
/// Read-only, and deliberately thin: story 009 owns rating a film and everything that
/// comes with it. This page exists because 003-004's central claim is about what a read
/// returns, and a claim about reads needs a read to make it.
/// <para>
/// Note what is <i>not</i> here: no <c>[Authorize]</c> (deny-by-default supplies it,
/// FR-005) and no <c>where OwnerUserId == ...</c> (the store supplies that, FR-002). A page
/// that has to remember either one is a page that can forget (NFR-005, EC-16).
/// </para>
/// </remarks>
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Rating> Ratings { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Ratings = await _db.Ratings
            .Include(rating => rating.Film)
            .OrderByDescending(rating => rating.RatedAt)
            .ToListAsync();
    }
}
