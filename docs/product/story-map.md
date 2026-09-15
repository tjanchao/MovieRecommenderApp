# User Story Map: movieRecommender

## Goal

Help someone decide what to watch tonight: learn their taste from a few quick
reactions and what they've already seen, then recommend films from a live movie
catalog with a plain-language explanation of *why* each pick fits them.

## Primary User

A casual movie watcher with an account of their own. They want a short list of picks
they'll trust, a reason to trust each one, and somewhere to keep the films they're
not in the mood for tonight. Their ratings and watch history are private to them and
quietly make the next recommendation better.

This is built as a **demo**, so the map is ordered for visible impact: the
explanation behind each pick and the group movie-night session are the moments meant
to land in front of an audience, not optional extras.

The demo runs on **localhost** — no hosting, no deployment story, no HTTPS
certificates beyond the dev one. That keeps every story on this map buildable on one
machine, with one consequence worth naming up front: movie night (026–029) is shown
as several browser sessions side by side on the presenter's screen rather than
friends joining from their phones. The join link still needs to work; it just
resolves locally.

## Story Map

### Get an account

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 001 | As a new visitor, I sign up with an email and password so I have a profile of my own                |
| 002 | As a returning user, I log in and log out, and stay signed in between visits                        |
| 003 | As a user, my taste profile, ratings and watchlist are visible only to me                           |
| 004 | As the person demoing the app, I can log into pre-seeded accounts that already have ratings and history — at least two, with visibly different taste — so the recommender looks alive from the first click and movie night has someone to blend with |

### Connect the live movie catalog (TMDB)

The catalog is **TMDB**. Stories 020 ("more like this") and 021 (trailer) depend on
its similar-films and video endpoints; 015 (mood/vibe) feeds TMDB's genres, keywords
and synopsis to the LLM and gets vibe tags back.

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 005 | As the person running the app, I configure a TMDB API key outside source control so the catalog works on any machine |
| 006 | As a user, I search the live catalog by title and see matching films with poster and year           |
| 007 | As a user, the films I interact with are saved locally, so my ratings and watchlist load fast and survive without re-fetching |
| 008 | As a user, when the catalog is unavailable or rate-limited I get a clear message and everything already saved still works |

### Build my taste profile

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 009 | As a new user, I rate a starter set of popular films pulled live from the catalog (loved / liked / meh / never seen) |
| 010 | As a user, I pick the genres and eras I lean toward so my first recommendations aren't cold-start noise |
| 011 | As a user, I mark films and genres as "never show me this" so hard dislikes stay out of my feed      |
| 012 | As a user, I see my taste profile summarized in plain language ("slow-burn thrillers from the 90s") and can edit it |

### Get recommendations

The "why we picked this" line (014) is **written by an LLM** — the Claude API, reached
from C# through the official `Anthropic` NuGet package, model `claude-opus-5`. The
ranking itself stays rule-based; the LLM only puts the reason into words, given the
film and the user's taste profile. That adds a second external dependency with a cost
per call, so **030** and **031** mirror what 005 and 008 do for TMDB.

The mood and vibe tags in 015 come from the same place: the LLM reads a film's genres,
keywords and synopsis from TMDB and returns tags from a **fixed vocabulary** the app
defines (cozy, tense, funny, mind-bending, …), so the filter buttons stay stable. This
is per-film, not per-user, so tags are written once into the local film record from
007 and reused — no API call when someone taps a mood.

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 013 | As a user, I open the home page and see a ranked list of films picked for me                        |
| 014 | As a user, I see a short "why we picked this" explanation on every recommendation, written in natural language rather than assembled from a template |
| 015 | As a user, I filter tonight's picks by mood or vibe (cozy, tense, funny, mind-bending) instead of by genre, with each film's vibes inferred once by the LLM rather than hand-tagged |
| 016 | As a user, I set a runtime limit so I only see films that fit the time I actually have               |
| 017 | As a user, I hit "surprise me" and get one bold pick that stretches outside my usual taste          |
| 018 | As a user, I dismiss a recommendation with a reason ("seen it", "not tonight", "not for me") and the list refreshes |
| 030 | As the person running the app, I configure a Claude API key outside source control, alongside the TMDB key, so explanations work on any machine |
| 031 | As a user, when the explanation service is slow, rate-limited or unavailable, my recommendations still render — the list never blocks on the explanation |

(030 and 031 are numbered last because they were added after the first pass, but they
belong with 014 in build order: 030 before it, 031 immediately after.)

### Explore and decide on a film

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 019 | As a user, I open a film's detail page to see poster, synopsis, year, runtime, cast and rating from the live catalog |
| 020 | As a user, I see "more like this" on a film page so one good pick leads to the next                 |
| 021 | As a user, I play the trailer without leaving the page so I can commit in thirty seconds            |

### Track what I watch

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 022 | As a user, I add a film to my watchlist so I can come back to it another night                      |
| 023 | As a user, I mark a film watched and rate it, and my next recommendations visibly shift as a result |
| 024 | As a user, I browse my watch history so I can remember what I thought of something                  |
| 025 | As a user, I see a stats page about my viewing — top genres, decades, total hours, taste over time   |

### Movie night with friends

Movie night is **refresh-based**, not real-time. Everyone reloads (or the page polls)
to see where the group is up to — no SignalR, no persistent connections, no presence
tracking. The vote count in 028 is "how many have voted so far", read at page load,
which is enough to make the session feel shared without a live-sync build behind it.
Since the demo is local, this is shown as two or three browser sessions side by side,
which is why 004 seeds more than one account.

| #   | Story                                                                                              |
| --- | -------------------------------------------------------------------------------------------------- |
| 026 | As a user, I start a movie night session and share a join link                                      |
| 027 | As a friend, I join the session with my own account and my taste profile blends into a shared shortlist |
| 028 | As a group, we each swipe yes/no through the shortlist and see how many people have voted so far    |
| 029 | As a group, we see the first film everyone said yes to declared the winner, with an explanation of why it fits all of us |

## Suggested first slice

Auth and the live catalog both sit on the critical path now — nothing personalized
works until a user can log in, and nothing at all renders until the catalog answers.
The shortest path to something **visibly impressive** rather than merely complete:

**001 → 002 → 005 → 006 → 009 → 013 → 030 → 014**

That gets you: sign up, log in, both API keys wired up, live search, rate a handful
of films, and a home page of ranked recommendations each carrying its own
LLM-written "why we picked this" line. Story 014 is the payoff — build toward it,
don't defer it.

Then **004** (seeded demo account) early, because it makes every later story
demonstrable without re-onboarding by hand, and **008** plus **031** before showing
anyone anything — a rate-limited API during a live demo is the obvious way this falls
over, and now there are two of them.

## Open Questions

1. **When is LLM work done — on page load, or ahead of time?** Generating
   explanations as the home page renders is simpler but puts an API call on the
   critical path of the demo's first click; precomputing them for a ranked list is
   faster on screen and cheaper per user. Vibe tagging (015) has the same choice at
   film-ingest time. Both are spec decisions for 014 and 015, not map-level ones.

## How to use this map

Each story is small enough to build in a day. Generate a spec with
`/spec <number>` — e.g. `/spec 014`. Story numbers are the contract between this map
and `docs/specs/NNN-<slug>.md`, so avoid renumbering once specs exist.
