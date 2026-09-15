namespace movieRecommender.Data;

/// <summary>How a user felt about a film. Three levels, no stars — story 009 owns the scale.</summary>
public enum Sentiment
{
    Meh = 0,
    Liked = 1,
    Loved = 2,
}

/// <summary>
/// What a user thought of a film. Defined here only to the depth the seed needs — story
/// 009 owns the entity (spec 003-004 §1.3). What this spec fixes is that it is an
/// <see cref="IOwnedRecord"/>, and that 009 inherits the ownership rule rather than
/// choosing one.
/// </summary>
public class Rating : OwnedRecord
{
    public int TmdbId { get; set; }

    public Film Film { get; set; } = null!;

    public Sentiment Sentiment { get; set; }

    public DateTimeOffset RatedAt { get; set; }

    /// <summary>
    /// What counts as "rated positively" for FR-016's shared shortlist and for the
    /// weaker form of FR-015 the seed fixture asserts (§10 question 1).
    /// </summary>
    public bool IsPositive => Sentiment is Sentiment.Liked or Sentiment.Loved;
}
