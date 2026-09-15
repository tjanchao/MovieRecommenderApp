# Feature Specification: Private Data and Demo Accounts

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                                 |
| --------------- | --------------------------------------------------------------------- |
| Feature ID      | 003-004                                                               |
| Status          | Draft                                                                 |
| Author          | tjanfei chao                                                          |
| Created         | 2026-09-15                                                            |
| Last updated    | 2026-09-15                                                            |
| Epic / Parent   | Story map → "Get an account" ([story-map.md](../product/story-map.md)) |
| Arc42 reference | 3, 5, 6, 8, 9, 10 — see §9.3                                          |

Covers story map entries **003** (personal data is visible only to its owner) and **004**
(pre-seeded demo accounts with ratings and history). Builds directly on
[001-002](001-002-accounts-and-sessions.md), which created the `User` entity and the
signed-in / signed-out distinction but explicitly deferred both of these.

The two are specified together because **004 is the fixture that proves 003**. Data
isolation cannot be demonstrated, or meaningfully tested, with a single account; it needs
two accounts that already hold visibly different data. Seeding them is the cheapest way
to get that, and it happens to be the same thing the demo needs on its first click.

### 1.1 Problem Statement

001-002 established that every personal record has an owner, but only on the write path —
nothing yet stops a read from returning someone else's rating, watchlist or history. Once
movie night (026–029) puts two or three signed-in sessions side by side on one screen, a
single unscoped query is visible to the audience as the app showing the wrong person's
taste. At the same time, a freshly installed app has no ratings at all, so the
recommender — the entire point of the demo — has nothing to work from and the first click
lands on an empty page.

### 1.2 Goal

Two outcomes, one feature.

**Ownership.** A single, centrally enforced rule — *personal data is readable only by the
user who owns it* — that every later story inherits without having to remember it. Adding
the ratings page (009), the watchlist (022), history (024) or the stats page (025) must
not require writing new authorization code, and forgetting to write it must not be
possible.

**Seeded accounts.** Running the app on a fresh clone, with no API keys and no network,
produces at least two working accounts that already carry ratings, watch history, a
watchlist and stated preferences — with visibly different taste, and enough common ground
that a blended movie-night shortlist has an answer. Signing into either takes one click,
and the demo can be reset to that starting state and run again.

### 1.3 Non-Goals

- **Roles, permissions, groups, or an admin surface** — the only two categories remain
  "the owner" and "everyone else". 001-002 said this; it still holds
- **Making anything public or shareable**, beyond the one scoped exception in FR-008
- **Encryption at rest.** Anyone with filesystem access to the demo machine can read every
  account's data. See NFR-007 — this is a decision, not an oversight
- **Audit logging of who read what** — nothing in the demo consumes such a log
- **Data export or "delete my account"** — no regulatory driver for a localhost demo
- **A general test-fixture or factory framework.** Seeding here serves the demo; test data
  builders are a testing concern, not this feature's
- **Fetching seed content from TMDB at seed time** — see §3.1; this is the decision that
  keeps 004 independent of 005/006
- **Defining the Rating, WatchlistEntry, WatchHistoryEntry or TasteProfile entities in
  full.** Those belong to stories 009, 010, 022, 023 and 024. This spec fixes only their
  ownership attribute and the minimum the seed needs — see §5.1
- **Password reset or credential rotation for demo accounts** — their passwords are fixed
  and published in the repo on purpose
- **Hardening against an attacker with network access to the demo machine.** The threat
  model is a second signed-in user and a curious person editing a URL, not an adversary

## 2. User Stories

### US-001: My data is mine

**As a** user,
**I want** my taste profile, ratings, watchlist and history to be visible only to me,
**so that** what I watch and what I thought of it stays private.

_(story map 003)_

### US-002: Someone else's link shows me nothing

**As a** user,
**I want** a link or an id belonging to another account to reveal nothing at all,
**so that** privacy does not depend on people not guessing.

_(derived from 003)_

### US-003: Movie night shares only what I chose to share

**As a** user,
**I want** joining a movie night to share my votes and my name, not my rating history,
**so that** taking part in a group session is not a back door into my private data.

_(derived from 003; constrains stories 027–029 before they are built)_

### US-004: Accounts that already have taste

**As the** person demoing the app,
**I want** to sign into accounts that already carry ratings and watch history,
**so that** the recommender looks alive on the first click instead of asking the audience
to watch me rate twenty films.

_(story map 004)_

### US-005: Sign in without typing

**As the** person demoing the app,
**I want** to sign into a demo account in one click,
**so that** a mistyped password in front of an audience cannot derail the opening.

_(derived from 004)_

### US-006: Two accounts that visibly disagree

