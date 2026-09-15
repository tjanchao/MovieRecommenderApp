using System.Text.Json;
using System.Text.Json.Serialization;
using movieRecommender.Identity;

namespace movieRecommender.Seeding;

/// <summary>
/// Reads <c>Seed/demo-seed.json</c> and refuses to hand back a fixture that would make a
/// worse demo than no fixture at all.
/// </summary>
/// <remarks>
/// The validation here is the load-bearing part. §5.4 notes that the seeded tastes have to
/// be different enough for FR-015 and alike enough for FR-016 — two invariants pulling in
/// opposite directions, either of which a casual edit to the fixture will break. EC-18
/// calls the resulting failure "the intended tripwire": without it, movie night silently
/// loses its winner and the failure surfaces three stories later, in 029.
/// <para>
/// Running the checks at load time rather than in a test is a deliberate choice for this
/// project — the tripwire fires on <c>dotnet run</c>, which per NFR-004 is exactly where
/// discovering a broken demo is still recoverable.
/// </para>
/// </remarks>
public sealed class DemoSeedFixtureLoader
{
    public const string FixtureFileName = "demo-seed.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IHostEnvironment _environment;

    public DemoSeedFixtureLoader(IHostEnvironment environment) => _environment = environment;

    public string FixturePath => Path.Combine(_environment.ContentRootPath, "Seed", FixtureFileName);

    public DemoSeedFixture Load()
    {
        if (!File.Exists(FixturePath))
        {
            throw new InvalidOperationException($"The demo seed fixture is missing: {FixturePath}");
        }

        DemoSeedFixture? fixture;
        try
        {
            fixture = JsonSerializer.Deserialize<DemoSeedFixture>(File.ReadAllText(FixturePath), SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"The demo seed fixture at {FixturePath} is not valid JSON: {exception.Message}", exception);
        }

        if (fixture is null)
        {
            throw new InvalidOperationException($"The demo seed fixture at {FixturePath} is empty.");
        }

        Validate(fixture);
        return fixture;
    }

    private static void Validate(DemoSeedFixture fixture)
    {
        var problems = new List<string>();

        problems.AddRange(ValidateAccountsExist(fixture));
        problems.AddRange(ValidateFilmReferences(fixture));
        problems.AddRange(ValidateTastesDiffer(fixture));
        problems.AddRange(ValidateTastesOverlap(fixture));

        if (problems.Count > 0)
        {
            // NFR-004: name what failed. Discovering an empty demo at `dotnet run` is
            // recoverable; discovering it on stage is not.
            throw new InvalidOperationException(
                $"The demo seed fixture is not usable:{Environment.NewLine}  - " +
                string.Join($"{Environment.NewLine}  - ", problems));
        }
    }

    private static IEnumerable<string> ValidateAccountsExist(DemoSeedFixture fixture)
    {
        // FR-011: "at least two demo accounts". Data isolation cannot be demonstrated
        // with one account — 004 is the fixture that proves 003 (§1).
        if (fixture.Accounts.Count < 2)
        {
            yield return $"at least two demo accounts are required; the fixture defines {fixture.Accounts.Count}";
        }

        var duplicates = fixture.Accounts
            .GroupBy(account => EmailNormalizer.Normalize(account.Email))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var email in duplicates)
        {
            yield return $"'{email}' is defined more than once; email is the idempotence key (§5.4)";
        }

