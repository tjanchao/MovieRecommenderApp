# Feature Specification: The Live Movie Catalog (TMDB)

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

| Field           | Value                                                                                        |
| --------------- | -------------------------------------------------------------------------------------------- |
| Feature ID      | 005-006-007-008                                                                              |
| Status          | Draft                                                                                        |
| Author          | tjanfei chao                                                                                 |
| Created         | 2026-09-15                                                                                   |
| Last updated    | 2026-09-15                                                                                   |
| Epic / Parent   | Story map → "Connect the live movie catalog (TMDB)" ([story-map.md](../product/story-map.md)) |
| Arc42 reference | 3, 5, 6, 7, 8, 9, 10, 11, 12 — see §9.3                                                      |

Covers story map entries **005** (API key outside source control), **006** (search the live
catalog by title), **007** (films saved locally), and **008** (clear message when the
catalog is unavailable or rate-limited). Builds on
[001-002](001-002-accounts-and-sessions.md) for identity and
[003-004](003-004-private-data-and-demo-accounts.md) for the ownership rule and the minimal
`Film` shape this spec now completes.

The four are specified together because they are one decision surface, not four. 005 is the
credential for the connection, 006 is the only thing anyone can *do* with it before story
009, 007 is what happens to whatever 006 returns, and 008 is what happens when 006 returns
nothing. Specifying 006 without 008 produces a demo that dies on a rate limit; specifying
007 separately produces two competing definitions of `Film`. Splitting them would mean
writing the same `Film` entity, the same error taxonomy and the same "is the catalog up?"
question four times.

This is the **first external system in the project**. 001-002 NFR-005 and 003-004 NFR-008
both asserted that the app works with no network; this feature is where that stops being
free and becomes something FR-024 through FR-032 have to actively protect.

### 1.1 Problem Statement

movieRecommender has no films. 003-004 seeded roughly forty of them as checked-in fixture
data precisely so the demo would not depend on a network, but forty films is a demo prop,
not a catalog — a user cannot search for the film they actually watched last night, and the
recommender has nothing outside the fixture to recommend. Reaching a real catalog means
introducing an API key that must not be committed, a network call on the path of the
demo's first click, and a third party that can rate-limit the app in front of an audience.

### 1.2 Goal

Three outcomes, one feature.

**A configured connection.** A TMDB credential is supplied per machine, from outside source
control, in one documented command. When it is missing or wrong, the app says so precisely
and keeps running on local data — a fresh clone with no key still starts, still seeds, and
still signs in.

**A live, searchable catalog that becomes local.** A signed-in user types a title and sees
matching films with poster and year, drawn live from TMDB. Any film they then act on —
open, rate, watchlist, mark watched — is written into the local store keyed by its TMDB id,
so it renders instantly from then on, survives a restart, and is still there with the
network off.

**Failure that reads as a message, not a crash.** Every way TMDB can fail — unconfigured,
unauthorized, rate-limited, timed out, offline, malformed — produces a specific,
plain-language message on the affected surface only, and leaves every page built from local
data completely unaffected.

### 1.3 Non-Goals

- **Caching poster or backdrop images locally.** Images load from TMDB's CDN; offline means
  placeholders, not broken layout. See §3.1 and EC-16 — this is a decision, not an omission
- **A full offline mode.** Local data works offline; *discovering new films* does not, and
  is not meant to
- **Browsing the catalog** — trending, popular, now-playing, discover-by-genre. Story 009
  needs a popular-films list and will add exactly that one endpoint; until then, title
  search is the only way in
- **The film detail page** — that is story 019. This spec persists everything that page will
  need and defines the route key, but renders no detail page
- **"More like this" (020) and trailers (021)** — separate TMDB endpoints, separate stories.
  §3.2 marks them as out of scope on purpose
- **LLM vibe tagging (015)** — the `Film` record reserves room for it (§5.1) and nothing more
- **The Claude API key** — story 030, deliberately mirroring 005 rather than sharing a spec
- **Search across anything but film titles** — no people, no TV, no collections, no keyword
  or year filters. Runtime filtering is story 016 and operates on recommendations, not search
- **Fuzzy matching, spell correction or ranking of our own.** TMDB's result order is the
  result order
- **A background sync, warm-up job, or scheduled catalog refresh.** Every outbound call is
  caused by something a user just did
- **Multi-language or multi-region catalogs.** One configured language, one region, fixed
- **Rotating, provisioning or managing TMDB credentials**, or supporting more than one
- **Hosting, deployment, or a production configuration story** — localhost only, as the map says

## 2. User Stories

### US-001: Configure the catalog on any machine

**As the** person running the app,
**I want** to supply a TMDB API key from outside source control, in one documented command,
**so that** the catalog works on my machine without the key working its way into a commit.

_(story map 005)_

### US-002: Know instantly that the key is the problem

**As the** person running the app,
**I want** a missing or rejected key to say exactly that, and to say how to fix it,
**so that** I am not debugging an empty search box thirty seconds before a demo.

_(derived from 005 and 008)_

### US-003: A clone with no key still runs

**As the** person running the app,
**I want** the app to start and work on local data with no key configured,
**so that** the guarantee 003-004 made — clone, run, one click, a populated page — survives
the arrival of an external dependency.

_(derived from 005; protects 003-004 FR-013)_

### US-004: Find a film by title

**As a** user,
**I want** to search the live catalog by title,
**so that** I can find the film I actually have in mind rather than only the ones the app
already knows about.

_(story map 006)_

### US-005: Recognize the film at a glance

**As a** user,
**I want** each result to show a poster and a release year,
**so that** I can tell the 1960 *Psycho* from the 1998 one without opening either.

_(story map 006)_

### US-006: A search that finds nothing says so

**As a** user,
**I want** a search with no matches to tell me there are none,
**so that** I can tell "no such film" apart from "something broke".

_(derived from 006)_

### US-007: Films I've touched load instantly

**As a** user,
**I want** the films I have rated, watchlisted or watched to load from this machine,
**so that** my own pages are fast and do not depend on the catalog being reachable.

_(story map 007)_

### US-008: My lists survive the catalog

**As a** user,
**I want** my ratings, watchlist and history to keep showing real titles and posters even
when the catalog is down,
**so that** a third party's outage cannot empty my own data.

_(story map 007)_

### US-009: Local copies don't go stale forever

**As a** user,
**I want** a locally saved film to pick up corrected details from the catalog eventually,
**so that** a placeholder title or a missing runtime from the day I saved it doesn't stay
wrong permanently.

_(derived from 007)_

### US-010: Told what happened, in words

**As a** user,
**I want** an unavailable or rate-limited catalog to give me one clear sentence,
**so that** I know whether to wait, retry, or give up — instead of reading a stack trace.

