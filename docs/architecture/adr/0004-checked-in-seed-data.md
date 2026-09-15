# ADR-0004: Demo seed data is checked in, not fetched from TMDB

| Field    | Value                                                                              |
| -------- | ---------------------------------------------------------------------------------- |
| Status   | Accepted                                                                           |
| Date     | 2026-09-15                                                                         |
| Deciders | tjanfei chao                                                                       |
| Context  | Spec [003-004](../../specs/003-004-private-data-and-demo-accounts.md) §3.1, §9.3(b) |

## Context

Story 004 needs two demo accounts that already hold ratings, watch history, a watchlist
and a taste profile, so that story 003's isolation claim has something to be true *about*.
Those records reference films, and films have titles, years, genres and TMDB ids.

The films are real. The question is where their metadata comes from at seed time.

What the spec demands:

- **FR-013 / NFR-008**: seeding calls nothing external. No TMDB, no network, no API key.
  A fresh clone with neither must still produce working accounts.
- **FR-014**: the films carry *real* TMDB ids, so that story 007's cache and story 013's
  recommendations can resolve them later without a re-key.
- **FR-012**: seeding is idempotent — running it twice produces the same state.
- **FR-022**: timestamps are relative to seed time, so a demo run six months from now
  does not show "watched 2026".
- **NFR-004**: if seeding cannot do its job, startup aborts naming what failed rather
  than starting an unseeded app.
- **§9.2**: TMDB's terms require attribution for use of their data.

The pull the other way is real too: hand-written metadata can be *wrong* — a mistyped id
points at a different film, and nothing notices until story 013 renders it.

## Decision

Check the fixture into the repository as **`Seed/demo-seed.json`**, copied to the output
directory, read from the content root at startup, and deserialized into
`DemoSeedFixture` by `DemoSeedFixtureLoader`.

The file holds one pool of ~40 films with real TMDB ids, and two account definitions
(Ava, Milo) that reference those films by id only. `DemoDataSeeder` upserts the films by
`TmdbId`, creates each account through `UserManager.CreateAsync` so the published password
is hashed by the normal path, and stamps every rating, history entry and watchlist entry
as `seededAt.AddDays(-daysAgo)` — the file stores offsets, never calendar dates (FR-022).

Idempotence is by normalized email (FR-012, EC-8): an account that already exists is
skipped and logged, not recreated. Films upsert by primary key. Re-running `dotnet run`
therefore changes nothing.

**The fixture validates itself at load, and a failure aborts startup.** This is where the
decision earns its keep, because it is what replaces the tests the spec asked for in
SUC-001. `DemoSeedFixtureLoader` throws an `InvalidOperationException` naming every
problem it finds when:

- fewer than two accounts exist, or two share a normalized email;
- an account has no ratings, no history, no watchlist or no preferred genres;
- two films share a `TmdbId`, or an account references an id that is not in the pool
  (EC-20 — the mistyped-id failure, caught at `dotnet run` instead of in story 013);
- the two accounts' preferred genres or eras are not disjoint, or their positively-rated
  films overlap outside the designed intersection (the weaker form of FR-015 that §10 Q1
  settles on until story 013 lands);
- that designed intersection is empty (FR-016 — movie night needs something to agree on).

Data-shaping consequences of the invariants: cross-taste ratings in the fixture are all
`Meh`, so the positive-rating intersection is exactly the four films chosen for FR-016.
Ava's preferences are Thriller/Crime/Mystery in the 1990s–2000s; Milo's are
Animation/Comedy/Family in the 2010s–2020s. The `_comment` array in the file records this
tension for whoever edits it next, since adding a plausible-looking film to both accounts
is exactly the edit that breaks FR-015.

Attribution lives in the fixture's `attribution` field, next to the data it covers (§9.2).

`posterPath` is left null throughout — a poster path is a URL into TMDB's image CDN, and
rendering one would reintroduce the network dependency FR-013 removes through the back
door. Story 007 fills it in when it fetches.

## Alternatives Considered

