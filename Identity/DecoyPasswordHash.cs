using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using movieRecommender.Data;

namespace movieRecommender.Identity;

/// <summary>
/// A throwaway hash to verify against when a login names an email that has no account.
/// </summary>
/// <remarks>
/// NFR-003: a failed login must take indistinguishable time whether the email is unknown
/// or the password is wrong. Without this, an unknown email skips the KDF entirely and
/// answers in single-digit milliseconds, leaking through response timing exactly what
/// FR-007 refuses to say in words.
/// <para>
/// Registered as a singleton so the one real hashing cost is paid at startup. The hasher
/// is constructed directly rather than injected because <see cref="IPasswordHasher{TUser}"/>
/// is scoped.
/// </para>
/// </remarks>
public sealed class DecoyPasswordHash
{
    public DecoyPasswordHash(IOptions<PasswordHasherOptions> options)
    {
        Value = new PasswordHasher<ApplicationUser>(options)
            .HashPassword(new ApplicationUser(), Guid.NewGuid().ToString("N"));
    }

    public string Value { get; }
}