**As the** person demoing the app,
**I want** the seeded accounts to have obviously different taste, but not *entirely*
different,
**so that** side-by-side recommendations are visibly personal and a blended movie-night
shortlist still produces a winner.

_(story map 004; the second half is what stories 027–029 need)_

### US-007: A demo that works on any machine

**As the** person demoing the app,
**I want** seeding to work with no API keys and no network,
**so that** cloning the repo and running it is the whole setup.

_(derived from 004; mirrors the intent of stories 005 and 030)_

### US-008: Run the demo again

**As the** person demoing the app,
**I want** to restore the demo accounts to their starting state,
**so that** the ratings and dismissals from the last run don't leak into the next one.

_(derived from 004)_

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                                             | Priority | User Story     |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------- |
| FR-001 | The system shall require every entity holding personal data to carry a non-null owner reference to `User.id`, rejecting any attempt to persist a record without one.                     | Must     | US-001         |
| FR-002 | The system shall scope every read of personal data to the signed-in user's id, such that no query path in the application can return another user's records.                             | Must     | US-001         |
| FR-003 | The system shall respond to a request for a personal record owned by another user identically to a request for a record that does not exist — same status, same page, same message.      | Must     | US-002         |
| FR-004 | The system shall require authentication for every page and endpoint that reads or writes personal data, sending anonymous requests to the login form per 001-002 FR-014.                 | Must     | US-001         |
| FR-005 | The system shall apply authorization deny-by-default, so that a newly added page is protected unless it is explicitly marked as allowing anonymous access.                               | Must     | US-001         |
| FR-006 | The system shall determine the acting user solely from the authentication cookie, ignoring any user identifier supplied in a route, query string, form field or header.                  | Must     | US-002         |
| FR-007 | The system shall compute every derived or aggregated view — taste profile summary, stats, recommendations, "surprise me" — exclusively from the signed-in user's own records.             | Must     | US-001         |
| FR-008 | The system shall expose, to other participants in a movie-night session, only a defined shared projection: display name, which shortlisted films the user has voted on, and each vote. Ratings, watchlist, history, dismissals and taste profile shall not be exposed. | Must     | US-003         |
| FR-009 | The system shall instruct browsers not to store pages rendering personal data, so that the back button after a sign-out does not redisplay the previous user's data.                     | Should   | US-001, US-003 |
| FR-010 | The system shall log and report errors using owner identifiers only, never the content of a personal record.                                                                             | Should   | US-001         |
| FR-011 | The system shall ensure, on startup in the Development environment, that at least two demo accounts exist, each carrying ratings, watch history, watchlist entries and stated genre/era preferences. | Must     | US-004         |
| FR-012 | The system shall make seeding idempotent — repeated startups shall neither duplicate demo accounts nor duplicate their data.                                                             | Must     | US-004, US-007 |
| FR-013 | The system shall seed without any network access, TMDB API key or Claude API key.                                                                                                       | Must     | US-007         |
| FR-014 | The system shall identify every seeded film by its real TMDB id, so that a later live-catalog fetch of the same film reconciles with the seeded record rather than creating a duplicate. | Must     | US-007         |
| FR-015 | The system shall seed demo accounts with visibly different taste, such that the top three recommendations for any two demo accounts share at most one film.                              | Must     | US-006         |
| FR-016 | The system shall seed a deliberate, non-empty overlap of films that every demo account rates positively, so that a blended movie-night shortlist yields a winner.                        | Must     | US-006         |
| FR-017 | The system shall give demo accounts fixed, documented credentials whose passwords are stored exactly as any other account's, so that signing in by typing them works normally.           | Must     | US-004         |
| FR-018 | The system shall offer a one-click sign-in control on the login page for each demo account.                                                                                             | Should   | US-005         |
| FR-019 | The system shall perform no seeding and render no demo sign-in control outside the Development environment.                                                                              | Must     | US-005, US-007 |
| FR-020 | The system shall grant demo accounts no privilege of any kind — they are subject to FR-001 through FR-010 exactly as a user-created account is.                                          | Must     | US-001, US-004 |
| FR-021 | The system shall provide an action, available in Development only, that restores the demo accounts to their seeded state, discarding activity accumulated since.                         | Should   | US-008         |
| FR-022 | The system shall record seeded timestamps relative to the moment of seeding, not as fixed calendar dates, so that seeded history does not age as the repository does.                    | Should   | US-004         |

### 3.1 Decisions Behind These Requirements

Recorded so they are not silently re-litigated during implementation.

- **Seed content is checked into the repository, not fetched from TMDB (FR-013).** The
  alternative — calling TMDB at seed time — gives real, current metadata but makes 004
  depend on story 005 and on a working network, so a fresh clone with no key produces no
  demo accounts. That inverts the entire value of the story, which exists to make the app
  look alive *immediately*. The cost is a small set of film metadata committed to the repo
  that can drift from the live catalog. **FR-014 is what keeps that cost bounded**: seeded
  films carry their real TMDB ids, so they are the same records the live catalog would
  produce, and story 007's cache reconciles with them instead of duplicating them.

