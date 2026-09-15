namespace movieRecommender.Identity;

/// <summary>
/// The single rule FR-008, SC-004 and SC-011 all rest on: an email is compared by its
/// normalized form — surrounding whitespace trimmed, case folded away.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
