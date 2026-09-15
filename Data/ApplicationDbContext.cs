using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace movieRecommender.Data;

/// <summary>
/// The application's data layer — the first in the project (spec 001-002 §9.1).
/// </summary>
/// <remarks>
/// Derives from <see cref="IdentityUserContext{TUser, TKey}"/> rather than
/// <c>IdentityDbContext</c>: roles and permissions are an explicit non-goal (§1.3), so
/// the role tables are left out of the schema entirely.
/// </remarks>
public class ApplicationDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            // The 1–50 limit in §5.1 is counted in text elements and enforced in
            // validation; the column is given headroom because a single emoji costs
            // two UTF-16 units.
            user.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            user.Property(u => u.CreatedAt).IsRequired();

            // §5.4: "enforced at the store, not only in validation, so that two
            // concurrent sign-ups still yield one account" (EC-6). Identity's own
            // EmailIndex is non-unique — only the username index is — so it is
            // redeclared here rather than relied upon.
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
        });
    }
}
