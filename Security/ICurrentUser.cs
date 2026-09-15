namespace movieRecommender.Security;

/// <summary>
/// Whose data the current request may read and write — spec 003-004 FR-006.
/// </summary>
/// <remarks>
/// The one place the acting user is decided. It reads the authentication cookie's
/// principal and nothing else: no route value, query string, form field or header can
/// change the answer (§5.4 "acting user is the cookie's user", EC-3, SC-005).
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The signed-in user's id, or <c>null</c> when the request is anonymous.</summary>
    Guid? Id { get; }

    bool IsSignedIn { get; }
}
