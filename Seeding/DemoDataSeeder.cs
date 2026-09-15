using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using movieRecommender.Data;
using movieRecommender.Identity;

namespace movieRecommender.Seeding;

/// <summary>
/// Turns the checked-in fixture into working accounts — spec 003-004 FR-011 through FR-014,
/// FR-021, FR-022.
/// </summary>
/// <remarks>
/// Calls nothing external: no TMDB, no Claude, no network, no API key (FR-013, NFR-008).
/// A fresh clone and <c>dotnet run</c> is the whole setup.
/// <para>
/// Every write goes through the same <see cref="ApplicationDbContext"/> guard as any other,
/// so the seeded records are ordinary owned records and the seeded accounts are ordinary
/// accounts (FR-020). Nothing in the system branches on whether a user was seeded.
/// </para>
/// </remarks>
public sealed class DemoDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DemoSeedFixture _fixture;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        DemoSeedFixture fixture,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _fixture = fixture;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the demo accounts exist, with their data. Idempotent: a second startup
    /// neither duplicates an account nor duplicates its records (FR-012, SC-011, EC-7).
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // §5.4 "seeding is all-or-nothing": a half-seeded account is worse than no
        // account, because it looks like it worked (NFR-004, EC-9).
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var films = await EnsureFilmsAsync(cancellationToken);
        var created = 0;

        foreach (var definition in _fixture.Accounts)
        {
            var email = EmailNormalizer.Normalize(definition.Email);

            // §5.4 "seed idempotence is keyed on email", using 001-002's normalization.
            // If a user with that email exists, seeding touches nothing about it.
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                // EC-8: a real sign-up may have claimed the address before seeding ever
                // ran. The account is left alone and the demo is quietly not what the
                // fixture describes, which is worth saying out loud.
                _logger.LogInformation(
                    "Demo account {Email} already exists (user {UserId}); seeding skipped it. " +
                    "Its data is whatever that account holds, not what the fixture describes.",
                    email, existing.Id);
                continue;
            }

            var user = await CreateAccountAsync(definition, email);
            PopulateAccount(user.Id, definition, films);
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // FR-010: counts and identifiers, never the contents of a personal record.
        _logger.LogInformation(
            "Demo seeding complete: {Created} account(s) created, {Films} film(s) in the pool.",
            created, films.Count);
    }

    /// <summary>
    /// FR-021 / SC-021: restores the demo accounts to their seeded state, discarding the
    /// ratings and dismissals accumulated during the last run.
    /// </summary>
    /// <remarks>
    /// Deletes and re-creates the owned records but leaves the <see cref="ApplicationUser"/>
    /// rows alone. That is what makes EC-10 true: a demo account signed in elsewhere keeps
    /// its cookie — there is no session entity to invalidate (§5.2) — and simply sees
    /// seeded state on its next page load.
    /// </remarks>
    /// <returns>How many accounts were restored.</returns>
    public async Task<int> ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var films = await EnsureFilmsAsync(cancellationToken);
        var restored = 0;

        foreach (var definition in _fixture.Accounts)
        {
            var email = EmailNormalizer.Normalize(definition.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                // The account was never seeded, or was deleted. Put it back — reset is the
                // recovery path EC-9 points at.
                user = await CreateAccountAsync(definition, email);
            }
            else
            {
                await ClearOwnedRecordsAsync(user.Id, cancellationToken);
            }

            PopulateAccount(user.Id, definition, films);
            restored++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Demo reset complete: {Restored} account(s) restored to seeded state.", restored);
        return restored;
    }

    /// <summary>
    /// FR-014: films carry their real TMDB ids, so a later live fetch of the same film
    /// reconciles with the seeded record rather than creating a second one (SC-013, EC-11).
    /// </summary>
    private async Task<IReadOnlyDictionary<int, Film>> EnsureFilmsAsync(CancellationToken cancellationToken)
    {
        // Films are shared catalogue data with no owner, so no query filter applies here.
        var existing = await _db.Films.ToDictionaryAsync(film => film.TmdbId, cancellationToken);

        foreach (var seed in _fixture.Films.Where(seed => !existing.ContainsKey(seed.TmdbId)))
        {
            var film = new Film
            {
                TmdbId = seed.TmdbId,
                Title = seed.Title,
                ReleaseYear = seed.ReleaseYear,
                RuntimeMin = seed.RuntimeMin,
                Genres = [.. seed.Genres],
                PosterPath = seed.PosterPath,
                Synopsis = seed.Synopsis,
            };

            _db.Films.Add(film);
            existing[film.TmdbId] = film;
        }

        return existing;
    }

    private async Task<ApplicationUser> CreateAccountAsync(DemoAccountDefinition definition, string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = definition.DisplayName,
        };

        // FR-017: the password is hashed exactly as any other account's, so typing the
        // documented credentials into the login form works normally (SC-017). The demo
        // account is an ordinary account that happens to have been created by us.
        var result = await _userManager.CreateAsync(user, definition.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create demo account '{email}': " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        return user;
    }

    /// <summary>
    /// FR-022 / SC-022: the fixture expresses ages, never calendar dates, so history
    /// seeded a year from now still reads as recent (EC-19).
    /// </summary>
    private void PopulateAccount(
        Guid ownerUserId,
        DemoAccountDefinition definition,
        IReadOnlyDictionary<int, Film> films)
    {
        var seededAt = DateTimeOffset.UtcNow;

        _db.TasteProfiles.Add(new TasteProfile
        {
            OwnerUserId = ownerUserId,
            TasteLabel = definition.TasteLabel,
            PreferredGenres = [.. definition.PreferredGenres],
            PreferredEras = [.. definition.PreferredEras],
        });

        foreach (var rating in definition.Ratings)
        {
            _db.Ratings.Add(new Rating
            {
                OwnerUserId = ownerUserId,
                TmdbId = Require(films, rating.TmdbId, definition.Email),
                Sentiment = rating.Sentiment,
                RatedAt = seededAt.AddDays(-rating.DaysAgo),
            });
        }

        foreach (var entry in definition.History)
        {
            _db.WatchHistory.Add(new WatchHistoryEntry
            {
                OwnerUserId = ownerUserId,
                TmdbId = Require(films, entry.TmdbId, definition.Email),
                WatchedAt = seededAt.AddDays(-entry.DaysAgo),
            });
        }

        foreach (var entry in definition.Watchlist)
        {
            _db.Watchlist.Add(new WatchlistEntry
            {
                OwnerUserId = ownerUserId,
                TmdbId = Require(films, entry.TmdbId, definition.Email),
                AddedAt = seededAt.AddDays(-entry.DaysAgo),
            });
        }
    }

    /// <summary>
    /// EC-20, restated at the point of use. <see cref="DemoSeedFixtureLoader"/> has already
    /// rejected an orphaned id, so reaching this means the film pool changed underneath us.
    /// </summary>
    private static int Require(IReadOnlyDictionary<int, Film> films, int tmdbId, string email) =>
        films.ContainsKey(tmdbId)
            ? tmdbId
            : throw new InvalidOperationException(
                $"Demo account '{email}' references film {tmdbId}, which is not in the seed film pool.");

    /// <summary>
    /// Removes one account's personal records. The query filter is bypassed deliberately
    /// and visibly: reset runs outside any request, so there is no acting user for the
    /// filter to scope to, and the scope is supplied here instead.
    /// </summary>
    private async Task ClearOwnedRecordsAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        await DeleteOwnedAsync<Rating>(ownerUserId, cancellationToken);
        await DeleteOwnedAsync<WatchHistoryEntry>(ownerUserId, cancellationToken);
        await DeleteOwnedAsync<WatchlistEntry>(ownerUserId, cancellationToken);
        await DeleteOwnedAsync<DismissedRecommendation>(ownerUserId, cancellationToken);
        await DeleteOwnedAsync<TasteProfile>(ownerUserId, cancellationToken);
    }

    private Task DeleteOwnedAsync<TRecord>(Guid ownerUserId, CancellationToken cancellationToken)
        where TRecord : class, IOwnedRecord =>
        _db.Set<TRecord>()
            .IgnoreQueryFilters()
            .Where(record => record.OwnerUserId == ownerUserId)
            .ExecuteDeleteAsync(cancellationToken);
}