        foreach (var account in fixture.Accounts)
        {
            // FR-011 names all four; an account missing one is a demo page that opens empty.
            if (account.Ratings.Count == 0) yield return $"'{account.Email}' has no ratings";
            if (account.History.Count == 0) yield return $"'{account.Email}' has no watch history";
            if (account.Watchlist.Count == 0) yield return $"'{account.Email}' has no watchlist entries";
            if (account.PreferredGenres.Count == 0) yield return $"'{account.Email}' states no preferred genres";
        }
    }

    /// <summary>EC-20: a rating pointing at a film that is not in the pool points at nothing.</summary>
    private static IEnumerable<string> ValidateFilmReferences(DemoSeedFixture fixture)
    {
        var duplicateFilms = fixture.Films
            .GroupBy(film => film.TmdbId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var tmdbId in duplicateFilms)
        {
            yield return $"film {tmdbId} appears more than once; a film has one identity (§5.3)";
        }

        var pool = fixture.Films.Select(film => film.TmdbId).ToHashSet();

        foreach (var account in fixture.Accounts)
        {
            var referenced = account.Ratings.Select(rating => rating.TmdbId)
                .Concat(account.History.Select(entry => entry.TmdbId))
                .Concat(account.Watchlist.Select(entry => entry.TmdbId));

            foreach (var orphan in referenced.Where(tmdbId => !pool.Contains(tmdbId)).Distinct())
            {
                yield return $"'{account.Email}' references film {orphan}, which is not in the seed film pool";
            }
        }
    }

    /// <summary>
    /// FR-015, in the weaker form §10 question 1 settles on until story 013 supplies
    /// ranked recommendations: stated tastes are disjoint, and no two accounts like the
    /// same film outside the intersection FR-016 asks for.
    /// </summary>
    private static IEnumerable<string> ValidateTastesDiffer(DemoSeedFixture fixture)
    {
        var shared = SharedPositives(fixture);

        foreach (var (first, second) in Pairs(fixture.Accounts))
        {
            var genres = first.PreferredGenres.Intersect(second.PreferredGenres, StringComparer.OrdinalIgnoreCase);
            if (genres.Any())
            {
                yield return
                    $"'{first.Email}' and '{second.Email}' both prefer {string.Join(", ", genres)}; " +
                    "stated tastes must be disjoint (FR-015)";
            }

            var eras = first.PreferredEras.Intersect(second.PreferredEras, StringComparer.OrdinalIgnoreCase);
            if (eras.Any())
            {
                yield return
                    $"'{first.Email}' and '{second.Email}' both prefer the {string.Join(", ", eras)}; " +
                    "stated eras must be disjoint (FR-015)";
            }

            // An accidental agreement is indistinguishable from the designed one, and it
            // is the designed one movie night is built on. Keep the intersection deliberate.
            var accidental = first.PositivelyRatedFilms.Intersect(second.PositivelyRatedFilms)
                .Except(shared)
                .ToList();

            if (accidental.Count > 0)
            {
                yield return
                    $"'{first.Email}' and '{second.Email}' both rate {string.Join(", ", accidental)} positively, " +
                    "but not every account does; the overlap must be designed, not incidental (FR-015, §3.1)";
            }
        }
    }

    /// <summary>
    /// FR-016 and EC-18. Sharpening the contrast until the last shared film is gone is the
    /// edit this exists to catch: story 029 ("the first film everyone said yes to") would
    /// have nothing to return.
    /// </summary>
    private static IEnumerable<string> ValidateTastesOverlap(DemoSeedFixture fixture)
    {
        if (fixture.Accounts.Count >= 2 && SharedPositives(fixture).Count == 0)
        {
            yield return
                "no film is rated positively by every demo account; a blended movie-night " +
                "shortlist would have no winner (FR-016, SC-015, EC-18)";
        }
    }

    private static HashSet<int> SharedPositives(DemoSeedFixture fixture) =>
        fixture.Accounts
            .Select(account => account.PositivelyRatedFilms.ToHashSet())
            .Aggregate(new HashSet<int>(fixture.Films.Select(film => film.TmdbId)), (shared, liked) =>
            {
                shared.IntersectWith(liked);
                return shared;
            });

    private static IEnumerable<(DemoAccountDefinition First, DemoAccountDefinition Second)> Pairs(
        IReadOnlyList<DemoAccountDefinition> accounts)
    {
        for (var i = 0; i < accounts.Count; i++)
        {
            for (var j = i + 1; j < accounts.Count; j++)
            {
                yield return (accounts[i], accounts[j]);
            }
        }
    }
}