_(story map 008)_

### US-011: Only the broken part is broken

**As a** user,
**I want** an outage to affect only the parts of the app that need the catalog,
**so that** my watchlist, history and stats keep working normally.

_(story map 008)_

### US-012: The demo survives a rate limit

**As the** person demoing the app,
**I want** a rate limit mid-demo to degrade to local data with a calm banner,
**so that** the presentation continues instead of ending on an error page.

_(derived from 008; the map names this as the obvious way the demo falls over)_

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                                                              | Priority | User Story             |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------------- |
| FR-001 | The system shall read the TMDB credential from configuration key `Tmdb:ApiReadAccessToken`, resolved from .NET user-secrets in Development and from the `Tmdb__ApiReadAccessToken` environment variable otherwise. | Must     | US-001                 |
| FR-002 | The system shall not read the credential from any file tracked by version control, and shall ship no file containing a real credential.                                                                   | Must     | US-001                 |
| FR-003 | The system shall ship a `Tmdb` configuration section in `appsettings.json` containing only non-secret settings — base URL, image base URL, language, region, timeouts — plus an empty credential placeholder, so the expected shape is discoverable. | Must     | US-001                 |
| FR-004 | The system shall start successfully, seed, and serve every page that does not require the catalog when no credential is configured.                                                                       | Must     | US-003                 |
| FR-005 | The system shall verify the credential once at startup, without blocking startup, and record the result as the catalog's configuration status.                                                            | Should   | US-002                 |
| FR-006 | The system shall present, in Development, a diagnostics view stating whether the catalog is configured, reachable and currently degraded, and the time and classification of the last failure.             | Should   | US-002, US-012         |
| FR-007 | The system shall redact the credential from every log entry, diagnostics view, error page, rendered page and persisted record, showing at most its last four characters.                                  | Must     | US-001                 |
| FR-008 | The system shall transmit the credential in a request header only, never in a URL, query string, form body or cookie.                                                                                     | Must     | US-001                 |
| FR-009 | The system shall document the setup as a single copyable command in the repository README, together with where to obtain a TMDB credential.                                                               | Must     | US-001                 |
| FR-010 | The system shall provide a title-search page reachable from the main navigation, accepting the query as a bookmarkable URL parameter.                                                                     | Must     | US-004                 |
| FR-011 | The system shall require an authenticated user for the search page, per the deny-by-default rule in 003-004 FR-005.                                                                                       | Must     | US-004                 |
| FR-012 | The system shall trim the query and require at least 2 non-whitespace characters, prompting rather than calling the catalog when the query is shorter.                                                    | Must     | US-004                 |
| FR-013 | The system shall display for each result: poster thumbnail, title, release year, and a truncated synopsis, preserving the order the catalog returned.                                                     | Must     | US-004, US-005         |
| FR-014 | The system shall render a fixed-size placeholder in place of a missing or unreachable poster, without changing the layout of the result list.                                                             | Must     | US-005                 |
| FR-015 | The system shall display "Year unknown" for a film with no release date, and shall still list it.                                                                                                        | Must     | US-005                 |
| FR-016 | The system shall page results at 20 per page with next/previous navigation, reporting the total number of matches.                                                                                        | Should   | US-004                 |
| FR-017 | The system shall exclude adult titles from search results.                                                                                                                                               | Must     | US-004                 |
| FR-018 | The system shall show an explicit "no films match that title" state, visually distinct from every catalog error state, when a successful search returns zero results.                                     | Must     | US-006                 |
| FR-019 | The system shall link each result to that film's detail route, keyed by TMDB id, and shall not persist any film merely because it appeared in search results.                                             | Must     | US-004                 |
| FR-020 | The system shall persist a local `Film` record, keyed by TMDB id, the first time a user acts on that film — opening it, rating it, adding it to a watchlist, marking it watched, or dismissing it.        | Must     | US-007                 |
| FR-021 | The system shall upsert local film records on TMDB id, never creating a second record for the same film, whether that record originated from seeding, search or a recommendation.                         | Must     | US-007                 |
| FR-022 | The system shall serve film details from the local store when present, calling the catalog only when the record is absent or stale.                                                                       | Must     | US-007                 |
| FR-023 | The system shall record, per film, the completeness of its local copy (summary or full) and when it was last fetched, upgrading summary to full on first full fetch and never downgrading.                | Must     | US-007, US-009         |
| FR-024 | The system shall treat a local film record as stale 14 days after its last fetch, refreshing it on next use when the catalog is available and serving it unchanged when it is not.                        | Should   | US-009                 |
| FR-025 | The system shall render every page built solely from local data — watchlist, history, ratings, stats, taste profile, seeded demo accounts — with no outbound catalog call and no dependence on catalog availability. | Must     | US-008, US-011         |
| FR-026 | The system shall refuse to create a personal record referencing a film that has no local `Film` record, persisting the film first.                                                                        | Must     | US-007, US-008         |
| FR-027 | The system shall classify every catalog failure into exactly one of the kinds in §3.3, and shall present the message defined there for that kind.                                                         | Must     | US-010                 |
| FR-028 | The system shall never expose a raw catalog response body, HTTP status code alone, exception type or stack trace to the user.                                                                             | Must     | US-010                 |
| FR-029 | The system shall retry only idempotent catalog reads, at most twice, with exponential backoff and jitter, and only for the failure kinds §3.3 marks retryable.                                            | Should   | US-010, US-012         |
| FR-030 | The system shall honour a `Retry-After` value from a rate-limited response, and shall not re-issue the request before it elapses.                                                                         | Must     | US-012                 |
| FR-031 | The system shall stop calling the catalog for 30 seconds after 5 consecutive failures, serving local data and the degraded state during that window without further outbound calls.                       | Should   | US-012                 |
| FR-032 | The system shall display a non-blocking degraded-catalog banner on catalog-dependent pages while degraded, and shall not display it on pages that need no catalog.                                        | Should   | US-011, US-012         |
| FR-033 | The system shall limit itself to 4 concurrent outbound catalog requests per process.                                                                                                                      | Should   | US-012                 |
| FR-034 | The system shall abandon any single catalog request after 5 seconds and any user-facing operation's catalog work after 10 seconds, falling back to local data.                                            | Must     | US-010, US-012         |
| FR-035 | The system shall maintain a local genre id-to-name map refreshed from the catalog when available, falling back to a checked-in copy, so that films saved from search carry genre names rather than bare ids. | Should   | US-007                 |
| FR-036 | The system shall display TMDB attribution wherever catalog data is shown, as TMDB's terms require.                                                                                                        | Must     | US-004                 |
| FR-037 | The system shall log every outbound catalog call with endpoint, outcome classification, duration and a correlation id, and without the credential or the user's query being treated as personal data.     | Should   | US-002, US-010         |

