---
name: movierecommender-scope
description: movieRecommender product framing — user-confirmed decisions on purpose, catalog source, auth, and demo context that shape the story map in docs/product/story-map.md
metadata:
  type: project
---

Product decisions confirmed by the user on 2026-09-15, driving the 29-story map in
`docs/product/story-map.md`:

- **Recommender-first, tracker in service of it.** Watch history and ratings feed the
  recommendations. "Suggest what I should watch" is the headline. The tracking
  activity is in scope but is not the backbone.
- **Live external catalog API** (TMDB or OMDb — not yet narrowed), not a seeded local
  dataset. This pulled API-key config, live search, local persistence of
  interacted-with films, and graceful degradation under outage/rate-limit into the
  map as first-class stories.
- **Real authentication, multiple accounts.** Sign up / log in, each user owns their
  taste profile, ratings and history. ASP.NET Core Identity is the natural fit for
  this stack.
- **This is a demo build.** It needs a wow moment.

**Why:** the demo context is the single most load-bearing fact. It is why the group
"movie night" activity (stories 026–029) and the plain-language "why we picked this"
explanation (story 014) are must-haves rather than cuttable extras — they are the
moments meant to land in front of an audience. The user explicitly rejected treating
movie night as the first thing to cut.

**How to apply:** when scope pressure hits, cut toward demo impact, not toward
technical completeness — a walking skeleton that reaches something visibly
impressive beats one that is merely correct. Auth and the live catalog both sit on
the critical path, so the suggested first slice is 001, 002, 005, 006, 009, 013, 014.
Five open questions remain at the bottom of the story map (TMDB vs OMDb; whether the
"why" text is rule-based or LLM-written; whether movie night is real-time or
refresh-based; where mood/vibe tags come from; localhost vs hosted) — check those
before writing the specs they touch. Story numbers are the contract with
`docs/specs/NNN-<slug>.md`; no specs exist yet, so renumbering is still free.
