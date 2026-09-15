using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using movieRecommender.Security;

namespace movieRecommender.Data;

/// <summary>
/// The application's data layer — the first in the project (spec 001-002 §9.1), and since
/// spec 003-004 the single place ownership is enforced (FR-002, NFR-002).
/// </summary>
/// <remarks>
/// Derives from <see cref="IdentityUserContext{TUser, TKey}"/> rather than
/// <c>IdentityDbContext</c>: roles and permissions are an explicit non-goal (001-002 §1.3),
/// so the role tables are left out of the schema entirely.
/// <para>
/// <b>Ownership.</b> Every entity implementing <see cref="IOwnedRecord"/> is given a global
/// query filter scoping it to <see cref="CurrentUserId"/>, and every write through it is
/// checked. The alternative — one <c>where OwnerUserId == currentUser</c> per query — works
/// right up until the query someone forgets, and a forgotten filter is invisible in review
/// because it looks like a query that simply doesn't need one. Doing it here makes the
/// omission structurally impossible rather than merely discouraged (§3.1,
/// <see href="../docs/architecture/adr/0003-ownership-enforcement.md">ADR-0003</see>).
/// </para>
/// </remarks>
public class ApplicationDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    private readonly ICurrentUser? _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// The acting user, read fresh on every query because the filter below closes over
    /// this context instance and EF re-evaluates it per query rather than baking it into
    /// the cached model.
    /// </summary>
    /// <remarks>
    /// <c>null</c> outside a request — startup seeding, for one. That is the safe
    /// direction: the filter becomes <c>OwnerUserId == null</c>, which matches no row.
    /// Code that legitimately works across accounts (only the seeder does) has to say so
    /// out loud with <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{T}"/>.
    /// </remarks>
    public Guid? CurrentUserId => _currentUser?.Id;

    public DbSet<Film> Films => Set<Film>();

    public DbSet<Rating> Ratings => Set<Rating>();

    public DbSet<WatchHistoryEntry> WatchHistory => Set<WatchHistoryEntry>();

    public DbSet<WatchlistEntry> Watchlist => Set<WatchlistEntry>();

    public DbSet<DismissedRecommendation> DismissedRecommendations => Set<DismissedRecommendation>();

    public DbSet<TasteProfile> TasteProfiles => Set<TasteProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            // The 1–50 limit in 001-002 §5.1 is counted in text elements and enforced in
            // validation; the column is given headroom because a single emoji costs
            // two UTF-16 units.
            user.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            user.Property(u => u.CreatedAt).IsRequired();

            // 001-002 §5.4: "enforced at the store, not only in validation, so that two
            // concurrent sign-ups still yield one account" (EC-6). Identity's own
            // EmailIndex is non-unique — only the username index is — so it is
            // redeclared here rather than relied upon.
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
        });

        builder.Entity<Film>(film =>
        {
            // §5.3: the application never mints a film identifier of its own, so the
            // TMDB id is the key rather than a surrogate with a unique index beside it.
            film.HasKey(f => f.TmdbId);
            film.Property(f => f.TmdbId).ValueGeneratedNever();
            film.Property(f => f.Title).IsRequired().HasMaxLength(300);
            film.Property(f => f.ReleaseYear).IsRequired();
            film.Ignore(f => f.Era);
        });

        builder.Entity<Rating>(rating =>
        {
            rating.Property(r => r.RatedAt).IsRequired();
            ReferenceFilm(rating);
            // One opinion per film per user; re-rating updates rather than accumulates.
            rating.HasIndex(r => new { r.OwnerUserId, r.TmdbId }).IsUnique();
        });

        builder.Entity<WatchHistoryEntry>(entry =>
        {
            entry.Property(e => e.WatchedAt).IsRequired();
            ReferenceFilm(entry);
            // Not unique: rewatching the same film is two events, not one.
            entry.HasIndex(e => new { e.OwnerUserId, e.WatchedAt });
        });

        builder.Entity<WatchlistEntry>(entry =>
        {
            entry.Property(e => e.AddedAt).IsRequired();
            ReferenceFilm(entry);
            entry.HasIndex(e => new { e.OwnerUserId, e.TmdbId }).IsUnique();
        });

        builder.Entity<DismissedRecommendation>(dismissal =>
        {
            dismissal.Property(d => d.DismissedAt).IsRequired();
            ReferenceFilm(dismissal);
            dismissal.HasIndex(d => new { d.OwnerUserId, d.TmdbId }).IsUnique();
        });

        builder.Entity<TasteProfile>(profile =>
        {
            profile.Property(p => p.TasteLabel).IsRequired().HasMaxLength(200);
            // One per user (§5.1).
            profile.HasIndex(p => p.OwnerUserId).IsUnique();
        });

        ConfigureOwnership(builder);
    }

    /// <summary>
    /// Points a personal record's <c>Film</c> navigation at its own <c>TmdbId</c> column.
    /// </summary>
    /// <remarks>
    /// Required, not decorative. EF's foreign-key conventions look for <c>FilmTmdbId</c> or
    /// <c>FilmId</c> — never the bare principal key name — so left alone it invents a shadow
    /// <c>FilmTmdbId</c> property beside the <c>TmdbId</c> the code actually sets. The rows
    /// then save with a zero foreign key and the insert fails on the constraint.
    /// <para>
    /// <see cref="DeleteBehavior.Restrict"/> rather than cascade: §5.3's films are shared
    /// catalogue data, and removing one out from under the ratings that reference it should
    /// fail loudly rather than silently delete a user's opinions.
    /// </para>
    /// </remarks>
    private static void ReferenceFilm(EntityTypeBuilder entity) =>
        entity.HasOne(typeof(Film), nameof(Rating.Film))
            .WithMany()
            .HasForeignKey(nameof(Rating.TmdbId))
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

    /// <summary>
    /// Stores every <see cref="DateTimeOffset"/> on an owned record as UTC ticks.
    /// </summary>
    /// <remarks>
    /// SQLite has no date type, and the provider refuses to translate <c>ORDER BY</c> over a
    /// <see cref="DateTimeOffset"/> at all — the default TEXT mapping throws at query time,
    /// not at model-build time, so the failure surfaces as a 500 on the first page that
    /// sorts history by date. Since every timestamp this application writes is UTC (see
    /// <see cref="OwnedRecord"/> and the seeder), an integer tick count loses nothing and
    /// sorts and compares in SQL.
    /// <para>
    /// Applied by sweep, alongside the ownership rules, for the same reason: stories 009–024
    /// each add a dated personal record, and each would otherwise rediscover this.
    /// Deliberately *not* applied to the Identity tables — their columns already hold data
    /// under 001-002's migration, nothing sorts by them, and converting them in place would
    /// mean rewriting rows for no gain.
    /// </para>
    /// </remarks>
    private static void StoreTimestampsAsTicks(ModelBuilder builder, Type clrType)
    {
        var entityType = builder.Entity(clrType).Metadata;

        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(DateTimeOffset))
            {
                property.SetValueConverter(UtcTicks);
            }
            else if (property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(NullableUtcTicks);
            }
        }
    }

    private static readonly ValueConverter<DateTimeOffset, long> UtcTicks =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    private static readonly ValueConverter<DateTimeOffset?, long?> NullableUtcTicks =
        new(value => value == null ? null : value.Value.UtcTicks,
            ticks => ticks == null ? null : new DateTimeOffset(ticks.Value, TimeSpan.Zero));

    /// <summary>
    /// Attaches the ownership filter, the owner foreign key and the owner index to every
    /// <see cref="IOwnedRecord"/> in the model, whatever they turn out to be.
    /// </summary>
    /// <remarks>
    /// Written as a sweep over the model rather than a list of entity types on purpose.
    /// NFR-005: adding a new kind of personal record must require no new authorization
    /// code, and a list is exactly the kind of thing a later story forgets to add to. A
    /// class that implements the interface is protected by having done so.
    /// </remarks>
    private void ConfigureOwnership(ModelBuilder builder)
    {
        var ownedTypes = builder.Model.GetEntityTypes()
            .Where(entityType => typeof(IOwnedRecord).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in ownedTypes)
        {
            var entity = builder.Entity(clrType);

            // §5.4 "no ownerless personal record": a non-nullable FK to an existing user,
            // enforced by the store and not only by the guard in SaveChanges. Cascade so
            // deleting an account takes its personal data with it.
            entity.HasOne(typeof(ApplicationUser))
                .WithMany()
                .HasForeignKey(nameof(IOwnedRecord.OwnerUserId))
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(nameof(IOwnedRecord.OwnerUserId));
            StoreTimestampsAsTicks(builder, clrType);

            // FR-002. Built by hand because the filter has to be applied per entity type
            // and there is no common base EF can hang one lambda off.
            //
            //     e => (Guid?)e.OwnerUserId == this.CurrentUserId
            //
            // The reference to `this` is what keeps the filter dynamic: EF recognizes a
            // constant of the context's own type and turns the property read into a query
            // parameter, so the model stays cacheable while the value follows the request.
            var parameter = Expression.Parameter(clrType, "e");
            var owner = Expression.Convert(
                Expression.Property(parameter, nameof(IOwnedRecord.OwnerUserId)),
                typeof(Guid?));
            var actor = Expression.Property(Expression.Constant(this), nameof(CurrentUserId));

            entity.HasQueryFilter(Expression.Lambda(Expression.Equal(owner, actor), parameter));
        }
    }

    /// <summary>
    /// Suspends the "you may only write your own records" check for the duration of the
    /// returned scope. The write-side counterpart of
    /// <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{T}"/>.
    /// </summary>
    /// <remarks>
    /// The demo seeder is the only caller, and the only code in the project that legitimately
    /// writes records for accounts other than the acting user. Startup seeding does not need
    /// it — there is no acting user at startup — but <see cref="DbSet{T}"/> writes during a
    /// <c>/Demo/Reset</c> post happen inside a request, where the acting user is whoever
    /// clicked the button and the records being written belong to both demo accounts.
    /// <para>
    /// Deliberately verbose and deliberately narrow. It suspends <i>only</i> the actor
    /// comparison: a record still cannot be saved without an owner, and ownership still
    /// cannot be changed. Grep for it the way you would grep for
    /// <c>IgnoreQueryFilters</c> — every use should be arguable
    /// (<see href="../docs/architecture/adr/0003-ownership-enforcement.md">ADR-0003</see>).
    /// </para>
    /// </remarks>
    public IDisposable WriteOnBehalfOfAnyOwner() => new SystemWriteScope(this);

    private bool _writingOnBehalfOfAnyOwner;

    private sealed class SystemWriteScope : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public SystemWriteScope(ApplicationDbContext context)
        {
            _context = context;
            _context._writingOnBehalfOfAnyOwner = true;
        }

        public void Dispose() => _context._writingOnBehalfOfAnyOwner = false;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// The write half of the chokepoint: FR-001, §5.4's immutability rule, and the
    /// symmetric form of FR-002.
    /// </summary>
    /// <remarks>
    /// The read half is the query filter, which means a record belonging to someone else
    /// cannot normally be loaded and so cannot be tracked for modification. The third
    /// check below therefore fires only on a record built by hand with a foreign owner —
    /// which is the one way the filter can be walked around.
    /// </remarks>
    private void EnforceOwnership()
    {
        var actor = _writingOnBehalfOfAnyOwner ? null : CurrentUserId;

        foreach (var entry in ChangeTracker.Entries<IOwnedRecord>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            // FR-001 / SC-020. Not "a record with a null column" — a rejected save.
            if (entry.Entity.OwnerUserId == Guid.Empty)
            {
                throw new OwnershipViolationException(
                    $"{entry.Metadata.DisplayName()} cannot be saved without an owner.");
            }

            if (entry.State is EntityState.Modified)
            {
                var ownerProperty = entry.Property(nameof(IOwnedRecord.OwnerUserId));
                if (!Equals(ownerProperty.OriginalValue, ownerProperty.CurrentValue))
                {
                    // §5.4 "ownership is immutable": no transfer, no re-parenting, no merge.
                    throw new OwnershipViolationException(
                        $"{entry.Metadata.DisplayName()} {entry.Entity.Id} cannot change owner " +
                        $"from {ownerProperty.OriginalValue} to {ownerProperty.CurrentValue}.");
                }
            }

            // Outside a request there is no acting user — startup seeding writes records
            // for accounts nobody is signed in as, and so does a demo reset, which says so
            // via WriteOnBehalfOfAnyOwner. Inside one, you write your own data or you write
            // nothing.
            if (actor is not null && entry.Entity.OwnerUserId != actor)
            {
                throw new OwnershipViolationException(
                    $"User {actor} may not write a {entry.Metadata.DisplayName()} owned by " +
                    $"{entry.Entity.OwnerUserId}.");
            }
        }
    }
}