- **Ownership is enforced at the data-access layer, not per page (FR-002, NFR-002).** The
  per-page alternative is one `where ownerId == currentUser` per query, which works right
  up until the query someone forgets — and a forgotten filter is invisible in review
  because it looks like a query that simply doesn't need one. Enforcing it centrally makes
  the omission structurally impossible rather than merely discouraged. This needs an ADR.

- **"Not yours" and "doesn't exist" are the same response (FR-003).** A distinct 403 tells
  the requester the record exists, which for a watchlist is itself the private fact. Note
  this is the *opposite* asymmetry to 001-002 FR-003, where sign-up deliberately admits an
  email is taken — there, usability won because the user is asking about their own account.
  Here they are asking about someone else's.

- **Deny-by-default costs a little friction and is worth it (FR-005).** It means the home
  page, login, sign-up and any error page must be explicitly opened up. The failure mode of
  the opposite choice is a *missing* attribute, and nothing in a code review or a test run
  draws attention to an attribute that isn't there.

- **One-click demo sign-in is an authentication bypass, and is fenced accordingly
  (FR-018, FR-019).** It signs a user in without verifying a password, which also means it
  bypasses the 001-002 FR-015 lockout — acceptable, because lockout exists to stop password
  guessing and this path guesses nothing. It must not exist outside Development, and
  FR-017 keeps ordinary password login working so the feature never becomes the *only* way
  in.

- **Demo accounts are ordinary accounts (FR-020).** They are seeded, not special. This is
  the hinge between the two halves of this spec: the seeded accounts are simultaneously the
  demo's opening move and the test subjects that prove story 003, and they can only be the
  second if they are genuinely unprivileged.

