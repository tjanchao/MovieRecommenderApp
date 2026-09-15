# Feature Specification: Accounts and Sessions

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

| Field           | Value                                                                   |
| --------------- | ----------------------------------------------------------------------- |
| Feature ID      | 001-002                                                                 |
| Status          | Draft                                                                   |
| Author          | tjanfei chao                                                            |
| Created         | 2026-09-15                                                              |
| Last updated    | 2026-09-15                                                              |
| Epic / Parent   | Story map → "Get an account" ([story-map.md](../product/story-map.md))   |
| Arc42 reference | 3, 5, 6, 8, 9, 10 — see §9.3                                            |

Covers story map entries **001** (sign up) and **002** (log in, log out, stay signed
in). The two are specified together because they share one entity, one set of
validation rules and one error-handling surface; story 002 has no meaning without 001.

## 1.1 Problem Statement

Every personalized feature in movieRecommender — taste profile, ratings, watchlist,
watch history, movie-night blending — needs to know whose data it is. Today the app has
no notion of a user at all, so there is nothing for a rating or a dismissed
recommendation to belong to. Until a visitor can create an account and be recognized on
their next visit, the recommender cannot be personal, and the demo's central claim
("picked for *you*") has no subject.

### 1.2 Goal

A visitor can create an account with an email address and a password, is signed in
immediately, and is recognized on return visits — optionally across browser restarts,
at their choice. They can sign out deliberately. Every subsequent feature can rely on a
stable, unique user identity being present on each request, and on a clear signed-in /
signed-out state to branch on.

### 1.3 Non-Goals

- Email confirmation or any outbound mail — no mail transport exists on localhost
- Password reset / "forgot password"
- Social or external identity providers (Google, GitHub, TMDB)
- Multi-factor authentication
- Roles, permissions, or an admin surface — the only distinction is signed in vs. not
- Enforcing that one user cannot read another's data — that is **story 003**
- Pre-seeded demo accounts carrying ratings and history — that is **story 004**
- Account deletion, email change, or profile management beyond what sign-up captures
- Hosting, deployment, or HTTPS certificates beyond the ASP.NET Core dev certificate

## 2. User Stories

### US-001: Create an account

**As a** new visitor,
**I want** to sign up with an email and password,
**so that** I have a profile of my own that my ratings and watchlist attach to.

_(story map 001)_

### US-002: Log in

**As a** returning user,
**I want** to log in with my email and password,
**so that** the app recognizes me and my taste profile.

_(story map 002)_

### US-003: Log out

**As a** signed-in user,
**I want** to log out deliberately,
**so that** I can hand the machine — or the browser — to someone else.

_(story map 002)_

### US-004: Stay signed in

**As a** returning user,
**I want** the option to stay signed in between visits,
**so that** I don't retype my password every time I open the app.

_(story map 002)_

### US-005: See who I am

**As a** user,
**I want** the app to show which account I'm signed in as,
**so that** I can tell at a glance — especially with several browser sessions open side
by side for movie night.

_(derived; stories 026–029 depend on it)_

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                   | Priority | User Story       |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------- |
| FR-001 | The system shall present a sign-up form capturing email, display name, password, and password confirmation.                                                     | Must     | US-001           |
| FR-002 | The system shall create an account only when email is well-formed, display name is 1–50 characters, password is ≥ 8 characters, and confirmation matches.       | Must     | US-001           |
| FR-003 | The system shall reject sign-up with a duplicate-email error when the email is already in use, leaving all other entered values intact on the redisplayed form. | Must     | US-001           |
| FR-004 | The system shall store passwords only as a salted, one-way hash, and shall never write a password to a log, error page, or response.                            | Must     | US-001, US-002   |
| FR-005 | The system shall sign the user in immediately on successful sign-up and redirect to the home page.                                                              | Must     | US-001           |
| FR-006 | The system shall present a login form capturing email, password, and a "Remember me" checkbox.                                                                  | Must     | US-002, US-004   |
| FR-007 | The system shall reject a failed login with a single message that does not reveal whether the email is registered.                                              | Must     | US-002           |
| FR-008 | The system shall treat email addresses as case-insensitive and surrounding-whitespace-insensitive for both uniqueness and login.                                | Must     | US-001, US-002   |
| FR-009 | With "Remember me" unchecked, the system shall issue a session-scoped sign-in that ends when the browser closes.                                                | Must     | US-004           |
| FR-010 | With "Remember me" checked, the system shall issue a sign-in persisting 14 days, sliding on activity.                                                           | Must     | US-004           |
| FR-011 | The system shall provide a logout action that ends the sign-in, clears its cookie, and redirects to the home page, after which requests are anonymous.          | Must     | US-003           |
| FR-012 | The system shall display the signed-in user's display name and a logout action when authenticated, and sign-up / login links when not.                          | Must     | US-005           |
| FR-013 | The system shall redirect an already-signed-in user away from the sign-up and login pages to the home page.                                                     | Should   | US-002           |
| FR-014 | After login, the system shall return the user to the page they were bounced from, honouring only same-site relative URLs; otherwise the home page.              | Should   | US-002           |
| FR-015 | The system shall temporarily refuse login for an account after 5 consecutive failed attempts, for 5 minutes.                                                    | Should   | US-002           |
| FR-016 | The system shall reject sign-up, login, and logout requests that do not carry a valid anti-forgery token.                                                       | Must     | US-001–US-003    |
| FR-017 | The system shall issue its authentication cookie as HttpOnly, Secure, and SameSite=Lax.                                                                        | Must     | US-002, US-004   |

