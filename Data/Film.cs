namespace movieRecommender.Data;

/// <summary>
/// A film, as the application knows it — spec 003-004 §5.1, the minimum viable shape.
/// </summary>
/// <remarks>
/// Seeding has to create film records before story 007's live catalogue exists, so this
/// spec pins the fields the seed depends on and story 007 extends them. What 007 must not
/// do is rename or re-key this type: <see cref="TmdbId"/> is the contract between checked-in
/// seed data and live-fetched data (§5.3, FR-014, EC-11).
/// <para>
/// Shared catalogue data, so it is deliberately <b>not</b> an <see cref="IOwnedRecord"/> —
/// two users may hold ratings of the same film without either seeing the other's.
/// </para>
/// </remarks>
public class Film
{
    /// <summary>
    /// The primary key, and the identity of a film everywhere in the system. The
    /// application never mints a film identifier of its own, which is what makes SC-013
    /// work: a seeded film and the same film fetched from TMDB are one record.
    /// </summary>
    public int TmdbId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int ReleaseYear { get; set; }

    /// <summary>Nullable: TMDB does not always have it.</summary>
    public int? RuntimeMin { get; set; }

    /// <summary>TMDB genre names.</summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>
    /// TMDB-relative path, never a full URL, so the image base can be swapped or stubbed
    /// when the machine is offline (EC-13).
    /// </summary>
    public string? PosterPath { get; set; }

    public string? Synopsis { get; set; }

    /// <summary>The decade label used by taste profiles — "1990s", "2020s".</summary>
    public string Era => $"{ReleaseYear / 10 * 10}s";
}
