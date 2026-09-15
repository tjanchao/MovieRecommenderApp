namespace movieRecommender.Data;

/// <summary>
/// The whole of what one movie-night participant may see of another — spec 003-004 FR-008.
/// </summary>
/// <remarks>
/// Movie night (stories 027–029) blends taste profiles into a shared shortlist, which is a
/// hole in "visible only to me" by construction. The spec cuts that hole to a fixed size
/// <i>now</i>, before the feature exists, so 027 is written against a stated boundary
/// instead of quietly widening one (§3.1).
/// <para>
/// Nothing consumes this type yet. It exists so that when 027 lands, the shared projection
/// is something it has to <i>use</i> rather than something it gets to invent. Ratings,
/// watchlist, history, dismissals and taste profile are absent on purpose: a participant's
/// private opinion of a shortlisted film stays private even while their vote on it does
/// not (EC-14, SC-008).
/// </para>
/// </remarks>
/// <param name="UserId">Identifies the participant. No personal record travels with it.</param>
/// <param name="DisplayName">The name already shown in the nav (001-002 FR-012).</param>
/// <param name="Votes">
/// Only the shortlisted films this participant has voted on, and how. Silence about a film
/// is not evidence of anything — it is simply absent.
/// </param>
public sealed record MovieNightParticipant(
    Guid UserId,
    string DisplayName,
    IReadOnlyDictionary<int, MovieNightVote> Votes);

/// <summary>A yes or a no on one shortlisted film. See <see cref="MovieNightParticipant"/>.</summary>
public enum MovieNightVote
{
    No = 0,
    Yes = 1,
}