### 3.1 Decisions Behind These Requirements

Recorded so they are not silently re-litigated during implementation.

- **A missing key is fail-soft, not fail-fast (FR-004).** The instinct with a required
  external credential is to validate it at startup and refuse to run without it. That would
  directly break 003-004 SC-012, which requires a fresh clone with no keys and no network to
  start and seed successfully — the property that makes the demo portable. So the app starts
  regardless, and "not configured" becomes a first-class state of the catalog rather than a
  startup crash. FR-005 and FR-006 are what keep this from turning into a silent failure:
  the check still runs, its answer is just reported instead of thrown.

- **The credential is a v4 API Read Access Token sent as a bearer header, not a v3 `api_key`
  query parameter (FR-008).** TMDB accepts both. The v3 form puts the secret in the URL,
  which means it lands in HTTP client logs, in any exception message carrying the request
  URI, and in the `Tmdb:BaseUrl` a developer pastes into a browser to test something. A
  header keeps FR-007 achievable instead of aspirational. The story map says "API key"; this
  is that key, in the form that can be kept out of logs.

- **Search results are not persisted; interaction is what persists (FR-019, FR-020).** The
  alternative — writing every search result into the local store — would warm the cache for
  free, and would also mean one search for "the" writing twenty films nobody asked about into
  a store that stories 013 and 025 read as "films this user has some relationship with".
  Story 007 says *"the films I interact with"*, and drawing the line at interaction keeps the
  store meaningful rather than merely large.

- **Poster images are not cached locally (§1.3, EC-16).** Caching them would make the app
  genuinely offline-complete, at the cost of binary blobs in the store or a file cache, an
  eviction policy, and a second staleness question. 003-004 EC-13 already settled the
  fallback behaviour — store the path, render a placeholder when the image cannot load — and
  that is enough: offline, a watchlist shows real titles and years with placeholder art,
  which is legible. The cost of the decision is that a fully-offline demo looks grey.

- **Search requires sign-in (FR-011).** Search reads no personal data, so it could be
  anonymous. But 003-004 FR-005 makes protection the default state, and every action a search
  result leads to — rate, watchlist, mark watched — needs an account anyway. Opening a hole
  to save an anonymous visitor one login, for a page whose every button then demands one, is
  a bad trade. The search query itself is consequently associated with a signed-in user, which
  FR-037 addresses by not logging it as personal data.

- **Freshness is 14 days, checked on use, and never blocking (FR-024).** Film metadata is
  nearly static — a title and release year from 2019 will not change. The things that do
  change (vote averages, newly added runtimes, corrected synopses) do not matter enough to
  spend a network round trip on every page view. Checking on use rather than on a schedule
  keeps the no-background-work rule in §1.3 intact.

- **"Not configured", "rate-limited" and "offline" are three different messages, not one
  (§3.3).** Collapsing them into "the catalog is unavailable" is what makes US-002 a
  thirty-second debugging session. The person who forgot to set a key and the person whose
  wifi dropped need opposite next actions, and the app already knows which is which.

- **Degradation is per-surface, not global (FR-025, FR-032).** A catalog outage must not put
  a banner on the watchlist, because the watchlist is not degraded — it is reading local data
  and is exactly as correct as it was. A banner on a page that is working teaches people to
  ignore banners.

- **The circuit breaker is short and forgiving (FR-031).** Thirty seconds, five failures.
  Long enough to stop hammering a rate-limited API, short enough that a presenter who pauses
  to explain something finds the catalog working again when they resume. A conventional
  60-second break would outlast the average recovery moment on stage.

- **`Film` is completed here, not re-specified.** 003-004 §5.1 pinned a minimal shape keyed
  on `tmdbId` and stated that this story extends it without renaming or re-keying. §5.1 below
  honours that exactly: every field it defined survives verbatim, and the additions are
  provenance and freshness metadata.

### 3.2 Catalog Endpoints in Scope

The system's entire outbound surface. Anything not listed is out of scope for this spec and
belongs to the story named.

| Purpose                     | Endpoint                | In scope | Notes                                                                    |
| --------------------------- | ----------------------- | -------- | ------------------------------------------------------------------------ |
| Credential verification     | `/authentication`       | Yes      | FR-005. Cheapest authenticated call; used for nothing else               |
| Title search                | `/search/movie`         | Yes      | FR-010–FR-018. `include_adult=false`, one page of 20 per request         |
| Film details                | `/movie/{id}`           | Yes      | FR-020, FR-023. Produces a *full* record                                 |
| Genre map                   | `/genre/movie/list`     | Yes      | FR-035. Fetched at most once per application run                         |
| Image base URL              | `/configuration`        | Should   | Fetched once per run; falls back to the configured default (EC-15)       |
| Popular films               | `/movie/popular`        | No       | Story 009 — the starter rating set                                       |
| Similar films               | `/movie/{id}/similar`   | No       | Story 020                                                                |
| Videos / trailers           | `/movie/{id}/videos`    | No       | Story 021                                                                |
| Credits / cast              | `/movie/{id}/credits`   | No       | Story 019                                                                |

### 3.3 Catalog Failure Taxonomy

FR-027 refers to this table. Every outbound call resolves to exactly one row. The messages
are the intent, not final copy.

| Kind             | Cause                                     | Retryable | User-facing message                                                                              |
| ---------------- | ----------------------------------------- | --------- | ------------------------------------------------------------------------------------------------ |
| `Unconfigured`   | No credential supplied                    | No        | "The movie catalog isn't set up on this machine yet." In Development, plus the setup command      |
| `Unauthorized`   | 401 / 403 — credential wrong or revoked   | No        | "The movie catalog rejected this app's credentials. The API key needs checking."                  |
| `RateLimited`    | 429                                       | Yes¹      | "The movie catalog is busy right now — try again in a moment." Everything saved still works       |
| `Timeout`        | No response within the FR-034 budget      | Yes       | "The movie catalog is taking too long to respond."                                                |
| `Unreachable`    | DNS, connection refused, no network       | Yes       | "Can't reach the movie catalog — check this machine's internet connection."                       |
| `ServerError`    | 5xx                                       | Yes       | "The movie catalog is having problems. This isn't something on your side."                        |
| `NotFound`       | 404 for a specific film                   | No        | "That film isn't in the catalog any more." Handled per-film, never as a page-level failure        |
| `Malformed`      | 2xx with a body that will not parse       | No        | Same message as `ServerError`; logged distinctly, because it means the contract moved             |
| `CircuitOpen`    | Suppressed by FR-031                      | n/a       | Same message as the failure that opened the circuit — the user sees no new concept                |

