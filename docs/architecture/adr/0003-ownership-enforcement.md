# ADR-0003: Ownership enforced in the data-access layer, not per query

| Field    | Value                                                                              |
| -------- | ---------------------------------------------------------------------------------- |
| Status   | Accepted                                                                           |
| Date     | 2026-09-15                                                                         |
| Deciders | tjanfei chao                                                                       |
| Context  | Spec [003-004](../../specs/003-004-private-data-and-demo-accounts.md) §3.1, §9.3(a) |

## Context

Spec 003-004 requires that personal data be readable only by its owner. The interesting
part is not *that* — it is where the rule lives.

- **NFR-002**: the owner filter is applied in one place, not per page or per query.
- **NFR-005**: a later story that adds a new kind of personal record inherits the rule
  without writing authorization code.
- **FR-002 / FR-007**: a read that does not name an owner returns the acting user's rows,
  not everyone's. There is no "all users' ratings" query to forget to narrow.
- **FR-003 / SC-002 / SC-003**: someone else's record and a record that does not exist
  must be indistinguishable — same status, same body, same timing.
- **FR-006**: the acting user comes from the authentication cookie and from nowhere else.
- **FR-010**: an ownership failure logs the fact, never the record's content.

Stories 009, 010, 022, 023 and 024 will each add a personal entity. Whatever this decision
is, it will be applied five more times by people who are thinking about watchlists, not
about authorization — so the default has to be the safe one.

The failure mode of per-query filtering is not a wrong `.Where()`. It is a *missing*
one, and nothing in a code review, a compiler, or a passing test draws attention to a
clause that isn't there. That asymmetry is the whole argument.

## Decision

Enforce ownership **inside `ApplicationDbContext`**, in two halves.

**Reads — an EF Core global query filter, applied to every `IOwnedRecord` by model sweep.**

`ConfigureOwnership` walks `builder.Model.GetEntityTypes()`, selects those whose CLR type
implements `IOwnedRecord`, and for each one builds `e => (Guid?)e.OwnerUserId == <actor>`
with `System.Linq.Expressions` and hands it to `HasQueryFilter`. It also adds the
`OwnerUserId` foreign key, a cascade delete and an index while it is there.

Three properties follow from that being a sweep rather than a list:

- NFR-005 holds literally. A new entity gets the filter by declaring
  `: OwnedRecord`; there is no registration to forget.
- The filter applies to `Include`d navigations and to `Find` as well as to top-level
  queries, so `SingleOrDefaultAsync(r => r.Id == id)` on another user's row returns
  `null`. FR-003's indistinguishability is then not a page behaving carefully — the page
  cannot tell the two cases apart, so it cannot leak the difference (SC-002, SC-003).
- Reaching past it requires typing `IgnoreQueryFilters()`, which is greppable. Three
  call sites exist, all in the demo seeder's reset path, all deliberate.

The actor side of the comparison is `Expression.Property(Expression.Constant(this),
nameof(CurrentUserId))` — a property access on the context instance, not a captured
`Guid?`. EF then treats it as a query parameter rather than a constant, so one cached
model serves every request and the filter still evaluates per request. Capturing the value
would bake the first request's user into the compiled model.

`CurrentUserId` delegates to `ICurrentUser`, whose implementation can read the
`ClaimsPrincipal` and nothing else — no route values, no query string, no form, no header
(FR-006). It returns `null` outside a request, which is what makes startup seeding
possible.

**Writes — a guard in `SaveChanges`/`SaveChangesAsync`.**

A query filter is read-only; it will happily save a row stamped with someone else's owner.
`EnforceOwnership` walks the `ChangeTracker` for added and modified `IOwnedRecord`s and
throws `OwnershipViolationException` when a record has no owner, when a modification
changes `OwnerUserId`, or when the owner is not the acting user. The exception message
names the entity type and the two ids and never the record's content (FR-010).

When there is no acting user — startup seeding — the actor check is skipped but the
"must have an owner" check is not. Seeding can write rows for Ava and Milo; a request
cannot.

**One escape, on each side, and both have to be typed.**

