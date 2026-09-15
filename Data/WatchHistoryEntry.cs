namespace movieRecommender.Data;

/// <summary>
/// A film a user watched. Minimum viable shape — story 024 owns the entity (§1.3); this
/// spec fixes only that it is an <see cref="IOwnedRecord"/>.
/// </summary>
/// <remarks>
/// Not unique per film: rewatching is a thing, and the seed's "watched three days ago"
/// entries are events, not a set.
/// </remarks>
public class WatchHistoryEntry : OwnedRecord
{
    public int TmdbId { get; set; }

    public Film Film { get; set; } = null!;

    public DateTimeOffset WatchedAt { get; set; }
}