¹ Retryable only when `Retry-After` is under 5 seconds; otherwise the call fails fast and
the window is respected per FR-030.

## 4. Acceptance Scenarios

### SC-001: The key is supplied from outside the repository (FR-001, FR-002)

```gherkin
Given a fresh clone with the documented setup command run once
When the application starts and a search is performed
Then results are returned from the live catalog
  And no file tracked by version control contains the credential
```

### SC-002: No key, and the app still runs (FR-004)

```gherkin
Given no TMDB credential is configured
  And the machine has no network access
When the application is started in Development
Then it starts successfully and seeds the demo accounts
  And one-click sign-in works
  And the watchlist, history and stats pages render
```

### SC-003: No key says so, and says how to fix it (FR-005, FR-006, §3.3 Unconfigured)

```gherkin
Given no TMDB credential is configured
When I open the search page
Then I see that the movie catalog is not set up on this machine
  And in Development I see the command that configures it
  And I do not see an error page or an empty result list
```

### SC-004: A rejected key is not confused with an outage (§3.3 Unauthorized)

```gherkin
Given a TMDB credential is configured but is not valid
When I search for a title
Then I am told the catalog rejected this app's credentials
  And I am not told to check my internet connection
```

### SC-005: The key never appears anywhere (FR-007, FR-008)

```gherkin
Given a valid TMDB credential is configured
When searches, film opens and a deliberately failing call have all been performed
Then the credential appears in no log entry, no rendered page, no error output and no stored record
  And it appears in no request URL — only in a request header
```

### SC-006: Search by title (FR-010, FR-013)

```gherkin
Given I am signed in and the catalog is available
When I search for "blade runner"
Then I see matching films in the order the catalog returned them
  And each shows a poster, a title and a release year
```

### SC-007: Two films with the same title are distinguishable (FR-013)

```gherkin
Given I am signed in and the catalog is available
When I search for "psycho"
Then the 1960 film and the 1998 film both appear
  And their release years distinguish them
```

### SC-008: Search is bookmarkable (FR-010)

```gherkin
Given I have searched for "arrival"
When I copy the page's address, open it in a new tab and reload it
Then I see the same search results for "arrival"
```

### SC-009: Search requires an account (FR-011)

```gherkin
Given I am not signed in
When I request the search page
Then I am sent to the login form
  And after logging in I arrive at the search page
```

### SC-010: A one-character query calls nothing (FR-012)

```gherkin
Given I am signed in
When I submit a search of a single character
Then I am asked for at least two characters
  And no request is made to the catalog
```

### SC-011: No matches is not an error (FR-018)

```gherkin
Given the catalog is available
When I search for a title that matches no film
Then I am told no films match that title
  And the page does not display any catalog error state
```

### SC-012: A missing poster does not break the list (FR-014)

```gherkin
Given a search result has no poster image in the catalog
When the results render
Then that result shows a placeholder of the same size as the other posters
  And the list alignment is unchanged
```

### SC-013: A film with no release date still appears (FR-015)

```gherkin
Given a matching film has no release date in the catalog
When the results render
Then the film is listed with "Year unknown" in place of its year
```

### SC-014: Searching saves nothing (FR-019)

```gherkin
Given the local store holds a known set of films
When I search for a title and view two pages of results without acting on any of them
Then the local store holds exactly the same set of films as before
```

### SC-015: Acting on a film saves it (FR-020)

```gherkin
Given a film appears in my search results and is not in the local store
When I add it to my watchlist
Then a local film record exists for it with its title, year and poster path
  And my watchlist shows it
```

### SC-016: A saved film renders without the catalog (FR-022, FR-025)

```gherkin
Given a film is saved locally from an earlier session
  And the catalog is unreachable
When I open my watchlist
Then the film's title, year and details render from the local copy
  And no request is made to the catalog
```

### SC-017: A seeded film and a searched film are one record (FR-021)

```gherkin
Given a film was seeded by 003-004 with a known TMDB id
When I find the same film through search and add it to my watchlist
Then the existing record is updated in place
  And no second record for that TMDB id exists
```

### SC-018: A summary record is upgraded, never downgraded (FR-023)

```gherkin
Given a film was saved from a search result and holds only summary details
When I open that film and the catalog returns its full details
Then the local record gains runtime, genres and full synopsis
  And a later save from a summary source does not remove them
```

### SC-019: A stale record refreshes without blocking (FR-024)

```gherkin
Given a local film record was last fetched 20 days ago
  And the catalog is available
When I open that film
Then its details render immediately from the local copy
  And the record is refreshed from the catalog
```

### SC-020: A stale record serves fine when the catalog is down (FR-024)

```gherkin
Given a local film record was last fetched 20 days ago
  And the catalog is unreachable
When I open that film
Then its details render from the local copy
  And I see no error on the parts of the page fed by local data
```

### SC-021: A rate limit degrades rather than fails (FR-030, FR-032, §3.3 RateLimited)

```gherkin
Given the catalog responds with a rate-limit status
When I search for a title
Then I am told the catalog is busy and to try again in a moment
  And I see a degraded-catalog banner
  And my watchlist, history and stats remain fully usable
```

### SC-022: A rate-limit window is respected (FR-030)

```gherkin
Given the catalog has rate-limited a request and asked to be retried after 10 seconds
When I search again immediately
Then no request is made to the catalog before that window elapses
  And I am shown the same "busy right now" state
```

### SC-023: Repeated failure stops the calls (FR-031)

```gherkin
Given 5 consecutive catalog calls have failed
When I perform a further catalog-dependent action within the next 30 seconds
Then no outbound call is made
  And I see the degraded state rather than waiting for a timeout
```

### SC-024: The circuit closes on its own (FR-031)

```gherkin
Given the catalog stopped being called after repeated failures
  And the catalog has since recovered
When 30 seconds have passed and I search again
Then the search succeeds
  And the degraded banner no longer appears
```

### SC-025: A slow catalog does not hang the page (FR-034)

```gherkin
Given the catalog accepts connections but never responds
When I search for a title
Then within 10 seconds I am told the catalog is taking too long
  And the page is fully rendered and usable
```

### SC-026: Errors stay in plain language (FR-028)

```gherkin
Given the catalog returns a server error
When I search for a title
Then I see one sentence explaining that the catalog is having problems
  And I see no status code alone, no response body, no exception type and no stack trace
```

### SC-027: Only the affected surface is degraded (FR-025, FR-032)

```gherkin
Given the catalog is unreachable
When I open my watchlist, my history and my stats
Then all three render normally
  And none of them shows a degraded-catalog banner
```

### SC-028: A film deleted from the catalog is handled per-film (§3.3 NotFound)

