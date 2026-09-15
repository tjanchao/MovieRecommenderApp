# ADR-0001: EF Core over SQLite as the persistence store

| Field    | Value                                                                     |
| -------- | ------------------------------------------------------------------------- |
| Status   | Accepted                                                                  |
| Date     | 2026-09-15                                                                |
| Deciders | tjanfei chao                                                              |
| Context  | Spec [001-002](../../specs/001-002-accounts-and-sessions.md) §9.1, §10 Q1 |

## Context

Spec 001-002 introduces the first persistence in the project — accounts have to exist
between requests, and between runs of the application. §9.1 is explicit that the choice
must be made with story 007 in view as well, because the same store will hold locally
cached films; spec 005-008 §9.1 restates that the decision is owed and now carries
further weight.

The forces:

- **EC-15 and SUC-003** require a "Remember me" sign-in to survive an application
  restart. An in-memory store cannot satisfy this — the account behind the cookie would
  be gone.
- **NFR-006** sizes the system at tens of accounts and a handful of concurrent sessions,
  one process on one machine. Nothing here needs a server.
- **NFR-005** requires sign-up and login to work with every external dependency down.
  The store is the one thing they may depend on, so it must be local.
- **§9.2** constrains the whole project to localhost, and there is no deployment story.
- The demo is reset between runs, so "delete a file and start again" is a feature.
- Spec 005-008 will add a film cache with staleness-based refresh — a second entity set
  with its own indexes and migrations.

## Decision

Use **Entity Framework Core with the SQLite provider**, against a file at
`App_Data/movieRecommender.db`, with schema changes managed as EF Core migrations under
`Data/Migrations/`.

Migrations are applied at startup (`Database.Migrate()` in `Program.cs`), which keeps
running the demo to a single `dotnet run`.

## Alternatives Considered

**SQL Server LocalDB.** Windows-native and already present on some developer machines,
but it ties the demo to a machine with LocalDB installed and to a service that has to be
running. Resetting between demos is a `sqlcmd` session rather than deleting a file. None
of its advantages over SQLite — concurrency, scale, tooling — are things NFR-006 asks
for.

**EF Core InMemory.** The least setup, and genuinely useful for tests later. Rejected
outright for the application itself: it loses every account on restart, which directly
contradicts EC-15 and SUC-003, and it is not a relational store, so the unique index
that §5.4 relies on for EC-6 would not be enforced.

**A JSON or file-based store with no ORM.** Tempting at the scale of "tens of accounts",
but it would have to grow uniqueness enforcement, indexing and a migration story by
hand, and story 007's film cache would push it further. It also rules out
`AddEntityFrameworkStores`, which is what makes ADR-0002 cheap.

## Consequences

**Positive**

- Accounts and data-protection keys both live on disk, so EC-15 and SUC-003 hold.
- The unique index on `NormalizedEmail` enforces §5.4's uniqueness invariant at the
  store, so EC-6's concurrent sign-up yields one account rather than two.
- Identity plugs straight in via `AddEntityFrameworkStores`, which is a precondition of
  ADR-0002.
- Resetting the demo is `rm App_Data/movieRecommender.db`.
- Migrations give story 005-008 a defined way to add the film cache.

**Negative**

- SQLite has a single writer. Irrelevant at NFR-006's scale, but it is not a property to
  build on if this ever becomes multi-user in earnest.
- SQLite ignores column length constraints, so `HasMaxLength` is documentation rather
  than enforcement. Validation, not the schema, is what actually holds EC-2.
- `Database.Migrate()` at startup is wrong for any deployment with more than one
  instance. Recorded here so it is revisited rather than inherited.
- The database file must not be committed; it is in `.gitignore` alongside the
  data-protection key ring.

## Related

- [ADR-0002: ASP.NET Core Identity](0002-identity-mechanism.md) — depends on this one
- Spec 001-002 §5, §9.1 — the entity and the dependency this resolves
- Spec 005-008 §9.1 — the film cache that shares this store