### 3.1 Decisions Behind These Requirements

Recorded so they are not silently re-litigated during implementation.

- **Password policy is length-only (≥ 8), with no composition rules.** This follows
  current NIST guidance, and composition rules are pure friction in a live demo. Note
  that ASP.NET Core Identity's defaults (6 characters plus digit, upper, lower and
  symbol) would need overriding to satisfy FR-002.
- **FR-003 and FR-007 are deliberately asymmetric.** Sign-up says "that email is already
  registered", because a vague sign-up error is unusable; login stays vague. This does
  leak account existence through the sign-up form. That is an **accepted** trade-off at
  demo scale, not an oversight.
- **FR-015's lockout is tuned short (5 minutes, not the conventional 15)** so that a
  mistyped password on stage does not end the demo.
- **The cookie is `Secure` unconditionally (FR-017), so the app is demoed over HTTPS.**
  A `Secure` cookie sent over `http://localhost:5274` — the default launch profile — is
  stored and then never returned, so login appears to succeed and instantly forgets the
  user. The https profile (`https://localhost:7279`) becomes the default for running the
  app. See EC-17 and §9.2.

## 4. Acceptance Scenarios

### SC-001: New visitor creates an account (FR-001, FR-002, FR-005)

```gherkin
Given I am not signed in
  And no account exists for "ava@example.com"
When I submit the sign-up form with email "ava@example.com", display name "Ava",
     password "correct horse", and a matching confirmation
Then an account exists for "ava@example.com"
  And I am signed in as "Ava"
  And I am on the home page
```

### SC-002: Password too short (FR-002)

```gherkin
Given I am on the sign-up form
When I submit a password of 7 characters
Then no account is created
  And I see a validation error against the password field
  And the email and display name I entered are still shown
```

### SC-003: Confirmation does not match (FR-002)

```gherkin
Given I am on the sign-up form
When I submit a password and a confirmation that differ
Then no account is created
  And I see a validation error saying the passwords do not match
  And neither password field is redisplayed with a value
```

### SC-004: Email already registered (FR-003, FR-008)

```gherkin
Given an account exists for "ava@example.com"
When I submit the sign-up form with email "  AVA@Example.com  "
Then no second account is created
  And I see an error saying that email is already registered
  And the display name I entered is still shown
```

### SC-005: Returning user logs in (FR-006, FR-009)

```gherkin
Given an account exists for "ava@example.com" with password "correct horse"
  And I am not signed in
When I submit the login form with those credentials and "Remember me" unchecked
Then I am signed in as that account
  And I am on the home page
```

### SC-006: Session-scoped sign-in ends with the browser (FR-009)

```gherkin
Given I logged in with "Remember me" unchecked
When I close the browser and open the app again
Then I am not signed in
```

### SC-007: "Remember me" survives a browser restart (FR-010)

```gherkin
Given I logged in with "Remember me" checked
When I close the browser and open the app again
Then I am still signed in as that account
```

### SC-008: Persistent sign-in expires after inactivity (FR-010)

```gherkin
Given I logged in with "Remember me" checked
  And I have not visited the app for 15 days
When I open the app
Then I am not signed in
```

### SC-009: Wrong password (FR-007)

```gherkin
Given an account exists for "ava@example.com"
When I submit the login form with that email and the wrong password
Then I am not signed in
  And I see the message "Email or password is incorrect"
```

### SC-010: Unknown email is indistinguishable from a wrong password (FR-007)

```gherkin
Given no account exists for "nobody@example.com"
When I submit the login form with that email and any password
Then I am not signed in
  And I see the message "Email or password is incorrect"
```

### SC-011: Email matching ignores case and surrounding whitespace (FR-008)

