# ADR-0002: ASP.NET Core Identity as the identity mechanism

| Field    | Value                                                                     |
| -------- | ------------------------------------------------------------------------- |
| Status   | Accepted                                                                  |
| Date     | 2026-09-15                                                                |
| Deciders | tjanfei chao                                                              |
| Context  | Spec [001-002](../../specs/001-002-accounts-and-sessions.md) §9.1, §10 Q1 |

## Context

Spec 001-002 needs accounts, passwords, sign-in state and a lockout. §10 Q1 poses the
choice as ASP.NET Core Identity versus minimal custom cookie authentication, and
recommends Identity on the grounds that it supplies FR-004, FR-015 and FR-017 out of the
box.

What the spec actually demands of the mechanism:

- **FR-004 / SC-021**: passwords stored only as a salted one-way hash, never logged or
  rendered.
- **NFR-002**: a deliberately slow KDF, one verification costing 50–250 ms.
- **FR-015 / SC-017–SC-019 / EC-11**: five failures, five minutes, reset on success, and
  a locked account refused *even with the correct password* without the window being
  extended.
- **FR-009 / FR-010**: session-scoped versus 14-day sliding sign-in, chosen per login.
- **FR-017 / NFR-004**: a signed and encrypted cookie, HttpOnly, Secure, SameSite=Lax,
  with keys outside the process.
- **§1.3**: no roles, no permissions, no admin surface, no email confirmation, no
  password reset, no external providers.

The tension is that Identity brings a schema and a set of defaults built for a larger
problem than this one, and several of those defaults actively contradict the spec.

## Decision

Use **ASP.NET Core Identity**, wired up deliberately rather than via `AddIdentity` or
`AddDefaultIdentity`:

- `AddIdentityCore<ApplicationUser>()` + `AddSignInManager()` +
  `AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`, so no
  role services are registered.
- `ApplicationDbContext : IdentityUserContext<ApplicationUser, Guid>` rather than
  `IdentityDbContext`, so the role tables are absent from the schema entirely (§1.3).
- `ApplicationUser : IdentityUser<Guid>` adding only `DisplayName` and `CreatedAt`;
  §5.1's `email`, `passwordHash`, `failedLoginAttempts` and `lockoutEndsAt` map onto
  columns Identity already provides.
- No Identity UI package. The three pages are hand-written Razor Pages under
  `Pages/Account/`, because the scaffolded UI carries confirmation, 2FA and external
  login flows that §1.3 lists as non-goals.

Identity's defaults are overridden where the spec disagrees with them, each one in
`Program.cs` next to the requirement it serves:

| Default                                | Overridden to                       | Because |
| -------------------------------------- | ----------------------------------- | ------- |
| 6 chars, digit + upper + lower + symbol | length ≥ 8, no composition rules    | FR-002, §3.1 |
| 15-minute lockout                       | 5 minutes                           | FR-015, §3.1 |
| `NormalizedEmail` index non-unique      | unique                              | §5.4, EC-6 |
| Username character allow-list           | disabled                            | the username is the email |
| Security stamp revalidated every 30 min | every request                       | EC-8 |
| In-memory data-protection keys          | persisted to `App_Data/keys`        | NFR-004, EC-15 |

## Alternatives Considered

**Minimal custom cookie authentication.** A `User` entity matching §5.1 exactly, a cookie
authentication handler, and hand-rolled PBKDF2 and lockout. Genuinely attractive: the
schema would be the domain model with nothing else in it, and there would be no defaults
to fight. Rejected because the things it would have to reimplement are the things worth
getting right — a correctly parameterised KDF, constant-time comparison, hash format
versioning for future re-hashing, and a lockout that checks *before* verifying the
password so EC-11 holds. Identity has all of that already and has had it reviewed.

**Identity via `AddDefaultIdentity` plus the scaffolded UI.** The fastest route to a
working login page, and rejected for that reason: it would put confirmation, 2FA,
external-login and password-reset pages into a project whose spec names all four as
non-goals, and the resulting pages would be harder to trace back to FR numbers than the
three written by hand.

## Consequences

**Positive**

- FR-004, NFR-002, FR-015 and EC-11 are satisfied by library code rather than by ours.
  In particular `PasswordSignInAsync` checks the lockout before the password, which is
  exactly EC-11's requirement and easy to get backwards by hand.
- FR-009 and FR-010 reduce to one `isPersistent` argument.
- Schema stayed close to §5.1: eight tables became four, and roles are absent.

**Negative**

- `AspNetUserClaims`, `AspNetUserLogins` and `AspNetUserTokens` exist and will stay
  empty. The cost of not hand-rolling.
- `ApplicationUser` carries `PhoneNumber`, `TwoFactorEnabled` and `EmailConfirmed`,
  none of which this project uses. They are inert, but they are visible to anyone
  reading the model, and a later story could mistake them for supported features.
- Two gaps Identity does *not* cover had to be filled explicitly:
  - **NFR-003** — `SignInManager` skips hashing entirely for an unknown email, which
    leaks account existence through response timing. `DecoyPasswordHash` restores the
    symmetry.
  - **EC-8 / EC-9 / EC-16** — the cookie handler ignores a dead ticket but leaves the
    cookie in the browser. `StaleAuthCookieMiddleware` clears it.
- The lockout message names a state only a real account can be in, so FR-015 leaks
  existence where FR-007 does not. This is what SC-017 asks for, and is consistent with
  §3.1's accepted trade-off on the sign-up form.

## Related

- [ADR-0001: EF Core over SQLite](0001-persistence-store.md) — the store this builds on
- Spec 001-002 §3.1, §5, §9.1, §10 Q1