```gherkin
Given a film in my watchlist no longer exists in the catalog
When I open my watchlist
Then the film still appears from its local copy
  And opening it tells me it is no longer in the catalog
  And the rest of the watchlist is unaffected
```

### SC-029: Genres are names, not numbers (FR-035)

```gherkin
Given a film is saved from a search result, whose catalog payload carries genre ids only
When the film is displayed
Then its genres read as names such as "Thriller"
  And they match the names on the same film's full record
```

### SC-030: Attribution is present (FR-036)

```gherkin
Given catalog data is displayed on any page
When that page renders
Then TMDB attribution is visible on it
```

## 5. Domain Model

### 5.1 Entities

#### Film

Shared, ownerless catalogue data — the local copy of a TMDB film. **Extends** the minimal
shape pinned in 003-004 §5.1 without renaming or re-keying it, as that spec required. The
first seven attributes are unchanged from there; the rest are provenance and freshness.

| Attribute       | Type       | Constraints                            | Description                                                                    |
| --------------- | ---------- | -------------------------------------- | ------------------------------------------------------------------------------ |
| tmdbId          | integer    | PK, required, unique, immutable        | The only identity a film has, anywhere in the system (§5.3)                     |
| title           | string     | required, max 300                      |                                                                                |
| releaseYear     | integer?   | nullable, 1888 – current year + 5      | **Nullable — widened from 003-004.** TMDB ships films with no release date (FR-015) |
| runtimeMin      | integer?   | nullable, > 0                          | Absent on summary records; TMDB does not always have it                        |
| genres          | string[]   | 0..n                                   | Names, never ids — resolved via the genre map (FR-035)                          |
| posterPath      | string?    | nullable                               | TMDB-relative path, never a full URL (003-004 EC-13)                            |
| synopsis        | string?    | nullable                               |                                                                                |
| originalTitle   | string?    | nullable, max 300                      | Distinguishes translated releases                                              |
| backdropPath    | string?    | nullable                               | Relative path, as posterPath                                                   |
| originalLanguage| string?    | nullable, ISO 639-1                    |                                                                                |
| tmdbVoteAverage | decimal?   | nullable, 0–10                         | Catalog rating, not any user's — never conflate with `Rating`                   |
| tmdbVoteCount   | integer?   | nullable, ≥ 0                          | Makes vote average interpretable; a 10.0 from 3 voters is noise                 |
| detailLevel     | enum       | [summary, full], required              | See §5.4 — monotonic (FR-023)                                                  |
| source          | enum       | [seed, search, detail, recommendation] | How this record first appeared; diagnostics only, never behaviour               |
| firstSeenAt     | datetime   | generated, immutable, UTC              |                                                                                |
| fetchedAt       | datetime?  | nullable, UTC                          | Last successful catalog fetch. Null for seeded records never refreshed (EC-19)  |
| lastFetchOutcome| enum?      | nullable, §3.3 kinds                   | Why the last refresh attempt did not update this record                         |

Reserved for later stories and deliberately not defined here: `vibeTags` (015), `cast` (019),
`similarFilmIds` (020), `trailerKey` (021). They extend this entity the way this spec extended
003-004's — additively, on the same key.

#### GenreMap

The catalog's genre id-to-name mapping. One row per genre, refreshed at most once per
application run (FR-035), with a checked-in fallback so a search with no network still
produces names.

| Attribute | Type     | Constraints              | Description                          |
| --------- | -------- | ------------------------ | ------------------------------------ |
| tmdbId    | integer  | PK, required             | TMDB genre id                        |
| name      | string   | required, max 100        |                                      |
| fetchedAt | datetime | required, UTC            |                                      |

#### CatalogStatus (process state, not persisted)

What FR-005, FR-006, FR-031 and FR-032 all read. Held in memory; a restart legitimately
resets it, because a restart is also the most common way a developer fixes it.

| Attribute            | Type      | Constraints                                 | Description                                              |
| -------------------- | --------- | ------------------------------------------- | -------------------------------------------------------- |
| configuration        | enum      | [unconfigured, unverified, valid, rejected] | Result of FR-005, or its absence                         |
| consecutiveFailures  | integer   | ≥ 0                                         | Reset to 0 by any success                                |
| suppressCallsUntil   | datetime? | nullable, UTC                               | The FR-031 window; null means calls are permitted        |
| retryNotBefore       | datetime? | nullable, UTC                               | The FR-030 `Retry-After` window                          |
| lastFailure          | value obj | nullable                                    | A `CatalogFailure` (§5.3)                                |

### 5.2 Relationships

- A **Film** is referenced by many personal records — `Rating`, `WatchlistEntry`,
  `WatchHistoryEntry`, `DismissedRecommendation` — always by `tmdbId`. The Film is shared and
  ownerless; the *reference* to it is the private fact (003-004 §5.2). Nothing about this
  feature changes that.
- A **Film** carries 0..n **genre names**, resolved through the **GenreMap** at the moment of
  saving. The map is a lookup, not a relationship the Film holds a foreign key into — so a
  later genre rename does not orphan a film.
- **No personal record may exist without its Film** (FR-026). This is the read-path
  counterpart to 003-004 EC-20, which aborted seeding on a rating pointing at a film outside
  the seed pool. The same invariant, now enforced at runtime.
- **CatalogStatus relates to nothing.** It is process state deliberately kept out of the
  store, so that ownership enforcement (003-004 NFR-002) has no reason to reach it and a
  corrupted status cannot outlive a restart.
- **A search result is not an entity.** It is a `FilmSearchResult` (§5.3) that exists for the
  duration of one request and is discarded (FR-019).

### 5.3 Value Objects

#### TmdbFilmId

Defined in 003-004 §5.3 and unchanged. Restated because this spec is where it stops being an
internal fixture key and becomes the identity shared with an external system: the app never
mints its own film identifier, so a seeded film, a searched film and a recommended film are
the same record by construction.

#### FilmSearchResult

One row of a search response. Transient by design — the type that exists so the answer to
"is this in the store?" is always no.

| Attribute    | Type     | Constraints                   |
| ------------ | -------- | ----------------------------- |
| tmdbId       | integer  | required, > 0                 |
| title        | string   | required                      |
| releaseYear  | integer? | nullable                      |
| posterPath   | string?  | nullable                      |
| synopsis     | string?  | nullable                      |
| genreIds     | integer[]| 0..n, resolved via GenreMap   |

#### SearchQuery

