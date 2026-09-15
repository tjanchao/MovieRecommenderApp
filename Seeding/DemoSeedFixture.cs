using movieRecommender.Data;

namespace movieRecommender.Seeding;

/// <summary>
/// The declarative description of the demo, read from the repository at seed time — spec
/// 003-004 §5.1. It has no database representation: it is the input to seeding, not its
/// output.
/// </summary>
/// <remarks>
/// Checked in rather than fetched from TMDB (FR-013). Calling TMDB at seed time would give
/// real, current metadata but would make story 004 depend on story 005 and on a working
/// network, so a fresh clone with no key would produce no demo accounts — inverting the
/// whole value of the story. See
/// <see href="../docs/architecture/adr/0004-checked-in-seed-data.md">ADR-0004</see>.
/// </remarks>
public sealed class DemoSeedFixture
{
    /// <summary>TMDB's terms require attribution for use of their data (§9.2).</summary>
    public string Attribution { get; init; } = string.Empty;

    public IReadOnlyList<SeedFilm> Films { get; init; } = [];

    public IReadOnlyList<DemoAccountDefinition> Accounts { get; init; } = [];
}

/// <summary>A film in the shared seed pool. Keyed on its real TMDB id (FR-014).</summary>
public sealed record SeedFilm
{
    public required int TmdbId { get; init; }

    public required string Title { get; init; }

    public required int ReleaseYear { get; init; }

    public int? RuntimeMin { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];

    /// <summary>
    /// TMDB-relative path, not a full URL (EC-13). Null throughout the checked-in fixture:
    /// poster paths are TMDB's to serve, and a demo that works offline must not depend on
    /// having guessed one correctly.
    /// </summary>
    public string? PosterPath { get; init; }

    public string? Synopsis { get; init; }

    public string Era => $"{ReleaseYear / 10 * 10}s";
}

/// <summary>
/// One demo account — spec 003-004 §5.1. <see cref="Email"/> doubles as the idempotence
/// key (§5.4, FR-012).
/// </summary>
public sealed record DemoAccountDefinition
{
    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>
    /// Plaintext in the repository <b>by design</b> (FR-017, §9.2), hashed on seeding
    /// exactly as any other account's would be. Harmless because these accounts exist only
    /// in Development on a localhost demo, and load-bearing because the point is that
    /// anyone cloning the repo can sign in. It is FR-019 that keeps it harmless.
    /// </summary>
    public required string Password { get; init; }

    public required string TasteLabel { get; init; }

    public IReadOnlyList<string> PreferredGenres { get; init; } = [];

    public IReadOnlyList<string> PreferredEras { get; init; } = [];

    public IReadOnlyList<SeedRating> Ratings { get; init; } = [];

    public IReadOnlyList<SeedEvent> History { get; init; } = [];

    public IReadOnlyList<SeedEvent> Watchlist { get; init; } = [];

    /// <summary>The films this account is seeded to like or love — FR-015 and FR-016 both count these.</summary>
    public IEnumerable<int> PositivelyRatedFilms =>
        Ratings.Where(r => r.Sentiment is Sentiment.Liked or Sentiment.Loved).Select(r => r.TmdbId);
}

/// <summary>An opinion in the fixture. <paramref name="DaysAgo"/>, never a date — see FR-022.</summary>
public sealed record SeedRating(int TmdbId, Sentiment Sentiment, int DaysAgo);

/// <summary>A watch or a watchlist addition, dated relative to the moment of seeding (FR-022).</summary>
public sealed record SeedEvent(int TmdbId, int DaysAgo);
