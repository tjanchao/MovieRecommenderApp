namespace movieRecommender.Data;

/// <summary>
/// What a user says they like, as distinct from what their ratings imply. Minimum viable
/// shape — story 010 owns the entity (§1.3); this spec fixes only that it is an
/// <see cref="IOwnedRecord"/> and seeds the fields FR-011 names.
/// </summary>
/// <remarks>
/// One per user, which the unique index on <see cref="IOwnedRecord.OwnerUserId"/>
/// enforces at the store.
/// </remarks>
public class TasteProfile : OwnedRecord
{
    /// <summary>Human-readable, e.g. "slow-burn thrillers and neo-noir, 1990s–2000s".</summary>
    public string TasteLabel { get; set; } = string.Empty;

    public List<string> PreferredGenres { get; set; } = [];

    /// <summary>Decade labels — "1990s", "2020s" — matching <see cref="Film.Era"/>.</summary>
    public List<string> PreferredEras { get; set; } = [];
}