- **The seeded tastes overlap on purpose (FR-016).** Maximally disjoint tastes make the
  side-by-side comparison in FR-015 as striking as possible — and make story 029 ("the
  first film everyone said yes to") return nothing, killing the other demo set-piece. The
  seed must satisfy both, so it needs a designed intersection rather than a random one.

- **Movie night's exception is defined now, not discovered in 027 (FR-008).** Story 027
  blends taste profiles into a shared shortlist, which is a hole in "visible only to me" by
  construction. Cutting that hole to a fixed size here means 027 is written against a
  stated boundary instead of quietly widening one.

### 3.2 The Seed Data

Concrete enough to build against; adjust the film choices freely as long as the
invariants in §5.4 hold.

| Account | Email             | Display name | Taste                                                    |
| ------- | ----------------- | ------------ | -------------------------------------------------------- |
| A       | `ava@example.com` | Ava          | Slow-burn thrillers and neo-noir, 1990s–2000s            |
| B       | `milo@example.com`| Milo         | Animation, comedy and feel-good, 2010s–2020s             |

Both use the password `demo demo demo` — a passphrase, which also exercises the
length-only, no-composition-rules policy from 001-002 FR-002.

Each account is seeded with roughly 25 ratings, 12 watch-history entries and 5 watchlist
entries, drawn from a shared pool of around 40 films. A third account is trivially added
by extending the fixture and would make movie night a better demo; two is the requirement.

## 4. Acceptance Scenarios

### SC-001: My own data renders (FR-002, FR-004)

```gherkin
Given I am signed in as Ava
  And Ava has rated 25 films
When I open my ratings
Then I see Ava's 25 ratings
  And I see no rating belonging to any other account
```

### SC-002: Another account's record is not found (FR-003)

```gherkin
Given I am signed in as Milo
  And a watchlist entry belongs to Ava
When I request that entry by its identifier
Then I get the same "not found" response I would get for an identifier that exists nowhere
  And nothing about Ava's entry is disclosed
```

### SC-003: A non-existent record is indistinguishable from someone else's (FR-003)

```gherkin
Given I am signed in as Milo
When I request a personal record using an identifier that belongs to no one
Then the response is identical to SC-002's — same status, same page, same message
```

### SC-004: Personal pages require an account (FR-004)

```gherkin
Given I am not signed in
When I request my watchlist, my history, my stats or my taste profile
Then I am sent to the login form
  And after logging in I arrive at the page I asked for
```

### SC-005: Identity comes from the cookie, never the request (FR-006)

```gherkin
Given I am signed in as Milo
When I request my ratings with a user identifier for Ava in the query string
Then I see Milo's ratings
  And Ava's data does not appear
```

### SC-006: A new personal page is protected without being told to be (FR-005)

```gherkin
Given a page reading personal data is added with no authorization attribute
When an anonymous visitor requests it
Then they are sent to the login form rather than shown the page
```

### SC-007: Aggregates do not cross accounts (FR-007)

```gherkin
Given Ava has watched 12 films and Milo has watched 12 different films
When Ava opens her stats page
Then the totals, top genres and decades reflect only Ava's 12 films
```

### SC-008: Movie night shares votes, not ratings (FR-008)

```gherkin
Given Ava and Milo are in the same movie-night session
  And Ava has privately rated a shortlisted film "meh"
When Milo views the session
Then Milo sees Ava's display name and whether Ava voted yes or no on that film
  And Milo does not see Ava's rating, watchlist, history or taste profile
```

### SC-009: The back button after sign-out reveals nothing (FR-009)

```gherkin
Given Ava signed in on a shared browser, opened her watchlist, and signed out
When the next person presses the browser's back button
Then Ava's watchlist is not redisplayed
  And they are sent to the login form
```

### SC-010: A fresh machine has demo accounts (FR-011)

```gherkin
Given a freshly cloned repository with an empty data store
When the application is started in Development
Then accounts exist for "ava@example.com" and "milo@example.com"
  And each has ratings, watch history, watchlist entries and stated preferences
```

### SC-011: Seeding twice changes nothing (FR-012)

```gherkin
Given the application has been started and seeded once
When it is started again
Then there are still exactly two demo accounts
  And neither has duplicated ratings, history or watchlist entries
```

### SC-012: Seeding needs no keys and no network (FR-013)

```gherkin
Given no TMDB API key and no Claude API key are configured
  And the machine has no network access
When the application is started in Development
Then seeding completes successfully
  And both demo accounts are fully populated
```

### SC-013: A seeded film is the same film the catalog returns (FR-014)

```gherkin
Given a film was seeded with a known TMDB identifier
  And a TMDB key is later configured
When a user searches the live catalog and opens that film
Then it resolves to the existing seeded record
  And no second record for the same film is created
```

### SC-014: The two accounts are visibly different (FR-015)

```gherkin
Given both demo accounts are seeded
When the home page recommendations are generated for each
Then the top three picks for Ava and the top three for Milo share at most one film
```

### SC-015: The two accounts still have common ground (FR-016)

```gherkin
Given both demo accounts are seeded
When a blended movie-night shortlist is built from both taste profiles
Then it contains at least one film both accounts rated positively
  And a winner can be declared
```

### SC-016: One click signs me in (FR-018)

```gherkin
Given I am not signed in and the application is running in Development
When I choose the one-click sign-in for Ava on the login form
Then I am signed in as Ava
  And I am on the home page with Ava's recommendations already populated
```

### SC-017: The demo password also works typed (FR-017)

```gherkin
Given the application is running in Development
When I type "ava@example.com" and the documented demo password into the login form
Then I am signed in as Ava
```

### SC-018: Nothing demo-related exists outside Development (FR-019)

```gherkin
Given the application is started in an environment other than Development
When the login page is rendered
Then no one-click demo sign-in control appears
  And no demo account has been created
```

### SC-019: Demo accounts are not privileged (FR-020)

```gherkin
Given I am signed in as Ava via one-click sign-in
When I request one of Milo's ratings by its identifier
Then I get the same "not found" response as in SC-002
```

### SC-020: A record cannot be saved without an owner (FR-001)

```gherkin
Given a personal record is constructed with no owner
When it is saved
Then the save is rejected
  And no ownerless record exists in the store
```

### SC-021: Reset restores the starting state (FR-021)

```gherkin
Given a demo has been run, during which Ava rated four new films and dismissed three picks
When the demo reset action is performed
Then Ava's ratings, history, watchlist and dismissals match the seeded state exactly
  And Milo's do too
```

### SC-022: Seeded history does not age (FR-022)

```gherkin
Given the seed defines a watch-history entry as "three days before seeding"
When the application is seeded for the first time a year from now
Then that entry is dated three days before that seeding
  And the history reads as recent rather than a year stale
```

## 5. Domain Model

### 5.1 Entities

This feature introduces one new entity and one shared shape. It does **not** define
`Rating`, `WatchlistEntry`, `WatchHistoryEntry` or `TasteProfile` — those belong to stories
009, 010, 022, 023 and 024. What it fixes about them is their ownership attribute, which
those stories inherit rather than choose.

#### OwnedRecord (shared shape, not a table)

Every entity holding personal data carries this. It is the single structural fact that
FR-001, FR-002 and FR-003 all rest on.

| Attribute   | Type     | Constraints                             | Description                                       |
| ----------- | -------- | --------------------------------------- | ------------------------------------------------- |
| id          | UUID     | PK, generated, immutable                |                                                   |
| ownerUserId | UUID     | required, FK → User.id, immutable       | The only account that may read or write the record |
| createdAt   | datetime | generated, immutable, UTC               |                                                   |

Applies to: `Rating`, `WatchlistEntry`, `WatchHistoryEntry`, `TasteProfile`,
`DismissedRecommendation`, `MovieNightVote`, and anything personal added later. It does
**not** apply to `Film` — a film record is shared catalogue data with no owner, which is
precisely why only the *relationships* to it are private.

#### Film (minimum viable shape — story 007 owns the full definition)

Seeding must create film records before story 007 exists, so this spec pins the fields the
seed depends on. Story 007 extends this; it must not rename or re-key it.

| Attribute  | Type     | Constraints                        | Description                                       |
| ---------- | -------- | ---------------------------------- | ------------------------------------------------- |
| tmdbId     | integer  | required, unique, immutable        | The reconciliation key — see FR-014 and §5.3      |
| title      | string   | required, max 300                  |                                                   |
| releaseYear| integer  | required, 1888 – current year + 5  |                                                   |
| runtimeMin | integer? | nullable, > 0                      | Nullable: TMDB does not always have it            |
| genres     | string[] | 0..n                               | TMDB genre names                                  |
| posterPath | string?  | nullable                           | TMDB-relative path, not a full URL — see EC-13    |
| synopsis   | string?  | nullable                           |                                                   |

#### DemoAccountDefinition (checked-in fixture, not persisted)

The declarative description of a demo account, read from the repository at seed time. It
has no database representation — it is the input to seeding, not its output.

| Attribute       | Type       | Constraints                       | Description                                            |
| --------------- | ---------- | --------------------------------- | ------------------------------------------------------ |
| email           | string     | required, valid, unique in fixture | Doubles as the idempotence key — see §5.4              |
| displayName     | string     | required, 1–50                    | Same rule as 001-002                                   |
| password        | string     | required, ≥ 8                     | Plaintext in the repo **by design**; hashed on seeding |
| tasteLabel      | string     | required                          | Human-readable, e.g. "slow-burn thrillers, 1990s"      |
| preferredGenres | string[]   | 1..n                              | Feeds story 010                                        |
| preferredEras   | string[]   | 0..n                              | e.g. "1990s"                                           |
| ratings         | seed[]     | 1..n                              | tmdbId + loved/liked/meh + relative age                |
| history         | seed[]     | 0..n                              | tmdbId + relative age                                  |
| watchlist       | seed[]     | 0..n                              | tmdbId + relative age                                  |

### 5.2 Relationships

- A **User** owns many **OwnedRecords** of each kind (one-to-many). Every OwnedRecord
  belongs to exactly one User, permanently.
- An **OwnedRecord** that concerns a film references a **Film** by `tmdbId`. The Film is
  shared and ownerless; the reference to it is what is private. Two users may hold
  ratings of the same Film without either being able to see the other's.
- A **DemoAccountDefinition** produces exactly one **User** plus its OwnedRecords the first
  time seeding runs, and nothing on subsequent runs.
- **There is still no Session entity** (001-002 §5.2). Consequences that matter here:
  one-click sign-in issues the same cookie as a password login and is indistinguishable
  from one afterwards; and a demo reset cannot invalidate the cookies of accounts already
  signed in — see EC-10.

### 5.3 Value Objects

#### TmdbFilmId

| Attribute | Type    | Constraints        |
| --------- | ------- | ------------------ |
| value     | integer | required, > 0      |

The identity of a film everywhere in the system — in seed fixtures, in the local cache
(007), and in every call to TMDB. Two Film records are the same film if and only if their
TmdbFilmIds are equal. This one rule is what makes FR-014 and SC-013 work: the app never
mints its own film identifier, so checked-in seed data and live-fetched data cannot
disagree about what is the same film.

### 5.4 Domain Rules and Invariants

- **No ownerless personal record**: persisting an OwnedRecord without an `ownerUserId`
  referencing an existing User is a failure, not a record with a null column. 001-002
  asserted this for the write path; here it becomes a store-level constraint.
- **Ownership is immutable**: `ownerUserId` is set at creation and never changes. There is
  no transfer, no re-parenting, no merge of two accounts.
- **Read scope equals ownership**: a read of an OwnedRecord not owned by the acting user
  returns nothing. Not an error, not a redacted record — nothing, indistinguishable from
  the record not existing.
- **Acting user is the cookie's user**: nothing in a request body, route or query may
  change whose data is read or written.
- **Sharing is opt-in and projected**: the only personal data crossing an account boundary
  is the movie-night projection in FR-008, and only for users who joined that session.
- **Protection is the default state**: an endpoint's protection comes from the absence of
  an opt-out, not the presence of an opt-in.
- **A film has one identity**: `tmdbId`. Seeded and live-fetched records for the same film
  are the same record (§5.3).
- **Seed idempotence is keyed on email**: a demo account exists if a User with that
  normalized email exists — using the same normalization as 001-002 §5.3. If it exists,
  seeding touches nothing about it.
- **Seeding is all-or-nothing**: every demo account and all of its data commit together or
  none of it does. A half-seeded account is worse than no account, because it looks like it
  worked. See NFR-004.
- **Demo accounts hold no privilege**: nothing in the system branches on whether a User was
  seeded. One-click sign-in is a property of the login page in Development, not a property
  of the account.
- **Seeded time is relative**: seed fixtures express ages ("3 days before seeding"), never
  calendar dates, so the demo does not rot.
- **The demo tastes intersect**: the seeded accounts must differ enough for FR-015 and
  agree enough for FR-016. Both are invariants of the fixture and both need a test — they
  pull in opposite directions and a casual edit to the seed will break one of them.

## 6. Non-Functional Requirements

| ID      | Category        | Requirement                                                                                                                                                                                 |
| ------- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | First-run startup including seeding completes in < 5 s on the demo machine. Subsequent startups, where seeding is a no-op, add < 100 ms.                                                     |
| NFR-002 | Security        | Ownership filtering is applied at the data-access layer, so that a page or query that omits an owner filter still cannot return another user's records. Per-page filtering alone is insufficient. |
| NFR-003 | Security        | Environment gating (FR-019) is evaluated server-side at request time. A hidden control, a disabled button or a client-side check does not satisfy it.                                        |
| NFR-004 | Reliability     | A seeding failure aborts startup with a message naming what failed, rather than starting an unseeded app. Discovering an empty demo at `dotnet run` is recoverable; discovering it on stage is not. |
| NFR-005 | Maintainability | Adding a new kind of personal record requires no new authorization code. If a future story has to remember to filter by owner, this feature has failed regardless of whether its tests pass.  |
| NFR-006 | Scale           | Sized for the demo: 2–5 accounts, roughly 25 ratings and 12 history entries each, a shared pool of ~40 films. No pagination or query-plan concerns at this size.                             |
| NFR-007 | Privacy         | Privacy is enforced between *application users*, not against the machine. Data is unencrypted at rest and readable by anyone with filesystem access to the demo machine. Accepted: the store holds seeded fiction and whatever a demo audience watched being typed. |
| NFR-008 | Reliability     | Ownership enforcement and seeding depend on nothing but the local store. Both must work with TMDB (008) and the Claude API (031) unavailable, preserving the property established in 001-002 NFR-005. |

Per the resolution of 001-002 Open Question 2, NFR-002, NFR-005 and NFR-008 are candidates
for promotion to arc42 §10 as project-wide quality requirements — see §10 below.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                   | Expected Behavior                                                                                                                            |
| ----- | ------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | URL edited to another account's record id                                                   | Not found, identical to EC-2 in status, body and timing                                                                                      |
| EC-2  | URL containing an id that exists nowhere                                                    | Not found — the baseline EC-1 must be indistinguishable from                                                                                 |
| EC-3  | `userId` supplied in a query string, hidden form field or header                            | Ignored entirely; the cookie's user acts. Not an error — silently ignored, so a probe learns nothing                                          |
| EC-4  | Signed-in user's account no longer exists in the store (e.g. after a data reset)            | Treated as anonymous and the cookie cleared, per 001-002 EC-8. No personal query runs with a dangling owner id                                |
| EC-5  | Two demo accounts signed in via two tabs of the **same** browser profile                    | The second sign-in replaces the first — one cookie jar, one identity. Movie night must be demoed in separate browser profiles or private windows. This is the single most likely way the movie-night demo fails; document it next to the demo script |
| EC-6  | Back button after sign-out on a shared browser                                              | No personal page redisplayed; login form instead (FR-009)                                                                                     |
| EC-7  | Application restarted repeatedly during development                                         | Seeding is a no-op after the first run; no duplicates, no measurable startup cost                                                             |
| EC-8  | A real user signs up with a demo account's email before seeding runs                        | Seeding leaves that account untouched — the email key says it exists. Log a warning: the demo account is not the one the fixture describes    |
| EC-9  | An earlier seeding run crashed partway                                                      | Cannot occur — seeding is transactional (§5.4). If the store somehow holds a partial account, the email key makes seeding skip it; the reset action (FR-021) is the recovery path |
| EC-10 | Demo reset performed while a demo account is signed in elsewhere                            | The signed-in session survives (no Session entity to invalidate) and immediately sees seeded state. Any page open at the time shows stale data until reloaded |
| EC-11 | A seeded film is later fetched live from TMDB with changed metadata                         | The record is updated in place, matched on `tmdbId`. One film, one record — never two                                                        |
| EC-12 | TMDB or Claude key present but the service is unreachable at startup                        | Irrelevant to seeding, which never calls either (FR-013). Startup must not probe them                                                        |
| EC-13 | Poster image unreachable because the machine is offline                                     | Layout holds with a placeholder; no broken image, no error, no blocked render. `posterPath` is stored as a path so the base URL can be swapped or stubbed |
| EC-14 | Movie-night shortlist contains a film a participant rated privately                         | The film appears and their vote appears; their rating does not (FR-008)                                                                      |
| EC-15 | Demo password typed wrong five times on stage                                               | The account locks per 001-002 FR-015 — it is an ordinary account. One-click sign-in still works, as it verifies no password, and clears the failure count |
| EC-16 | A new personal page is added and the developer forgets to scope its query                   | Returns only the acting user's records anyway (NFR-002). A route-enumeration test asserting every non-anonymous page requires authentication catches the FR-005 half |
| EC-17 | Anonymous visitor reaches the one-click sign-in control in a non-Development environment    | Cannot occur — the control is not rendered and the endpoint is not registered (NFR-003). A forged request to the endpoint is a plain not-found |
| EC-18 | Seed fixture edited to sharpen the taste contrast, removing the last shared film            | The FR-016 test fails. This is the intended tripwire — without it, movie night silently loses its winner and the failure surfaces in story 029 |
| EC-19 | Seeded history dates read as years old because the repo has aged                            | Cannot occur — ages are relative to seed time (FR-022, SC-022)                                                                               |
| EC-20 | Rating seeded for a `tmdbId` absent from the seed film pool                                 | Seeding aborts with a message naming the orphaned id, rather than creating a rating pointing at nothing                                       |

## 8. Success Criteria

<!-- Prefixed SUC- rather than SC- to avoid colliding with the acceptance scenarios in §4. -->

| ID      | Criterion                                                                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 22 acceptance scenarios in §4 pass as automated tests                                                                                                         |
| SUC-002 | From a fresh clone with no API keys and no network: `dotnet run`, one click, and a populated personalized page — in under 60 seconds                              |
| SUC-003 | The two demo accounts, opened side by side in separate browser profiles, show visibly different recommendations — at most one film shared in the top three         |
| SUC-004 | A test enumerating every route in the application confirms that each one either allows anonymous access deliberately or requires authentication                    |
| SUC-005 | An attempt to read another account's record, by any route the application exposes, returns a response indistinguishable from a record that does not exist         |
| SUC-006 | Adding a new personal entity requires zero new authorization code — demonstrated when story 022 (watchlist) lands                                                  |
| SUC-007 | The demo can be run, reset, and run again producing identical results, without restarting the application                                                          |
| SUC-008 | A blended shortlist across both demo accounts yields at least one film both rated positively — the precondition for story 029                                      |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec [001-002](001-002-accounts-and-sessions.md)** — hard dependency. `User.id`, the
  authentication cookie, the signed-in/signed-out distinction, and the email normalization
  rule this feature's seed idempotence relies on.
- **The persistence store ADR owed by 001-002 §9.1.** That decision now carries a further
  requirement it did not have when it was written: it must support enforcing an owner
  filter centrally (NFR-002) rather than query by query. Worth settling before the store is
  chosen, not after.
- **Story 007 (local film cache)** — an ordering risk rather than a blocker. Seeding needs
  film records before 007 exists, so this spec pins a minimal `Film` shape (§5.1) keyed on
  `tmdbId`. Story 007 must extend that shape rather than replace or re-key it; if 007 lands
  first, this spec adopts its definition instead.
- **Stories 009, 010, 022, 023, 024** define the personal entities this feature protects.
  003 lands first and constrains them — they inherit `ownerUserId` and the read-scoping
  rule rather than each deciding for themselves.
- **Stories 027–029 (movie night)** are the only consumers of the FR-008 exception. Until
  they are built, FR-008 and SC-008 constrain a surface that does not yet exist.
- **Story 013 (ranked recommendations)** is needed for FR-015 and SC-014 to be observable
  as written. Until 013 exists, "visibly different taste" can only be asserted at the
  profile level — see Open Question 1.
- **Nothing external.** No TMDB, no Claude API, no network (FR-013, NFR-008).

### 9.2 Constraints

- **Environment-gated behaviour** (FR-019) requires the application to distinguish
  Development from other environments and to do so server-side (NFR-003).
- **Seed film metadata is committed to the repository**, which places a small amount of
  TMDB-derived data under version control. TMDB's terms require attribution for use of
  their data; the seed fixture should carry an attribution note.
- **Demo passwords are published in the repository** (FR-017). Harmless because the
  accounts exist only in Development on a localhost demo, and load-bearing because the
  point is that anyone cloning the repo can sign in. It does mean FR-019 must hold — these
  credentials must never seed anywhere else.
- **Localhost only, demo-first** — as 001-002 §9.2. Reaffirmed here for NFR-007: privacy
  between users is real; privacy against the machine's owner is not attempted.
- **Target framework `net10.0`, nullable reference types enabled.**
- **Client-side assets are checked in under `wwwroot/lib/`** — the one-click sign-in
  control (FR-018) must be a plain server-rendered form post, not a new client dependency.

### 9.3 Architecture References

All twelve arc42 chapters remain empty templates. As with 001-002, this table is as much a
list of what to write as of what to read.

| Arc42 Section                    | Relevance to This Feature                                                                                                                                             |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | Adds no external system. Worth recording *explicitly* that seeding has no external dependency — it is the property FR-013 exists to protect                             |
| 5. Building Block View           | Adds two blocks: the ownership chokepoint inside the data-access component, and a startup seeding component that reads a checked-in fixture                              |
| 6. Runtime View                  | Three scenarios worth documenting: an authorized personal read; first-run seeding; demo reset                                                                           |
| 8. Crosscutting Concepts         | **The main home for this feature.** Authorization and data ownership belong here as a project-wide concept, alongside environment gating and cache-control on personal pages. Every later story should reference this rather than restate it |
| 9. Architecture Decisions (ADRs) | Both written: [ADR-0003](../architecture/adr/0003-ownership-enforcement.md) (ownership enforced in the data-access layer, not per query — NFR-002) and [ADR-0004](../architecture/adr/0004-checked-in-seed-data.md) (checked-in seed data rather than a TMDB fetch — §3.1). Both extend [ADR-0001](../architecture/adr/0001-persistence-store.md), the store ADR owed by 001-002 |
| 10. Quality Requirements         | NFR-002, NFR-005 and NFR-008 are project-wide, not feature-local — see §10 question 2                                                                                   |
| 12. Glossary                     | First entries worth writing: *owner*, *personal data*, *demo account*, *seed*, *shared projection*, *TMDB id*                                                           |

## 10. Open Questions

| #   | Question                                                                                                                                | Owner        | Status  | Resolution                                                                                                                                                                                                                            |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------- | ------------ | ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | FR-015 measures "visibly different taste" via top-three recommendations, but story 013 supplies those. How is it verified before 013?     | tjanfei chao | Open    | **Weaker property adopted, and enforced.** `DemoSeedFixtureLoader` checks on every load that the two accounts' preferred genres and eras are pairwise disjoint and that their positively-rated films overlap only in the designed FR-016 intersection; a violation aborts startup. This shaped the fixture — cross-taste ratings are all `Meh` so the positive intersection is exactly the four overlap films. Still open in the sense that the FR-015 wording cannot be checked until 013 exists |
| 2   | Should NFR-001 – NFR-007 from 001-002 be promoted to arc42 §10? _(carried over from 001-002 Open Question 2, which deferred it to "when the second spec lands" — this is that spec)_ | tjanfei chao | Decided | **Yes.** 001-002 NFR-002/003/004 (credential handling) and this spec's NFR-002/005/008 (ownership, inheritance, no-external-dependency) are project-wide. Write arc42 §10 before story 009, or every subsequent spec re-derives them and they drift |
| 3   | Does the minimal `Film` shape in §5.1 belong to this spec or should 004 wait for story 007?                                              | tjanfei chao | Decided | **Define it here.** Waiting inverts 004's value — a seeded demo that needs the film cache first needs TMDB first, which is exactly the dependency FR-013 removes. 007 extends this shape; the `tmdbId` key is the contract between them   |
| 4   | SUC-001 and SUC-004 ask for automated tests, but the project still has no test project. How are the §4 scenarios verified? _(raised during implementation)_ | tjanfei chao | Open    | **Deferred by decision — tests were explicitly out of scope for this implementation.** The invariants that would otherwise be test assertions run as startup tripwires instead: fixture validation covers EC-18, EC-20 and Open Question 1, and startup aborts naming the failure (NFR-004). SC-001–SC-022 remain manually verified, and SUC-001/SUC-004 are unmet. Worth closing before story 009 — a route-enumeration check (SUC-004) is the one this feature cannot approximate at startup |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/004, US-002 SC-002/003/005,
        US-003 SC-008, US-004 SC-010/017, US-005 SC-016/018, US-006 SC-014/015,
        US-007 SC-011/012/013, US-008 SC-021)
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements — with the deliberate
        exception of Rating/Watchlist/History/TasteProfile, owned by stories 009–024 and
        constrained here only via the OwnedRecord shape
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections (all currently empty templates)
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero)
  - [x] Open questions are assigned and have a resolution path (2 open, 2 decided)
-->