**Fetch from TMDB at seed time, caching the results.** Metadata is then correct by
construction and posters come for free. Rejected outright: it breaks FR-013 and NFR-008,
which are the requirements, not preferences. A demo whose first run needs an API key and
a working connection fails in exactly the situation it exists for — a cloned repo, a
conference wifi, five minutes before a talk. It also makes seeding non-deterministic, so
FR-012's idempotence would depend on TMDB returning the same thing twice.

**C# object initializers in a `DemoData.cs` static class.** No file IO, no JSON, no copy
step, and compile-time checking of the shape. Genuinely tempting, and rejected on two
counts: forty films of initializer syntax is a large unreviewable diff that a reader will
skim, and editing the demo's content would mean recompiling. The shape-checking it offers
is the part validation already covers, and the part it cannot check — whether id 550 really
is *Fight Club* — is the part that actually goes wrong.

**EF Core `HasData` in the migration.** The framework's seeding mechanism, and wrong here
twice over: `HasData` values are baked into a migration, so relative timestamps (FR-022)
become the calendar dates of whenever the migration was generated, and changing the demo
content would mean a new migration. It also cannot go through `UserManager`, so the demo
password would have to be a pre-computed hash in a migration file.

**A separate `dotnet run --seed` command or an EF seeding script.** Keeps seeding out of
the startup path, which is architecturally cleaner. Rejected because it adds a second
command to the demo's instructions, and the failure mode — forgetting it — produces an
empty app at the worst moment. Startup seeding makes "it works after clone and run" the
only path there is.

## Consequences

**Positive**

- FR-013 and NFR-008 hold by construction: the seeder has no `HttpClient` and no
  configuration to read. There is nothing to break offline.
- The demo's content is editable by anyone who can edit JSON, and the diff of such an edit
  is readable in review — which is the main thing a C# version would have lost.
- Validation moved the FR-015/FR-016 invariants and the EC-20 typo check from "asserted by
  a test suite" to "checked on every `dotnet run`", which suits a project that has no test
  project (spec SUC-001 remains open).
- FR-022's relative timestamps mean the fixture does not age. The same file demos
  correctly next year.

**Negative**

- **Nothing verifies that a TMDB id names the film beside it.** Validation catches
  duplicates and dangling references; it cannot catch id 550 being labelled *Se7en*. The
  error would surface in story 007 as a title that changes when the cache fills. This is
  the accepted cost of not calling TMDB, and the reason the ids were taken from TMDB by
  hand rather than invented.
- A small amount of TMDB-derived data now lives under version control (§9.2). Mitigated by
  the attribution field, and bounded — forty rows of title/year/genre.
- The fixture is a file that must reach the output directory. The `Content Update` item in
  the csproj is load-bearing and invisible; if it is dropped, seeding fails at startup with
  a missing-file message rather than silently, which is NFR-004 working as intended.
- Startup now has a failure mode it did not have. A malformed fixture stops the app from
  starting at all — deliberate per NFR-004, but it means a bad JSON edit is a hard stop,
  not a degraded demo.
- The fixture is loaded, and validated, only in Development (FR-019). Outside it the
  registered `DemoSeedFixture` is empty, which is what switches seeding and one-click
  sign-in off. So a fixture error cannot be discovered by a non-Development run — which is
  correct, but worth knowing.

## Related

- [ADR-0001: EF Core over SQLite](0001-persistence-store.md) — the file-backed store seeding
  writes into; `Database.Migrate()` at startup is the hook seeding follows
- [ADR-0002: ASP.NET Core Identity](0002-identity-mechanism.md) — demo accounts are created
  through `UserManager`, so the published password is hashed by the ordinary path
- [ADR-0003: Ownership enforcement](0003-ownership-enforcement.md) — the seeder writes
  records for two different owners, which is why the write guard tolerates a null actor and
  why the reset path uses `IgnoreQueryFilters()`
- Spec 003-004 §3.1, §3.2, §5.1, §9.2, §9.3, §10 Q1 and Q3; FR-011 – FR-022; NFR-004,
  NFR-008; EC-8, EC-10, EC-18, EC-20
