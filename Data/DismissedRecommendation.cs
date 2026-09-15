namespace movieRecommender.Data;

/// <summary>
/// A recommendation a user waved away. Listed among the owned records in §5.1 and cleared
/// by the demo reset (SC-021); the story that creates them owns the rest of the shape.
/// </summary>
public class DismissedRecommendation : OwnedRecord
{
    public int TmdbId { get; set; }

    public Film Film { get; set; } = null!;

    public DateTimeOffset DismissedAt { get; set; }
}
