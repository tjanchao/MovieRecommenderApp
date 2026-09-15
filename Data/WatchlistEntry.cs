namespace movieRecommender.Data;

/// <summary>
/// A film a user means to watch. Minimum viable shape — story 022 owns the entity (§1.3);
/// this spec fixes only that it is an <see cref="IOwnedRecord"/>.
/// </summary>
/// <remarks>
/// The spec's own worked example of why "not yours" and "doesn't exist" must look alike
/// (FR-003, SC-002): that a particular film is on someone's watchlist is itself the
/// private fact, so a 403 would disclose it.
/// </remarks>
public class WatchlistEntry : OwnedRecord
{
    public int TmdbId { get; set; }

    public Film Film { get; set; } = null!;

    public DateTimeOffset AddedAt { get; set; }
}
