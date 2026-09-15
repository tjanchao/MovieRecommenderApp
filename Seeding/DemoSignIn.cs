using movieRecommender.Identity;

namespace movieRecommender.Seeding;

/// <summary>
/// Whether one-click demo sign-in exists, and for whom — spec 003-004 FR-018, FR-019.
/// </summary>
/// <remarks>
/// One-click sign-in is an authentication bypass: it signs a user in without verifying a
/// password, which also bypasses 001-002's lockout. The spec accepts that — lockout exists
/// to stop password guessing and this path guesses nothing — and fences it instead (§3.1).
/// This type is the fence.
/// <para>
/// NFR-003 requires the gate to be server-side and evaluated at request time; a hidden
/// control or a disabled button does not satisfy it. So <see cref="IsAvailable"/> is asked
/// afresh on every call, the login page renders nothing when it is false, and the handler
/// behind it answers a forged request with a plain not-found (EC-17).
/// </para>
/// <para>
/// Registered unconditionally, and so is the fixture — which is empty outside Development,
/// where it is never read from disk. Registering conditionally would mean every page that
/// touches the demo has to cope with a service that may not resolve, and "may not resolve"
/// fails as an exception rather than as the not-found EC-17 asks for.
/// </para>
/// </remarks>
public sealed class DemoSignIn
{
    private readonly IHostEnvironment _environment;
    private readonly DemoSeedFixture _fixture;

    public DemoSignIn(IHostEnvironment environment, DemoSeedFixture fixture)
    {
        _environment = environment;
        _fixture = fixture;
    }

    public bool IsAvailable => _environment.IsDevelopment() && _fixture.Accounts.Count > 0;

    public IReadOnlyList<DemoSignInOption> Options =>
        IsAvailable
            ? [.. _fixture.Accounts.Select(account => new DemoSignInOption(
                EmailNormalizer.Normalize(account.Email),
                account.DisplayName,
                account.TasteLabel,
                account.Password))]
            : [];

    /// <summary>
    /// The one place an email from a request is allowed to select an account, and only by
    /// matching the fixture exactly. Anything else is not-found.
    /// </summary>
    public DemoSignInOption? Find(string? email)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = EmailNormalizer.Normalize(email);
        return Options.FirstOrDefault(option => option.Email == normalized);
    }
}

/// <param name="Password">
/// Rendered on the login page next to the button. Published in the repository on purpose
/// (FR-017): the demo password has to work typed as well as clicked, or one-click sign-in
/// becomes the only way in.
/// </param>
public sealed record DemoSignInOption(string Email, string DisplayName, string TasteLabel, string Password);