```gherkin
Given an account exists for "ava@example.com"
When I log in as "  AVA@EXAMPLE.COM  " with the correct password
Then I am signed in as that account
```

### SC-012: Logout (FR-011)

```gherkin
Given I am signed in
When I choose to log out
Then I am on the home page
  And I am not signed in
  And returning to a page that requires an account sends me to the login form
```

### SC-013: The app shows who I am (FR-012)

```gherkin
Given I am signed in as "Ava"
When I open any page
Then I see "Ava" and a logout action
  And I do not see sign-up or login links
```

### SC-014: Signed-in user cannot reach the login form (FR-013)

```gherkin
Given I am signed in
When I navigate to the login page
Then I am redirected to the home page
```

### SC-015: Login returns me where I was going (FR-014)

```gherkin
Given I am not signed in
  And I request a page that requires an account
When I am sent to the login form and log in successfully
Then I am on the page I originally requested
```

### SC-016: Return target outside the app is ignored (FR-014)

```gherkin
Given I am not signed in
When I open the login form with a return target pointing at another site
  And I log in successfully
Then I am on the home page
  And I am not sent to the other site
```

### SC-017: Repeated failures lock the account (FR-015)

```gherkin
Given an account exists for "ava@example.com"
When I submit the wrong password 5 times in a row
Then the 6th attempt is refused even with the correct password
  And I see a message saying to try again shortly
```

### SC-018: The lock lifts on its own (FR-015)

```gherkin
Given an account is locked after 5 failed attempts
When 5 minutes pass
  And I submit the correct password
Then I am signed in
```

### SC-019: A successful login clears the failure count (FR-015)

```gherkin
Given I have failed to log in 4 times in a row
When I log in successfully
  And I later fail once
Then the account is not locked
```

### SC-020: Forged request without a token (FR-016)

```gherkin
Given I submit a sign-up, login, or logout request without a valid anti-forgery token
When the system receives it
Then the request is rejected
  And no account is created and no sign-in state changes
```

### SC-021: Passwords are not recoverable (FR-004)

```gherkin
Given an account was created with password "correct horse"
When the stored account record and the application logs are inspected
Then neither contains "correct horse" in any readable form
```

### SC-022: Cookie hardening (FR-017)

```gherkin
Given I log in successfully
When the authentication cookie is inspected
Then it is marked HttpOnly, Secure, and SameSite=Lax
  And its value does not contain my email or password
```

## 5. Domain Model

### 5.1 Entities

#### User

A person with an account. The only entity this feature owns; every later personal record
— rating, watchlist entry, watch history entry, movie-night vote — points back at it.

| Attribute             | Type      | Constraints                                            | Description                                                                 |
| --------------------- | --------- | ------------------------------------------------------ | --------------------------------------------------------------------------- |
| id                    | UUID      | PK, generated, immutable                               | Stable identity for all downstream ownership                                |
| email                 | string    | required, max 256, unique case-insensitively, valid    | The login identifier                                                        |
| displayName           | string    | required, 1–50 chars after trimming                    | Shown in the nav and in movie night                                         |
| passwordHash          | string    | required, opaque                                       | Salted one-way hash; never plaintext, compared only in constant time        |
| createdAt             | datetime  | generated, immutable, UTC                              |                                                                             |
| failedLoginAttempts   | integer   | ≥ 0, default 0                                         | Reset to 0 on successful login                                              |
| lockoutEndsAt         | datetime? | nullable, UTC                                          | Null, or a past instant, means not locked                                   |

### 5.2 Relationships

- A **User** will have many **Ratings**, **WatchlistEntries**, **WatchHistoryEntries**
  and **MovieNightVotes** (one-to-many in each case). Those entities belong to later
  stories and are out of scope here; what this spec fixes is that `User.id` is the key
  they all reference.
- **There is no Session entity.** Authentication is a signed, encrypted cookie held by
  the browser, with no server-side record. Three consequences, stated because later
  stories will bump into them:
  - Logging out in one browser does not end a sign-in in another.
  - A change to a user's display name would not appear in an existing cookie until that
    cookie is re-issued.
  - Movie night (026–029) cannot ask "who is currently online" — which is consistent
    with the map's refresh-based, no-presence-tracking decision.

### 5.3 Value Objects

#### EmailAddress

| Attribute | Type   | Constraints                                                          |
| --------- | ------ | -------------------------------------------------------------------- |
| value     | string | non-empty, ≤ 256, one `@` with a non-empty local part and domain part |

Normalized by trimming surrounding whitespace and lowercasing. Two `EmailAddress` values
are equal if and only if their normalized forms are equal. This single rule is what
FR-008, SC-004 and SC-011 all rest on.

