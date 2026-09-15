using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;

namespace movieRecommender.Pages.Ratings;

/// <summary>
/// One rating, by its identifier — spec 003-004 SC-002, SC-003, SC-005, EC-1, EC-2.
/// </summary>
/// <remarks>
/// The page the spec's privacy claim is actually testable against: it takes a record id
/// straight off the URL, which is the thing a curious person edits.
/// <para>
/// There is one lookup and one failure path. A rating belonging to somebody else is
/// filtered out of the query before this code sees it, so it arrives as <c>null</c> — the
/// same <c>null</c> an id that exists nowhere produces, answered with the same 404.
/// FR-003 is satisfied not because the page treats the two cases alike but because it
/// cannot tell them apart.
/// </para>
/// <para>
/// SC-005: <c>id</c> selects a record; it never selects a user. Whose ratings are in scope
/// was settled by the cookie before routing began (FR-006).
/// </para>
/// </remarks>
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DetailsModel(ApplicationDbContext db) => _db = db;

    public Rating Rating { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var rating = await _db.Ratings
            .Include(candidate => candidate.Film)
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (rating is null)
        {
            return NotFound();
        }

        Rating = rating;
        return Page();
    }
}