| Attribute | Type   | Constraints                                  |
| --------- | ------ | -------------------------------------------- |
| text      | string | trimmed, 2–200 characters after trimming     |
| page      | integer| ≥ 1, ≤ 500 (the catalog's own page ceiling)  |

Two SearchQuery values are equal if their trimmed text and page are equal. The trim happens
once, at construction; nothing downstream re-trims or re-validates.

#### CatalogFailure

| Attribute   | Type      | Constraints                    |
| ----------- | --------- | ------------------------------ |
| kind        | enum      | required, one of §3.3          |
| occurredAt  | datetime  | required, UTC                  |
| retryAfter  | duration? | nullable, from the response    |
| correlationId | string  | required                       |

Carries no response body, no URL and no credential — which is what makes FR-007 and FR-028
hold by construction rather than by remembering to redact at each render site.

#### ImageReference

| Attribute | Type   | Constraints                                       |
| --------- | ------ | ------------------------------------------------- |
| path      | string | required, catalog-relative, begins with `/`       |
| size      | enum   | [thumbnail, card, detail]                         |

Resolved to a URL at render time against the configured image base. Storing the path rather
than the URL is what lets the base change — or be stubbed in tests — without rewriting stored
records, and is the same rule 003-004 EC-13 set for seeded posters.

### 5.4 Domain Rules and Invariants

- **A film has one identity: its TMDB id.** Restated from 003-004 §5.4 because this is the
  spec where it is tested against reality. Seeded, searched and recommended records for the
  same film are one record. The application mints no film identifiers of its own.
- **Film is ownerless.** No `ownerUserId`, no read scoping. It is the one entity in the system
  deliberately outside 003-004's ownership rule, because two users seeing the same film's
  title is not a privacy event — seeing each other's *relationship* to it is.
- **Detail level is monotonic**: `summary → full` is the only permitted transition. A save
  from a summary source may add fields and may not null out fields the full record supplied.
  Without this, one search after opening a film silently strips its runtime.
- **No orphan personal record**: persisting a `Rating`, `WatchlistEntry`, `WatchHistoryEntry`
  or `DismissedRecommendation` requires the referenced `Film` to exist locally. The film is
  written first, in the same transaction.
- **Stale is usable**: staleness schedules a refresh; it never suppresses a render, invalidates
  a record, or turns into a failure state. There is no such thing as a film too old to show.
- **A failed refresh leaves the record alone**: the previous values stand and
  `lastFetchOutcome` records why. A refresh never partially overwrites a good record with a
  bad response.
- **Genres are stored as names**: a film never persists a bare genre id, so an unavailable
  genre map at save time means the film is saved with the names it can resolve and the rest
  filled on its next refresh — not with numbers leaking into the UI.
- **The credential is header-only and write-only**: it is read from configuration, placed in a
  request header, and never rendered, logged, stored or returned. Mirrors the password rule in
  001-002 §5.4, for the same reason.
- **Every outbound call has exactly one classification**: no call ends in an unclassified
  state, and no two classifications apply at once. This is what makes FR-027 testable rather
  than aspirational.
- **Local data never depends on the catalog**: any page whose data is entirely local performs
  zero outbound calls and cannot enter a degraded state. 001-002 NFR-005 and 003-004 NFR-008
  asserted this before an external system existed; here it becomes a rule with something to
  test against.
- **The catalog is never on the write path of personal data**: adding a watchlist entry may
  need the film fetched first, but a catalog failure after that fetch cannot leave a personal
  write half-done. Fetch, then persist both together, or neither.
- **Search is read-only**: a search changes nothing in the store. Not the film set, not a
  history of queries, not a "recently searched" list.

## 6. Non-Functional Requirements

| ID      | Category        | Requirement                                                                                                                                                                                   |
| ------- | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance     | A title search completes in < 1.5 s at p95 on the demo machine, inclusive of the catalog round trip and image placeholders but excluding poster download.                                       |
| NFR-002 | Performance     | Any page served entirely from local data completes in < 200 ms at p95 and issues zero outbound requests — asserted by counting calls, not by timing.                                            |
| NFR-003 | Performance     | A degraded catalog adds no more than 100 ms to a page that needs it, once the FR-031 window is open. Degradation must be cheaper than failure, or the breaker is pointless.                     |
| NFR-004 | Security        | The credential appears in no log, URL, rendered page, error output or persisted record (FR-007, FR-008). Verified by a test that searches all of them for the configured value.                 |
| NFR-005 | Security        | The credential is absent from every version-controlled file. Committing one is a defect, and the repository needs a `.gitignore` before this feature merges — see §9.2 and Open Question 1.     |
| NFR-006 | Security        | A user-supplied search term is transmitted to the catalog encoded, and is never interpolated into a URL, a log message or rendered output without encoding.                                     |
| NFR-007 | Reliability     | No user-facing operation blocks on the catalog for more than 10 s (FR-034), under any failure mode including a connection that accepts and never responds.                                      |
| NFR-008 | Reliability     | No catalog failure produces an unhandled exception, a 500 page, or a partially rendered page. Every §3.3 kind has a rendered state.                                                             |
| NFR-009 | Reliability     | At most 4 concurrent outbound catalog requests per process (FR-033), so that one user's paging cannot rate-limit the demo.                                                                      |
| NFR-010 | Usability       | Every catalog error state is one sentence, in plain language, naming what happened and what the person can do. A status code, an exception name or "an error occurred" fails this requirement.  |
| NFR-011 | Observability   | Every outbound call logs endpoint, classification, duration and correlation id. A failure a developer cannot classify from the logs alone fails this requirement.                               |
| NFR-012 | Compliance      | TMDB attribution is displayed wherever catalog data appears (FR-036), and the app does not present itself as endorsed or certified by TMDB.                                                     |
| NFR-013 | Scale           | Sized for the demo: hundreds of local film records, a handful of concurrent users, one process. No pagination strategy, index tuning or cache eviction is required at this size.                |
| NFR-014 | Maintainability | Adding a catalog endpoint (009, 019, 020, 021) requires no new error handling, retry or degradation code — it inherits §3.3 and FR-027–FR-034. If a later story writes its own, this spec failed. |

Per 003-004 Open Question 2 (Decided), NFR-002, NFR-007 and NFR-008 are project-wide rather
than feature-local and belong in arc42 §10 alongside the ones promoted there — see §10.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                          | Expected Behavior                                                                                                                                                             |
| ----- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | Credential configured as an empty string or whitespace                                            | Treated as `Unconfigured`, not as a rejected key. An empty setting is a setup mistake, and pointing at the wrong one wastes the debugging minute FR-006 exists to save          |
| EC-2  | Credential is a valid v3 `api_key` rather than a v4 read access token                              | `Unauthorized`, with a message naming the expected credential type. This is the single most likely setup error given TMDB issues both                                          |
| EC-3  | Credential is revoked between startup verification and a later search                             | The search fails `Unauthorized` and `CatalogStatus.configuration` moves to `rejected`. Startup's verdict is not cached as permanent truth                                      |
| EC-4  | Credential set in `appsettings.Development.json` instead of user-secrets                          | It works — configuration precedence allows it — which is exactly the risk. A repository `.gitignore` and a README warning are the mitigation; see NFR-005                      |
| EC-5  | Search query of 200+ characters, or composed entirely of punctuation                              | Truncated to the SearchQuery bound and passed through. A weird query returning nothing is EC-11, not an error                                                                  |
| EC-6  | Search query containing `&`, `#`, `/` or non-Latin script                                         | Encoded correctly and returns matching films. Non-Latin titles are a core catalog case, not an edge one                                                                        |
| EC-7  | Query producing more than 500 pages of results                                                    | Paging stops at the catalog's page ceiling with no next link, rather than requesting a page the catalog rejects                                                                |
| EC-8  | User pages beyond the last page by editing the URL                                                | Clamped to the last available page; no error                                                                                                                                  |
| EC-9  | User types quickly, firing several searches before the first returns                              | Only the latest result renders. Superseded requests are abandoned and do not count toward the FR-031 failure tally                                                             |
| EC-10 | Catalog returns 200 with an empty result set                                                      | The FR-018 "no films match" state — never a spinner, a blank page, or an error                                                                                                |
| EC-11 | Catalog returns a result whose title is empty or null                                             | Rendered as "Untitled", still listed and still openable. A bad row is not a bad page                                                                                          |
| EC-12 | Catalog returns 200 with a body that will not parse                                               | `Malformed` → the ServerError message, logged distinctly. A parse failure means the contract moved and a developer needs to see it, while the user does not                    |
| EC-13 | Rate-limit response carries no `Retry-After`                                                      | A 5-second default window applies. Absent guidance is not permission to retry immediately                                                                                     |
| EC-14 | Catalog becomes reachable mid-page — some calls on a page succeed, some fail                      | Each surface reports its own state. A page does not roll up to "all broken" because one call of three failed                                                                  |
| EC-15 | `/configuration` is unavailable, so the image base URL is unknown                                 | The configured default base applies. Posters must never fail because the *configuration* call failed                                                                          |
| EC-16 | Machine is offline and poster images cannot load                                                  | Placeholders of identical dimensions; layout holds; no broken-image icon, no console error cascade. This is the accepted cost of the §1.3 no-image-caching decision            |
| EC-17 | Film exists locally but has been deleted from the catalog                                         | Local copy continues to render everywhere. Only an explicit fetch reports `NotFound`, and only on that film. Never delete a local film because the catalog forgot it           |
| EC-18 | Film's TMDB metadata changed since it was saved — retitled, re-dated                              | Updated in place on refresh, matched on `tmdbId` (003-004 EC-11). One film, one record                                                                                        |
| EC-19 | A seeded film has never been fetched, so `fetchedAt` is null                                       | Treated as stale and refreshed on first use when the catalog is available; served unchanged when it is not. A null fetch time is not an error state                           |
| EC-20 | Two requests save the same previously-unknown film concurrently                                   | Exactly one record results; the loser updates rather than inserting. The `tmdbId` uniqueness constraint is the enforcement, not an in-memory check                             |
| EC-21 | Refresh returns a *less* complete record than the one stored — TMDB dropped a field               | Existing values stand. Monotonic detail level (§5.4) means a refresh may enrich and may not strip                                                                              |
| EC-22 | User adds a film to the watchlist and the catalog fails mid-fetch                                 | Neither the film nor the watchlist entry is persisted, and the user is told the catalog is unavailable. Never a watchlist entry pointing at nothing (FR-026)                   |
| EC-23 | Catalog recovers during the FR-031 suppression window                                             | Calls stay suppressed until the window elapses, then resume. A 30-second false negative is cheaper than probing a service that just failed 5 times                             |
| EC-24 | Application restarts while the circuit is open                                                    | The circuit is closed again — `CatalogStatus` is process state (§5.1). Acceptable, and occasionally the fastest fix a presenter has                                            |
| EC-25 | Every catalog call fails throughout a whole demo                                                  | The demo still runs entirely on seeded data: sign-in, recommendations, watchlist, history and stats. Search is the only surface that reports itself unusable                   |
| EC-26 | A genre id appears in a search result that the genre map does not contain                         | That genre is omitted from the saved film rather than stored as a number, and is filled on the film's next refresh                                                             |
| EC-27 | System clock moves backward, making `fetchedAt` appear to be in the future                        | Treated as fresh. Never refresh in a loop over a clock anomaly                                                                                                                |

## 8. Success Criteria

<!-- Prefixed SUC- rather than SC- to avoid colliding with the acceptance scenarios in §4. -->

| ID      | Criterion                                                                                                                                                                  |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 30 acceptance scenarios in §4 pass as automated tests, with the catalog stubbed rather than called                                                                        |
| SUC-002 | From a fresh clone: one documented command supplies the credential, and the next `dotnet run` returns live search results — no other setup step                              |
| SUC-003 | 003-004 SUC-002 still holds unchanged — clone, run, one click, a populated personalized page, with no keys and no network, in under 60 seconds                               |
| SUC-004 | A search for a title the app has never seen returns live results with posters and years, and acting on one result makes that film render from local data on the next request  |
| SUC-005 | With the network disconnected mid-session, every page except search continues to render correctly, and search reports a specific, actionable message                          |
| SUC-006 | Each of the nine failure kinds in §3.3 can be induced in a test and produces its own distinct rendered state — no two collapse into the same message                         |
| SUC-007 | A search of the configured credential's value across all logs, rendered pages, error output and the data store, after an exercised session, returns zero hits                 |
| SUC-008 | A test asserts that watchlist, history, stats and taste-profile pages issue exactly zero outbound catalog requests                                                            |
| SUC-009 | No version-controlled file contains a credential, verified by a check that runs on the repository rather than by inspection                                                   |
| SUC-010 | Story 009 (starter rating set) adds its endpoint with no new error-handling, retry or degradation code — the NFR-014 test of this spec's design                              |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec [001-002](001-002-accounts-and-sessions.md)** — hard dependency. FR-011's
  authentication requirement, and the return-to-page behaviour SC-009 relies on.
- **Spec [003-004](003-004-private-data-and-demo-accounts.md)** — hard dependency, in both
  directions. It pinned the minimal `Film` shape and the `tmdbId` identity rule this spec
  completes (§5.1, §5.3), and its FR-013 no-network guarantee is the constraint that forces
  FR-004's fail-soft behaviour. SUC-003 exists to prove this spec did not break it.
- **The persistence store ADR owed by 001-002 §9.1 and extended by 003-004 §9.1.** It now
  carries a third requirement: a shared, ownerless `Film` table alongside owner-scoped
  personal tables, with the ownership filter of 003-004 NFR-002 *not* applied to it. A
  chokepoint that filters everything indiscriminately would make every film invisible.
- **An HTTP resilience approach** — timeouts, retry with backoff, and the FR-031 breaker.
  Needs an ADR; `Microsoft.Extensions.Http.Resilience` is the obvious candidate and would be
  the project's first NuGet dependency.
- **A `UserSecretsId` in the project file**, without which FR-001's Development path does not
  exist.
- **A TMDB account and API read access token**, obtained by whoever runs the app. Free, but
  it is a signup step and belongs in the README per FR-009.
- **TMDB itself** — the first external system in the project. arc42 §3 gains an external
  actor; §11 gains a risk that did not previously exist.

Downstream, and constrained by this spec rather than blocking it: **009** (starter rating
set) adds one endpoint under §3.2's rules; **013** reads local films only; **015** adds
`vibeTags` to `Film`; **019**, **020**, **021** add endpoints and fields on the same key;
**030–031** mirror 005 and 008 for the Claude API and should reuse §3.3's shape rather than
invent a second taxonomy.

### 9.2 Constraints

- **The repository has no `.gitignore` and tracks `obj/`** (CLAUDE.md). This feature
  introduces the project's first secret, which makes that a live risk rather than housekeeping
  — see NFR-005 and Open Question 1.
- **TMDB's terms of use** require attribution (FR-036, NFR-012) and prohibit presenting the
  app as endorsed or certified by TMDB. 003-004 §9.2 already noted this for the seeded
  fixture; it now applies to every rendered page.
- **TMDB publishes no firm rate limit**, so the app must treat a 429 as the authoritative
  signal (FR-030) rather than rely on a documented number, and must bound its own concurrency
  (FR-033) rather than assume a ceiling.
- **Localhost only, demo-first.** No proxy, no egress restrictions, no production
  configuration story. The only network conditions that need handling are "works", "slow",
  "rate-limited" and "absent".
- **Client-side assets are checked in under `wwwroot/lib/`.** Search must be a plain
  server-rendered GET form (FR-010); type-ahead would need a new client dependency and is
  outside scope for that reason as much as any other.
- **The app is run over HTTPS** (`https://localhost:7279`), per 001-002 §9.2. Poster images
  load from TMDB over HTTPS, so no mixed-content issue arises — which it would have on the
  http profile.
- **Target framework `net10.0`, nullable reference types enabled.** The nullable-heavy `Film`
  shape in §5.1 is deliberate: the catalog genuinely omits these fields, and pretending
  otherwise moves the failure from the type system to the page.

### 9.3 Architecture References

All twelve arc42 chapters remain empty templates. As with the previous two specs, this table
is as much a list of what to write as of what to read — but this feature is the one that makes
§3 and §11 non-trivial for the first time.

| Arc42 Section                    | Relevance to This Feature                                                                                                                                                              |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | **The first external system.** TMDB enters the context diagram as an outbound dependency, along with its image CDN. The two previous specs explicitly added none; this chapter is now worth drawing |
| 4. Solution Strategy             | Records the local-first stance: the catalog enriches a locally-owned data set rather than being read through on every request. Every later catalog story inherits it                     |
| 5. Building Block View           | Adds a catalog client (credential, endpoints, classification), a resilience layer (timeout, retry, breaker), the film store, and the degradation surface. The ownership chokepoint from 003-004 must explicitly *not* cover `Film` |
| 6. Runtime View                  | Four scenarios worth documenting: a search; a read-through film fetch with local upsert; a degraded read served locally; the breaker opening and closing                                |
| 7. Deployment View               | First chapter with anything to say — outbound HTTPS to two TMDB hosts, and a per-machine secret that is not part of the deployable                                                      |
| 8. Crosscutting Concepts         | **The main home for this feature.** External-service access, failure classification, retry and degradation, and secret handling are project-wide patterns. 030–031 must reference this chapter rather than re-derive it, or the project ends up with two error taxonomies |
| 9. Architecture Decisions (ADRs) | Three ADRs needed: (a) the resilience approach per §9.1; (b) local-first caching with interaction-triggered writes and a 14-day staleness window, per §3.1; (c) credential storage and transport — user-secrets plus bearer header. `docs/architecture/adr/` still does not exist |
| 10. Quality Requirements         | NFR-002, NFR-007 and NFR-008 are project-wide (see §10 question 2 of 003-004, Decided). This chapter is now overdue — three specs have deferred to it                                   |
| 11. Risks & Technical Debt       | First real entries: third-party availability and rate limiting during a live demo; a secret in a repository with no `.gitignore`; checked-in seed metadata drifting from the live catalog |
| 12. Glossary                     | Adds: *catalog*, *TMDB id*, *local film record*, *summary vs. full*, *stale*, *degraded*, *read access token*                                                                            |

## 10. Open Questions

| #   | Question                                                                                                                      | Owner        | Status  | Resolution                                                                                                                                                                                                                                                  |
| --- | ------------------------------------------------------------------------------------------------------------------------------- | ------------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Is adding a `.gitignore` and untracking `obj/` in scope of this story, or separate housekeeping?                              | tjanfei chao | Open    | Proposal: **in scope**, minimally. This story introduces the project's first secret, and NFR-005 and SUC-009 are unsatisfiable in a repository with no ignore file. Untracking `obj/` can stay separate; a `.gitignore` covering `secrets`, `*.user` and build output cannot |
| 2   | Should search results be persisted eagerly to warm the local film store?                                                       | tjanfei chao | Decided | **No** — see §3.1. Story 007 says "films I interact with", and the store is read by 013 and 025 as a set of films the user has a relationship with. Eager persistence would make one search for a common word pollute it permanently                          |
| 3   | Does the 14-day staleness window (FR-024) hold once story 013 ranks from local data?                                           | tjanfei chao | Open    | Revisit when 013 lands. If ranking uses `tmdbVoteAverage`, a 14-day-old vote average is fine; if it uses popularity, which moves weekly, the window needs shortening for that field alone — which would mean per-field freshness, and is worth avoiding       |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/005, US-002 SC-003/004,
        US-003 SC-002, US-004 SC-006/008/009/010/014, US-005 SC-007/012/013,
        US-006 SC-011, US-007 SC-015/016/017/018, US-008 SC-016/020/028,
        US-009 SC-019/020, US-010 SC-025/026, US-011 SC-027, US-012 SC-021/022/023/024)
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements — Film is completed
        here per 003-004's contract; Rating/Watchlist/History remain owned by 009–024
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths (27, of which 20 are failures)
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections (all currently empty templates)
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero)
  - [x] Open questions are assigned and have a resolution path (2 open, 1 decided)
-->