### 5.4 Domain Rules and Invariants

- **Unique email**: no two Users may share a normalized email. Enforced at the store,
  not only in validation, so that two concurrent sign-ups still yield one account.
- **Password is write-only**: a password is accepted, hashed, and discarded. No
  operation returns, logs, or renders it, and no stored value can be reversed to it.
- **Display name is never blank**: after trimming, a whitespace-only name is a
  validation failure rather than an empty name.
- **Lockout is derived, not a stored flag**: an account is locked if and only if
  `lockoutEndsAt` is in the future. Nothing needs to actively unlock it.
- **A successful login resets the failure count**: `failedLoginAttempts` returns to 0
  and `lockoutEndsAt` to null.
- **Creation data is immutable**: `id` and `createdAt` never change after creation.
- **No ownerless personal data**: every personal record introduced by a later story must
  reference an existing `User.id`. Story 003 enforces this invariant on the read path;
  this spec establishes it on the write path.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                                                                                    |
| ------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NFR-001 | Performance  | Sign-up and login complete in < 500 ms at p95 on the demo machine, inclusive of password hashing.                                                                               |
| NFR-002 | Security     | Password hashing uses a deliberately slow, salted KDF tuned so one verification costs 50–250 ms. This is in tension with NFR-001 and wins: 500 ms is the budget, not the target. |
| NFR-003 | Security     | A failed login takes indistinguishable time whether the email is unknown or the password is wrong — an unknown email must still incur the hashing cost. Otherwise FR-007's guarantee leaks through response timing. |
| NFR-004 | Security     | The authentication cookie is signed and encrypted, with keys persisted outside the process so that restarting the app does not sign everyone out mid-demo.                       |
| NFR-005 | Reliability  | Sign-up, login and logout depend on nothing but the local account store. They must succeed with TMDB (008) and the Claude API (031) both unavailable — this is the one path in the app with no external dependency, and it stays that way. |
| NFR-006 | Scale        | Sized for demo load: tens of accounts, a handful of concurrent sessions, one process on one machine. No requirement to withstand credential-stuffing volumes.                     |
| NFR-007 | Usability    | Every validation failure is rendered server-side with the message tied to its field, independent of client-side scripting. The checked-in jQuery validation is a convenience, never the enforcement. |

Project-wide quality requirements belong in arc42 §10, which is currently an empty
template — there is nothing to reference yet. See Open Question 2.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                | Expected Behavior                                                                                       |
| ----- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| EC-1  | Sign-up or login submitted entirely empty                                | All required-field errors shown together; nothing persisted                                             |
| EC-2  | Email > 256 chars, or display name > 50                                  | Rejected with a field error stating the limit                                                           |
| EC-3  | Password longer than 128 characters                                      | Rejected rather than hashed — an unbounded password is an unbounded CPU cost                            |
| EC-4  | Password with leading or trailing spaces                                 | Preserved exactly; spaces are part of the secret and are never trimmed, unlike email and display name    |
| EC-5  | Display name containing emoji or non-Latin script                        | Accepted; length counted in characters, not bytes                                                       |
| EC-6  | Two sign-ups for the same email submitted concurrently (double-click)    | Exactly one account created; the loser sees the duplicate-email error, not a crash                      |
| EC-7  | Sign-up form resubmitted via browser back-and-refresh                    | Duplicate-email error; no second account                                                                |
| EC-8  | Valid-looking auth cookie whose user no longer exists in the store       | Treated as anonymous, cookie cleared, no error page                                                     |
| EC-9  | Auth cookie tampered with, or encrypted under a key that is gone         | Treated as anonymous, cookie cleared, no error page                                                     |
| EC-10 | Login page left open in two tabs; signing in on one, then submitting the other | Login form re-rendered with a fresh token and a plain "please try again" message — never a raw 400 |
| EC-11 | Locked-out account submits the **correct** password                      | Still refused, and the lockout window is **not** reset or extended. A lockout the correct password clears is no lockout |
| EC-12 | Concurrent failed logins racing the attempt counter                      | Undercounting is acceptable; the counter is a speed bump, not an accountant                             |
| EC-13 | Return target is a protocol-relative URL such as `//evil.example`        | Rejected as off-site. A naive "starts with `/`" check passes this — the test must be a genuine local-URL check |
| EC-14 | Logout requested when already signed out                                 | No error; redirect home                                                                                 |
| EC-15 | App restarted while a "Remember me" user is away                         | The user is still signed in on return — this is what NFR-004 buys                                       |
| EC-16 | Persistent cookie presented after its 14-day window                      | Treated as anonymous; the user sees the login form, not an error                                        |
| EC-17 | App reached over plain `http://localhost:5274`                           | The `Secure` cookie (FR-017) is never returned, so sign-in silently fails to stick. Prevented by making the https profile the default rather than by weakening the cookie |