`IgnoreQueryFilters()` is the read escape. `WriteOnBehalfOfAnyOwner()` is the write one: a
disposable scope on the context that suspends the actor comparison and nothing else — a
record still cannot be saved ownerless, and ownership still cannot change. The demo seeder
is the only caller of either, and it needs the write escape specifically because
`/Demo/Reset` runs *inside* a request: the acting user is whoever clicked the button, while
the rows being written belong to both demo accounts. The guard rejected that, correctly,
the first time reset was exercised. Suspending the check where it is inconvenient is the
easy mistake here; making the suspension a named, greppable, argued-for scope is the point.

## Alternatives Considered

**A repository or service layer that takes the owner as a parameter.** Every personal read
goes through `_ratings.ForUser(userId)`. It makes ownership visible at the call site, which
is a real virtue, and it does not depend on EF. Rejected because it satisfies NFR-002 only
by convention: `ApplicationDbContext` is still injectable and `_db.Ratings` still returns
everything, so the rule holds exactly as long as nobody takes the shortcut. Under this
decision the shortcut is the safe path.

**Per-query `.Where(r => r.OwnerUserId == currentUserId)`.** The obvious thing, and the
thing NFR-002 exists to forbid. Explicit at every call site and impossible to audit: a
missing clause looks like ordinary code. It also puts FR-003 in the hands of every page
author — each one must remember to return not-found rather than forbidden.

**An authorization handler per resource (`IAuthorizationService.AuthorizeAsync(User,
rating, "Owner")`).** The framework's intended answer, and the right one when the rule is
richer than equality — shared documents, org membership, roles. Here it is strictly worse:
it authorizes a record *after* fetching it, so "not yours" and "doesn't exist" are
different code paths that must be made to look alike, and it does nothing for list queries,
which are most of what this app does. Worth revisiting if movie night (stories 027–029)
turns out to need more than the flat projection FR-008 describes.

**Row-level security in the database.** Not available in SQLite (ADR-0001), and it would
move the rule out of the language the rest of the codebase is written in.

## Consequences

**Positive**

- NFR-002 and NFR-005 are structural rather than aspirational. `Pages/Ratings/Index` and
  `Pages/Index` contain no owner filter, no `[Authorize]`, and no authorization code —
  and that is the point, not an oversight.
- FR-003 is free. `Details.OnGetAsync(Guid id)` cannot distinguish a stranger's rating
  from a deleted one, so SC-002 and SC-003 hold without the page trying.
- Combined with the fallback authorization policy (FR-005), the two defaults compose:
  a new page is signed-in-only until someone opts out, and its data is owner-scoped
  until someone types `IgnoreQueryFilters`.
- The write guard catches the case query filters silently permit — a record saved with a
  forged `ownerUserId` — at the same chokepoint.

**Negative**

- The filter is invisible at the call site. `_db.Ratings.CountAsync()` reads as "count
  all ratings" and means "count mine". That is a genuine comprehension cost, paid by
  every future reader in exchange for NFR-005; it is why the property is documented on
  the context itself and why the DbSets carry comments.
- Expression-tree construction is the least approachable code in the project. The
  alternative — a generic `HasQueryFilter<T>` per entity type — needs a line per entity
  and therefore loses NFR-005.
- Global query filters apply to *required* navigations too, and EF warns when a required
  navigation's principal is filtered. Not hit today, because `Film` is unfiltered shared
  data and `ApplicationUser` is never navigated from an owned record. A later story that
  makes one owned record require another will meet this.
- `IgnoreQueryFilters()` is all-or-nothing per query: escaping the owner filter escapes
  every filter on that query. Fine while ownership is the only one; if soft-delete ever
  arrives, they will need naming (`IgnoreQueryFilters(["Ownership"])`).
- Ownership violations surface as exceptions, not as validation errors. Correct for a
  condition that should be unreachable through the UI, but it means a bug here is a 500
  rather than a message.

## Related

- [ADR-0001: EF Core over SQLite](0001-persistence-store.md) — global query filters are an
  EF Core feature; this decision is part of what that store now has to support (spec
  003-004 §9.1)
- [ADR-0002: ASP.NET Core Identity](0002-identity-mechanism.md) — supplies the cookie and
  the claim that `ICurrentUser` reads, and nothing else; ownership is deliberately not a
  role or a claim
- [ADR-0004: Checked-in seed data](0004-checked-in-seed-data.md) — the seeder is the one
  component that writes across owners, and therefore the only caller of either escape
- Spec 003-004 §3.1, §5.2, §9.3; NFR-002, NFR-005; FR-002, FR-003, FR-006, FR-007, FR-010
