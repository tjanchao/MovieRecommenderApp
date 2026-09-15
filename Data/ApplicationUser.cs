using Microsoft.AspNetCore.Identity;

namespace movieRecommender.Data;

/// <summary>
/// A person with an account — spec 001-002 §5.1. Every later personal record (rating,
/// watchlist entry, watch history entry, movie-night vote) points back at <see cref="IdentityUser{TKey}.Id"/>.
/// </summary>
/// <remarks>
/// Identity's base type already supplies the spec's <c>email</c>, <c>passwordHash</c>,
/// <c>failedLoginAttempts</c> (<c>AccessFailedCount</c>) and <c>lockoutEndsAt</c>
/// (<c>LockoutEnd</c>) columns. Only the two the spec adds are declared here.
/// </remarks>
public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        // §5.4 "creation data is immutable": both are set once, at construction, and
        // never written again. Guid keys have no store-side default to fall back on.
        Id = Guid.CreateVersion7();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Shown in the nav and in movie night. 1–50 characters after trimming.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