## 8. Success Criteria

<!-- Prefixed SUC- rather than SC- to avoid colliding with the acceptance scenarios in §4. -->

| ID      | Criterion                                                                                                              |
| ------- | ---------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All 22 acceptance scenarios in §4 pass as automated tests                                                              |
| SUC-002 | A visitor can go from the home page to a signed-in account in under 30 seconds, without leaving the browser             |
| SUC-003 | A "Remember me" sign-in survives both a browser restart and an application restart                                     |
| SUC-004 | Two accounts can be signed in simultaneously in two browser profiles, each seeing its own display name — the precondition for demoing stories 026–029 |
| SUC-005 | No password appears in the account store, application logs, or any HTTP response, verified by inspection               |
| SUC-006 | Sign-up and login succeed with no network access to TMDB or the Claude API                                             |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **No upstream feature dependencies.** This is the root of the build order; every other
  story in the map depends on it, directly or through story 003.
- **A persistence store — the first in the project.** This feature introduces the
  application's data layer. Whatever is chosen here also has to serve story 007 (locally
  cached films), so the decision should be made with 007 in view rather than for accounts
  alone. Needs an ADR.
- **An identity mechanism** — ASP.NET Core Identity versus minimal custom cookie
  authentication. Needs an ADR; see Open Question 1.
- **The ASP.NET Core development HTTPS certificate**, per FR-017 and EC-17.

### 9.2 Constraints

- **Localhost only, no mail transport.** This is why email confirmation and password
  reset are non-goals rather than deferred work — neither can be demonstrated.
- **Demo-first.** Friction on the auth path is friction in the first 30 seconds of every
  demo, which is why FR-002 avoids composition rules and FR-015 uses a short lockout.
- **The app is run over HTTPS** (`https://localhost:7279`), a consequence of FR-017
  recorded in §3.1 and EC-17.
- **Target framework `net10.0`, nullable reference types enabled**, matching the existing
  project.
- **Client-side assets are checked in under `wwwroot/lib/`** and are not restored by a
  package manager; any new client dependency must be vendored the same way. This is a
  reason to prefer server-rendered validation (NFR-007).

### 9.3 Architecture References

All twelve arc42 chapters are currently empty templates. This feature is the first to
touch them, so the table below is as much a list of what to write as of what to read.

| Arc42 Section                    | Relevance to This Feature                                                                                        |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | Adds the authenticated user as an actor. Notably adds **no** external system — see NFR-005                        |
| 5. Building Block View           | Introduces the account store and the authentication boundary; the first persistence component in the system        |
| 6. Runtime View                  | Sign-up, login, logout, and per-request authentication are the first runtime scenarios worth documenting          |
| 8. Crosscutting Concepts         | Establishes the project's patterns for validation, form error rendering, and secret handling. Later specs should follow rather than reinvent these |
| 9. Architecture Decisions (ADRs) | Two ADRs needed: identity mechanism, and persistence store. `docs/architecture/adr/` does not exist yet           |
| 10. Quality Requirements         | NFR-001 through NFR-007 are candidates for promotion to project-wide quality scenarios                            |

## 10. Open Questions

| #   | Question                                                                                                        | Owner       | Status | Resolution                                                                                          |
| --- | ---------------------------------------------------------------------------------------------------------------- | ----------- | ------ | ----------------------------------------------------------------------------------------------------- |
| 1   | ASP.NET Core Identity, or minimal custom cookie authentication?                                                  | tjanfei chao | Decided | **Identity**, wired up without roles or the scaffolded UI — [ADR-0002](../architecture/adr/0002-identity-mechanism.md). The persistence half of §9.1 went the same way in [ADR-0001](../architecture/adr/0001-persistence-store.md): EF Core over SQLite. Two gaps Identity does not cover had to be closed by hand — NFR-003's timing symmetry and EC-8/EC-9's cookie clearing; both are recorded in ADR-0002 |
| 2   | Should NFR-001 – NFR-007 be promoted to arc42 §10 as project-wide quality requirements?                          | tjanfei chao | Open   | Still open. Specs 003-004 and 005-008 have since landed and both restate security NFRs of their own, so the drift this question anticipated has begun. Worth resolving before a fourth spec |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections (all currently empty templates)
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero)
  - [x] Open questions are assigned and have a resolution path
-->
